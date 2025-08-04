using Azunt.Models.Common;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace Azunt.DepartmentManagement;

public class DepartmentRepositoryDapper : IDepartmentRepository
{
    private readonly string _defaultConnectionString;
    private readonly ILogger<DepartmentRepositoryDapper> _logger;

    public DepartmentRepositoryDapper(string defaultConnectionString, ILoggerFactory loggerFactory)
    {
        _defaultConnectionString = defaultConnectionString;
        _logger = loggerFactory.CreateLogger<DepartmentRepositoryDapper>();
    }

    private SqlConnection GetConnection(string? connectionString)
    {
        return new SqlConnection(connectionString ?? _defaultConnectionString);
    }

    public async Task<Department> AddAsync(Department model, string? connectionString = null)
    {
        var conn = GetConnection(connectionString);
        var sql = @"INSERT INTO Departments (Active, CreatedAt, CreatedBy, Name)
                    OUTPUT INSERTED.Id
                    VALUES (@Active, @CreatedAt, @CreatedBy, @Name)";

        model.CreatedAt = DateTimeOffset.UtcNow;
        model.Id = await conn.ExecuteScalarAsync<long>(sql, model);
        return model;
    }

    public async Task<List<Department>> GetAllAsync(string? connectionString = null)
    {
        var conn = GetConnection(connectionString);
        var sql = "SELECT Id, Active, CreatedAt, CreatedBy, Name FROM Departments ORDER BY Id DESC";
        var list = await conn.QueryAsync<Department>(sql);
        return list.ToList();
    }

    public async Task<Department> GetByIdAsync(long id, string? connectionString = null)
    {
        var conn = GetConnection(connectionString);
        var sql = "SELECT Id, Active, CreatedAt, CreatedBy, Name FROM Departments WHERE Id = @Id";
        var model = await conn.QuerySingleOrDefaultAsync<Department>(sql, new { Id = id });
        return model ?? new Department();
    }

    public async Task<bool> UpdateAsync(Department model, string? connectionString = null)
    {
        var conn = GetConnection(connectionString);
        var sql = @"UPDATE Departments SET
                        Active = @Active,
                        Name = @Name
                    WHERE Id = @Id";

        var rows = await conn.ExecuteAsync(sql, model);
        return rows > 0;
    }

    public async Task<bool> DeleteAsync(long id, string? connectionString = null)
    {
        var conn = GetConnection(connectionString);
        var sql = "DELETE FROM Departments WHERE Id = @Id";
        var rows = await conn.ExecuteAsync(sql, new { Id = id });
        return rows > 0;
    }

    public async Task<ArticleSet<Department, int>> GetArticlesAsync<TParentIdentifier>(
        int pageIndex, int pageSize, string searchField, string searchQuery, string sortOrder, TParentIdentifier parentIdentifier, string? connectionString = null)
    {
        var all = await GetAllAsync(connectionString);
        var filtered = string.IsNullOrWhiteSpace(searchQuery)
            ? all
            : all.Where(m => m.Name != null && m.Name.Contains(searchQuery)).ToList();

        var paged = filtered
            .Skip(pageIndex * pageSize)
            .Take(pageSize)
            .ToList();

        return new ArticleSet<Department, int>(paged, filtered.Count);
    }
}