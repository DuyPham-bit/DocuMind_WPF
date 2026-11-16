using DocuMind.Application.DTOs;
using DocuMind.Domain.Entities;

namespace DocuMind.Application.Interfaces;

public interface IQuizStore
{
    Task SaveAsync(QuizAttempt attempt, CancellationToken ct = default);
    Task<List<QuizAttemptSummary>> ListAsync(Guid workspaceId, int take = 20, CancellationToken ct = default);
}
