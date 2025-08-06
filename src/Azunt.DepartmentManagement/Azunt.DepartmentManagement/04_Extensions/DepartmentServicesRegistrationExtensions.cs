using Azunt.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Azunt.DepartmentManagement;

/// <summary>
/// DepartmentApp 의존성 주입 확장 메서드 (멀티 DB 지원)
/// </summary>
public static class DepartmentServicesRegistrationExtensions
{
    /// <summary>
    /// DepartmentApp 모듈의 서비스를 등록합니다.
    /// </summary>
    /// <param name="services">서비스 컨테이너</param>
    /// <param name="connectionString">연결 문자열</param>
    /// <param name="mode">레포지토리 사용 모드 (기본: EF Core)</param>
    /// <param name="dbProvider">EF Core 모드에서 사용할 DB Provider (기본: SqlServer)</param>
    /// <param name="dbContextLifetime">DbContext 수명 주기 (기본: Transient)</param>
    public static void AddDependencyInjectionContainerForDepartmentApp(
        this IServiceCollection services,
        string connectionString,
        RepositoryMode mode = RepositoryMode.EfCore,
        DbProvider dbProvider = DbProvider.SqlServer,
        ServiceLifetime dbContextLifetime = ServiceLifetime.Transient)
    {
        switch (mode)
        {
            case RepositoryMode.EfCore:
                // EF Core 방식 등록 (멀티 DB 지원)
                services.AddDbContext<DepartmentDbContext>(options =>
                {
                    switch (dbProvider)
                    {
                        case DbProvider.SqlServer:
                            options.UseSqlServer(connectionString);
                            break;
                        case DbProvider.Sqlite:
                            options.UseSqlite(connectionString);
                            break;
                        default:
                            throw new InvalidOperationException(
                                $"Unsupported EF Core DbProvider '{dbProvider}'. Supported: SqlServer, Sqlite.");
                    }
                }, dbContextLifetime);

                services.AddTransient<IDepartmentRepository, DepartmentRepository>();
                services.AddTransient<DepartmentDbContextFactory>();
                break;

            case RepositoryMode.Dapper:
                // Dapper 방식 등록
                services.AddTransient<IDepartmentRepository>(provider =>
                    new DepartmentRepositoryDapper(
                        connectionString,
                        provider.GetRequiredService<ILoggerFactory>()));
                break;

            case RepositoryMode.AdoNet:
                // ADO.NET 방식 등록
                services.AddTransient<IDepartmentRepository>(provider =>
                    new DepartmentRepositoryAdoNet(
                        connectionString,
                        provider.GetRequiredService<ILoggerFactory>()));
                break;

            default:
                throw new InvalidOperationException(
                    $"Invalid repository mode '{mode}'. Supported modes: EfCore, Dapper, AdoNet.");
        }
    }
}
