using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;

namespace Azunt.DepartmentManagement;

/// <summary>
/// Sqlite 전용 Departments 테이블 생성 및 컬럼 보정/시드 클래스
/// </summary>
public class DepartmentsTableBuilderSqlite
{
    private readonly string _masterConnectionString;
    private readonly ILogger<DepartmentsTableBuilderSqlite> _logger;

    public DepartmentsTableBuilderSqlite(string masterConnectionString, ILogger<DepartmentsTableBuilderSqlite> logger)
    {
        _masterConnectionString = masterConnectionString;
        _logger = logger;
    }

    public void BuildTenantDatabases()
    {
        var tenantConnectionStrings = GetTenantConnectionStrings();

        foreach (var connStr in tenantConnectionStrings)
        {
            try
            {
                EnsureDepartmentsTable(connStr);
                _logger.LogInformation($"Departments table processed (tenant DB: {connStr})");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error processing tenant DB: {connStr}");
            }
        }
    }

    public void BuildMasterDatabase()
    {
        try
        {
            EnsureDepartmentsTable(_masterConnectionString);
            _logger.LogInformation($"Departments table processed (master DB)");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error processing master DB");
        }
    }

    private List<string> GetTenantConnectionStrings()
    {
        var result = new List<string>();

        using (var connection = new SqliteConnection(_masterConnectionString))
        {
            connection.Open();
            var cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT ConnectionString FROM Tenants"; // Sqlite는 스키마 없음

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
        using (var connection = new SqliteConnection(connectionString))
        {
            connection.Open();

            // 1. 테이블 존재 여부 확인
            var cmdCheck = connection.CreateCommand();
            cmdCheck.CommandText = @"
                SELECT COUNT(*) 
                FROM sqlite_master 
                WHERE type='table' AND name='Departments'";
            var tableCount = Convert.ToInt32(cmdCheck.ExecuteScalar());

            // 2. 테이블 없으면 생성
            if (tableCount == 0)
            {
                var cmdCreate = connection.CreateCommand();
                cmdCreate.CommandText = @"
                    CREATE TABLE Departments (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        Active INTEGER DEFAULT 1,
                        CreatedAt TEXT DEFAULT (datetime('now')),
                        CreatedBy TEXT,
                        Name TEXT
                    )";
                cmdCreate.ExecuteNonQuery();
                _logger.LogInformation("Departments table created.");
            }
            else
            {
                // 3. 누락 컬럼 확인 및 추가
                var expectedColumns = new Dictionary<string, string>
                {
                    ["Active"] = "INTEGER DEFAULT 1",
                    ["CreatedAt"] = "TEXT DEFAULT (datetime('now'))",
                    ["CreatedBy"] = "TEXT",
                    ["Name"] = "TEXT"
                };

                foreach (var kvp in expectedColumns)
                {
                    var columnName = kvp.Key;

                    var cmdColumnCheck = connection.CreateCommand();
                    cmdColumnCheck.CommandText = $"PRAGMA table_info(Departments);";

                    bool colExists = false;
                    using (var reader = cmdColumnCheck.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            if (string.Equals(reader["name"]?.ToString(), columnName, StringComparison.OrdinalIgnoreCase))
                            {
                                colExists = true;
                                break;
                            }
                        }
                    }

                    if (!colExists)
                    {
                        var alterCmd = connection.CreateCommand();
                        alterCmd.CommandText = $"ALTER TABLE Departments ADD COLUMN {columnName} {kvp.Value}";
                        alterCmd.ExecuteNonQuery();

                        _logger.LogInformation($"Column added: {columnName} ({kvp.Value})");
                    }
                }
            }

            // 4. 초기 데이터 시드
            var cmdCountRows = connection.CreateCommand();
            cmdCountRows.CommandText = "SELECT COUNT(*) FROM Departments";
            int rowCount = Convert.ToInt32(cmdCountRows.ExecuteScalar());

            if (rowCount == 0)
            {
                var cmdInsertDefaults = connection.CreateCommand();
                cmdInsertDefaults.CommandText = @"
                    INSERT INTO Departments (Active, CreatedAt, CreatedBy, Name)
                    VALUES
                        (1, datetime('now'), 'System', 'Initial Department 1'),
                        (1, datetime('now'), 'System', 'Initial Department 2')";
                int inserted = cmdInsertDefaults.ExecuteNonQuery();
                _logger.LogInformation($"Departments 기본 데이터 {inserted}건 삽입 완료");
            }
        }
    }

    public static void Run(IServiceProvider services, bool forMaster)
    {
        try
        {
            var logger = services.GetRequiredService<ILogger<DepartmentsTableBuilderSqlite>>();
            var config = services.GetRequiredService<IConfiguration>();
            var masterConnectionString = config.GetConnectionString("DefaultConnection");

            if (string.IsNullOrEmpty(masterConnectionString))
            {
                throw new InvalidOperationException("DefaultConnection is not configured in appsettings.json.");
            }

            var builder = new DepartmentsTableBuilderSqlite(masterConnectionString, logger);

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
            var fallbackLogger = services.GetService<ILogger<DepartmentsTableBuilderSqlite>>();
            fallbackLogger?.LogError(ex, "Error while processing Departments table for Sqlite.");
        }
    }
}
