using System.Linq.Expressions;
using DocuMind.Domain.Entities;

namespace DocuMind.Application.Interfaces;

public interface IRepository<T> where T : class
{
    Task<T?> GetByIdAsync(Guid id);
    Task<List<T>> GetAllAsync();
    Task<List<T>> FindAsync(Expression<Func<T, bool>> predicate);
    Task AddAsync(T entity);
    Task AddRangeAsync(IEnumerable<T> entities);
    Task UpdateAsync(T entity);
    Task DeleteAsync(T entity);
    Task DeleteRangeAsync(IEnumerable<T> entities);
}

public interface IDocumentChunkRepository : IRepository<DocumentChunk>
{
    Task<List<DocumentChunk>> GetByWorkspaceAsync(Guid workspaceId);
    Task<List<string>> GetDistinctFilesByWorkspaceAsync(Guid workspaceId);
    Task DeleteByFileNameAsync(Guid workspaceId, string fileName);
}
