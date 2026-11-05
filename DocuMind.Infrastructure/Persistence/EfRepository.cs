using System.Linq.Expressions;
using DocuMind.Application.Interfaces;
using DocuMind.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DocuMind.Infrastructure.Persistence;

public class EfRepository<T> : IRepository<T> where T : class
{
    protected readonly AppDbContext _db;
    protected readonly DbSet<T> _dbSet;

    public EfRepository(AppDbContext db)
    {
        _db = db;
        _dbSet = db.Set<T>();
    }

    public async Task<T?> GetByIdAsync(Guid id)
    {
        return await _dbSet.FindAsync(id);
    }

    public async Task<List<T>> GetAllAsync()
    {
        return await _dbSet.ToListAsync();
    }

    public async Task<List<T>> FindAsync(Expression<Func<T, bool>> predicate)
    {
        return await _dbSet.Where(predicate).ToListAsync();
    }

    public async Task AddAsync(T entity)
    {
        await _dbSet.AddAsync(entity);
    }

    public async Task AddRangeAsync(IEnumerable<T> entities)
    {
        await _dbSet.AddRangeAsync(entities);
    }

    public Task UpdateAsync(T entity)
    {
        _dbSet.Update(entity);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(T entity)
    {
        _dbSet.Remove(entity);
        return Task.CompletedTask;
    }

    public Task DeleteRangeAsync(IEnumerable<T> entities)
    {
        _dbSet.RemoveRange(entities);
        return Task.CompletedTask;
    }

    public async Task<int> SaveChangesAsync()
    {
        return await _db.SaveChangesAsync();
    }
}

public class EfDocumentChunkRepository : EfRepository<DocumentChunk>, IDocumentChunkRepository
{
    public EfDocumentChunkRepository(AppDbContext db) : base(db)
    {
    }

    public async Task<List<DocumentChunk>> GetByWorkspaceAsync(Guid workspaceId)
    {
        return await _dbSet
            .Where(c => c.WorkspaceId == workspaceId)
            .OrderBy(c => c.FileName)
            .ThenBy(c => c.PageNumber)
            .ToListAsync();
    }

    public async Task<List<string>> GetDistinctFilesByWorkspaceAsync(Guid workspaceId)
    {
        return await _dbSet
            .Where(c => c.WorkspaceId == workspaceId)
            .Select(c => c.FileName)
            .Distinct()
            .OrderBy(f => f)
            .ToListAsync();
    }

    public async Task DeleteByFileNameAsync(Guid workspaceId, string fileName)
    {
        var existing = await _dbSet
            .Where(c => c.WorkspaceId == workspaceId && c.FileName == fileName)
            .ToListAsync();

        if (existing.Count > 0)
        {
            _dbSet.RemoveRange(existing);
            await _db.SaveChangesAsync();
        }
    }
}

