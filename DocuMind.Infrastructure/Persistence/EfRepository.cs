using System.Linq.Expressions;
using DocuMind.Application.Interfaces;
using DocuMind.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DocuMind.Infrastructure.Persistence;

// Each method is a complete unit of work; callers never hold a DbContext across awaits/HTTP.
public class EfRepository<T>(IDbContextFactory<AppDbContext> contexts) : IRepository<T> where T : class
{
    public async Task<T?> GetByIdAsync(Guid id)
    {
        await using var db = await contexts.CreateDbContextAsync();
        return await db.Set<T>().FindAsync(id);
    }
    public async Task<List<T>> GetAllAsync()
    {
        await using var db = await contexts.CreateDbContextAsync();
        return await db.Set<T>().AsNoTracking().ToListAsync();
    }
    public async Task<List<T>> FindAsync(Expression<Func<T, bool>> predicate)
    {
        await using var db = await contexts.CreateDbContextAsync();
        return await db.Set<T>().AsNoTracking().Where(predicate).ToListAsync();
    }
    public Task AddAsync(T entity) => AddRangeAsync([entity]);
    public async Task AddRangeAsync(IEnumerable<T> entities)
    {
        await using var db = await contexts.CreateDbContextAsync();
        db.Set<T>().AddRange(entities);
        await db.SaveChangesAsync();
    }
    public async Task UpdateAsync(T entity)
    {
        await using var db = await contexts.CreateDbContextAsync();
        db.Set<T>().Update(entity);
        await db.SaveChangesAsync();
    }
    public Task DeleteAsync(T entity) => DeleteRangeAsync([entity]);
    public async Task DeleteRangeAsync(IEnumerable<T> entities)
    {
        await using var db = await contexts.CreateDbContextAsync();
        db.Set<T>().RemoveRange(entities);
        await db.SaveChangesAsync();
    }
}

public sealed class EfDocumentChunkRepository(IDbContextFactory<AppDbContext> contexts) : EfRepository<DocumentChunk>(contexts), IDocumentChunkRepository
{
    public async Task<List<DocumentChunk>> GetByWorkspaceAsync(Guid workspaceId) =>
        (await FindAsync(c => c.WorkspaceId == workspaceId)).OrderBy(c => c.FileName).ThenBy(c => c.PageNumber).ToList();
    public async Task<List<string>> GetDistinctFilesByWorkspaceAsync(Guid workspaceId) =>
        (await GetByWorkspaceAsync(workspaceId)).Select(c => c.FileName).Distinct().Order().ToList();
    public async Task DeleteByFileNameAsync(Guid workspaceId, string fileName) =>
        await DeleteRangeAsync(await FindAsync(c => c.WorkspaceId == workspaceId && c.FileName == fileName));
}
