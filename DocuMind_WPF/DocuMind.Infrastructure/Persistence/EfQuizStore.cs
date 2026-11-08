using DocuMind.Application.DTOs;
using DocuMind.Application.Interfaces;
using DocuMind.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DocuMind.Infrastructure.Persistence;

public sealed class EfQuizStore(IDbContextFactory<AppDbContext> contexts) : IQuizStore
{
    public async Task SaveAsync(QuizAttempt attempt, CancellationToken ct = default)
    {
        await using var db = await contexts.CreateDbContextAsync(ct);
        db.QuizAttempts.Add(attempt);
        await db.SaveChangesAsync(ct);
    }

    public async Task<List<QuizAttemptSummary>> ListAsync(Guid workspaceId, int take = 20, CancellationToken ct = default)
    {
        await using var db = await contexts.CreateDbContextAsync(ct);
        return await db.QuizAttempts.AsNoTracking().Where(a => a.WorkspaceId == workspaceId)
            .OrderByDescending(a => a.CreatedAt).Take(take)
            .Select(a => new QuizAttemptSummary(a.Id, a.Topic, a.CorrectCount, a.QuestionCount, a.Model, a.CreatedAt)).ToListAsync(ct);
    }
}
