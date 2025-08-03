using Azunt.Models.Common;

namespace Azunt.DepartmentManagement;

public interface IDepartmentRepository
{
    Task<Department> AddAsync(Department model, string? connectionString = null);
    Task<List<Department>> GetAllAsync(string? connectionString = null);
    Task<Department> GetByIdAsync(long id, string? connectionString = null);
    Task<bool> UpdateAsync(Department model, string? connectionString = null);
    Task<bool> DeleteAsync(long id, string? connectionString = null);
    Task<ArticleSet<Department, int>> GetArticlesAsync<TParentIdentifier>(int pageIndex, int pageSize, string searchField, string searchQuery, string sortOrder, TParentIdentifier parentIdentifier, string? connectionString = null);
}
