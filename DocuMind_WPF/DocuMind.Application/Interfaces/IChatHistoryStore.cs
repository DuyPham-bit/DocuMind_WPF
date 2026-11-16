using DocuMind.Domain.Entities;

namespace DocuMind.Application.Interfaces;

public sealed record ConversationSummary(Guid ConversationId, string Title, DateTime LastMessageAt, int MessageCount);

public interface IChatHistoryStore
{
    // Context for the model: newest complete turns that fit a UTF-8 byte budget (Vietnamese text costs 2-3 bytes per character).
    Task<List<ChatMessage>> LoadAsync(Guid workspaceId, Guid conversationId, int tokenBudget = 16000, CancellationToken ct = default);
    // What the chat view shows: the newest messages, oldest first, with no size budget.
    Task<List<ChatMessage>> LoadForDisplayAsync(Guid workspaceId, Guid conversationId, int limit = 200, CancellationToken ct = default);
    Task<List<ConversationSummary>> ListConversationsAsync(Guid workspaceId, CancellationToken ct = default);
    Task AppendAsync(ChatMessage user, ChatMessage assistant, CancellationToken ct = default);
    Task DeleteConversationAsync(Guid workspaceId, Guid conversationId, CancellationToken ct = default);
}
