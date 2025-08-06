using Microsoft.EntityFrameworkCore;

namespace Azunt.DepartmentManagement
{
    /// <summary>
    /// DepartmentApp에서 사용하는 데이터베이스 컨텍스트 클래스입니다.
    /// Entity Framework Core와 데이터베이스 간의 브리지 역할을 합니다.
    /// </summary>
    public class DepartmentDbContext : DbContext
    {
        /// <summary>
        /// DbContextOptions을 인자로 받는 생성자입니다.
        /// 주로 Program.cs 또는 Startup.cs에서 서비스로 등록할 때 사용됩니다.
        /// </summary>
        public DepartmentDbContext(DbContextOptions<DepartmentDbContext> options)
            : base(options)
        {
            // 기본적으로 NoTracking으로 설정하여 조회 성능 최적화
            ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
        }

        /// <summary>
        /// 데이터베이스 모델을 설정하는 메서드입니다.
        /// DB Provider에 따라 CreatedAt 기본값을 다르게 설정합니다.
        /// </summary>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            var providerName = Database.ProviderName;

            if (!string.IsNullOrEmpty(providerName) && providerName.Contains("SqlServer"))
            {
                // SQL Server에서는 GETDATE() 사용
                modelBuilder.Entity<Department>()
                    .Property(m => m.CreatedAt)
                    .HasDefaultValueSql("GETDATE()");
            }
            else
            {
                // Sqlite 등 다른 DB에서는 CURRENT_TIMESTAMP 사용
                modelBuilder.Entity<Department>()
                    .Property(m => m.CreatedAt)
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");
            }
        }

        /// <summary>
        /// DepartmentApp 관련 테이블을 정의합니다.
        /// </summary>
        public DbSet<Department> Departments { get; set; } = null!;
    }
}
