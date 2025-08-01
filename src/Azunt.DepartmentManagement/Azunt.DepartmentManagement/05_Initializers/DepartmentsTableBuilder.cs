using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;

namespace Azunt.DepartmentManagement;

public class DepartmentsTableBuilder
{
    private readonly string _masterConnectionString;
    private readonly ILogger<DepartmentsTableBuilder> _logger;

    public DepartmentsTableBuilder(string masterConnectionString, ILogger<DepartmentsTableBuilder> logger)
    {
        _masterConnectionString = masterConnectionString;
        _logger = logger;
    }

    public void BuildTenantDatabases()
    {
        var tenantConnectionStrings = GetTenantConnectionStrings();

        foreach (var connStr in tenantConnectionStrings)
        {
            var dbName = new SqlConnectionStringBuilder(connStr).InitialCatalog;

            try
            {
                EnsureDepartmentsTable(connStr);
                _logger.LogInformation($"Departments table processed (tenant DB: {dbName})");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error processing tenant DB: {dbName}");
            }
        }
    }

    public void BuildMasterDatabase()
    {
        var dbName = new SqlConnectionStringBuilder(_masterConnectionString).InitialCatalog;

        try
        {
            EnsureDepartmentsTable(_masterConnectionString);
            _logger.LogInformation($"Departments table processed (master DB: {dbName})");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error processing master DB: {dbName}");
        }
    }

    private List<string> GetTenantConnectionStrings()
    {
        var result = new List<string>();

        using (var connection = new SqlConnection(_masterConnectionString))
        {
            connection.Open();
            var cmd = new SqlCommand("SELECT ConnectionString FROM dbo.Tenants", connection);

            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    var connectionString = reader["ConnectionString"]?.ToString();
                    if (!string.IsNullOrEmpty(connectionString))
                    {
                        result.Add(connectionString);
                    }
                }
            }
        }

        return result;
    }

    private void EnsureDepartmentsTable(string connectionString)
    {
        using (var connection = new SqlConnection(connectionString))
        {
            connection.Open();

            // 1. 테이블 존재 여부 확인
            var cmdCheck = new SqlCommand(@"
                SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES 
                WHERE TABLE_NAME = 'Departments'", connection);

            int tableCount = (int)cmdCheck.ExecuteScalar();

            // 2. 테이블 없으면 생성
            if (tableCount == 0)
            {
                var cmdCreate = new SqlCommand(@"
                    CREATE TABLE [dbo].[Departments] (
                        [Id] BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                        [Active] BIT DEFAULT ((1)) NULL,
                        [CreatedAt] DATETIMEOFFSET NULL DEFAULT SYSDATETIMEOFFSET(),
                        [CreatedBy] NVARCHAR(255) NULL,
                        [Name] NVARCHAR(MAX) NULL
                    )", connection);

                cmdCreate.ExecuteNonQuery();
                _logger.LogInformation("Departments table created.");
            }
            else
            {
                // 3. 누락 컬럼 확인 및 추가
                var expectedColumns = new Dictionary<string, string>
                {
                    ["Active"] = "BIT NULL DEFAULT(1)",
                    ["CreatedAt"] = "DATETIMEOFFSET(7) NULL DEFAULT SYSDATETIMEOFFSET()",
                    ["CreatedBy"] = "NVARCHAR(255) NULL",
                    ["Name"] = "NVARCHAR(MAX) NULL"
                };

                foreach (var kvp in expectedColumns)
                {
                    var columnName = kvp.Key;

                    var cmdColumnCheck = new SqlCommand(@"
                        SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS 
                        WHERE TABLE_NAME = 'Departments' AND COLUMN_NAME = @ColumnName", connection);
                    cmdColumnCheck.Parameters.AddWithValue("@ColumnName", columnName);

                    int colExists = (int)cmdColumnCheck.ExecuteScalar();

                    if (colExists == 0)
                    {
                        var alterCmd = new SqlCommand(
                            $"ALTER TABLE [dbo].[Departments] ADD [{columnName}] {kvp.Value}", connection);
                        alterCmd.ExecuteNonQuery();

                        _logger.LogInformation($"Column added: {columnName} ({kvp.Value})");
                    }
                }
            }

            // 4. 초기 데이터 시드
            var cmdCountRows = new SqlCommand("SELECT COUNT(*) FROM [dbo].[Departments]", connection);
            int rowCount = (int)cmdCountRows.ExecuteScalar();

            if (rowCount == 0)
            {
                var cmdInsertDefaults = new SqlCommand(@"
                    INSERT INTO [dbo].[Departments] (Active, CreatedAt, CreatedBy, Name)
                    VALUES
                        (1, SYSDATETIMEOFFSET(), 'System', 'Initial Department 1'),
                        (1, SYSDATETIMEOFFSET(), 'System', 'Initial Department 2')", connection);

                int inserted = cmdInsertDefaults.ExecuteNonQuery();
                _logger.LogInformation($"Departments 기본 데이터 {inserted}건 삽입 완료");
            }
        }
    }

    public static void Run(IServiceProvider services, bool forMaster)
    {
        try
        {
            var logger = services.GetRequiredService<ILogger<DepartmentsTableBuilder>>();
            var config = services.GetRequiredService<IConfiguration>();
            var masterConnectionString = config.GetConnectionString("DefaultConnection");

            if (string.IsNullOrEmpty(masterConnectionString))
            {
                throw new InvalidOperationException("DefaultConnection is not configured in appsettings.json.");
            }

            var builder = new DepartmentsTableBuilder(masterConnectionString, logger);

            if (forMaster)
            {
                builder.BuildMasterDatabase();
            }
            else
            {
                builder.BuildTenantDatabases();
            }
        }
        catch (Exception ex)
        {
            var fallbackLogger = services.GetService<ILogger<DepartmentsTableBuilder>>();
            fallbackLogger?.LogError(ex, "Error while processing Departments table.");
        }
    }
}
