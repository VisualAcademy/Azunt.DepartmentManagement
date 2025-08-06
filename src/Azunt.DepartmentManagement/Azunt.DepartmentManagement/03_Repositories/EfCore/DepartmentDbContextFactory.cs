using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Azunt.DepartmentManagement;

/// <summary>
/// EF Core DbContext를 생성하는 Factory 클래스
/// </summary>
public class DepartmentDbContextFactory
{
    private readonly IConfiguration? _configuration;
    private readonly DbProvider _dbProvider;

    /// <summary>
    /// 기본 생성자 (Configuration 없이, 기본 Provider는 SqlServer)
    /// </summary>
    public DepartmentDbContextFactory()
    {
        _dbProvider = DbProvider.SqlServer;
    }

    /// <summary>
    /// IConfiguration과 DbProvider를 주입받는 생성자
    /// </summary>
    public DepartmentDbContextFactory(IConfiguration configuration, DbProvider dbProvider = DbProvider.SqlServer)
    {
        _configuration = configuration;
        _dbProvider = dbProvider;
    }

    /// <summary>
    /// 연결 문자열을 사용하여 DbContext 인스턴스를 생성합니다.
    /// </summary>
    public DepartmentDbContext CreateDbContext(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new ArgumentException("Connection string must not be null or empty.", nameof(connectionString));
        }

        var optionsBuilder = new DbContextOptionsBuilder<DepartmentDbContext>();

        switch (_dbProvider)
        {
            case DbProvider.SqlServer:
                optionsBuilder.UseSqlServer(connectionString);
                break;
            case DbProvider.Sqlite:
                optionsBuilder.UseSqlite(connectionString);
                break;
            default:
                throw new InvalidOperationException($"Unsupported database provider: {_dbProvider}");
        }

        return new DepartmentDbContext(optionsBuilder.Options);
    }

    /// <summary>
    /// DbContextOptions를 사용하여 DbContext 인스턴스를 생성합니다.
    /// </summary>
    public DepartmentDbContext CreateDbContext(DbContextOptions<DepartmentDbContext> options)
    {
        ArgumentNullException.ThrowIfNull(options);

        return new DepartmentDbContext(options);
    }

    /// <summary>
    /// appsettings.json의 "DefaultConnection"을 사용하여 DbContext 인스턴스를 생성합니다.
    /// </summary>
    public DepartmentDbContext CreateDbContext()
    {
        if (_configuration == null)
        {
            throw new InvalidOperationException("Configuration is not provided.");
        }

        var defaultConnection = _configuration.GetConnectionString("DefaultConnection");

        if (string.IsNullOrWhiteSpace(defaultConnection))
        {
            throw new InvalidOperationException("DefaultConnection is not configured properly.");
        }

        return CreateDbContext(defaultConnection);
    }
}

/// <summary>
/// EF Core에서 사용할 데이터베이스 Provider 종류
/// </summary>
public enum DbProvider
{
    SqlServer,
    Sqlite
}
