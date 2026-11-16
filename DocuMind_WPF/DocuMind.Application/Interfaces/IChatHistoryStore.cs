using DocuMind.Domain.Entities;

namespace DocuMind.Application.Interfaces;

public interface IChatHistoryStore
{
    Task<List<ChatMessage>> LoadAsync(Guid workspaceId, Guid conversationId, int tokenBudget = 4000, CancellationToken ct = default);
    Task AppendAsync(ChatMessage user, ChatMessage assistant, CancellationToken ct = default);
}
