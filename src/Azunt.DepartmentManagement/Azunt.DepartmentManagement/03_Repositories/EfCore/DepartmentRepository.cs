using Azunt.Models.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Azunt.DepartmentManagement
{
    public class DepartmentRepository : IDepartmentRepository
    {
        private readonly DepartmentDbContextFactory _factory;
        private readonly ILogger<DepartmentRepository> _logger;

        public DepartmentRepository(
            DepartmentDbContextFactory factory,
            ILoggerFactory loggerFactory)
        {
            _factory = factory;
            _logger = loggerFactory.CreateLogger<DepartmentRepository>();
        }

        private DepartmentDbContext CreateContext(string? connectionString)
        {
            return string.IsNullOrEmpty(connectionString)
                ? _factory.CreateDbContext()
                : _factory.CreateDbContext(connectionString);
        }

        public async Task<Department> AddAsync(Department model, string? connectionString = null)
        {
            await using var context = CreateContext(connectionString);
            model.CreatedAt = DateTime.UtcNow;
            context.Departments.Add(model);
            await context.SaveChangesAsync();
            return model;
        }

        public async Task<List<Department>> GetAllAsync(string? connectionString = null)
        {
            await using var context = CreateContext(connectionString);
            return await context.Departments
                .OrderByDescending(m => m.Id)
                .ToListAsync();
        }

        public async Task<Department> GetByIdAsync(long id, string? connectionString = null)
        {
            await using var context = CreateContext(connectionString);
            return await context.Departments
                       .SingleOrDefaultAsync(m => m.Id == id)
                   ?? new Department();
        }

        public async Task<bool> UpdateAsync(Department model, string? connectionString = null)
        {
            await using var context = CreateContext(connectionString);
            context.Attach(model);
            context.Entry(model).State = EntityState.Modified;
            return await context.SaveChangesAsync() > 0;
        }

        public async Task<bool> DeleteAsync(long id, string? connectionString = null)
        {
            await using var context = CreateContext(connectionString);
            var entity = await context.Departments.FindAsync(id);
            if (entity == null) return false;
            context.Departments.Remove(entity);
            return await context.SaveChangesAsync() > 0;
        }

        public async Task<ArticleSet<Department, int>> GetArticlesAsync<TParentIdentifier>(
            int pageIndex, int pageSize, string searchField, string searchQuery, string sortOrder, TParentIdentifier parentIdentifier, string? connectionString = null)
        {
            await using var context = CreateContext(connectionString);

            var query = context.Departments.AsQueryable();

            if (!string.IsNullOrEmpty(searchQuery))
            {
                query = query.Where(m => m.Name!.Contains(searchQuery));
            }

            var totalCount = await query.CountAsync();
            var items = await query
                .OrderByDescending(m => m.Id)
                .Skip(pageIndex * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new ArticleSet<Department, int>(items, totalCount);
        }
    }
}