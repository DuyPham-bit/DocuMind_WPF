using DocuMind.Application.Interfaces;
using DocuMind.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DocuMind.Infrastructure.Persistence;

public sealed class EfChatHistoryStore(IDbContextFactory<AppDbContext> contexts) : IChatHistoryStore
{
    public async Task<List<ChatMessage>> LoadAsync(Guid workspaceId, Guid conversationId, int tokenBudget = 4000, CancellationToken ct = default)
    {
        await using var db = await contexts.CreateDbContextAsync(ct);
        var messages = await db.ChatMessages.AsNoTracking().Where(m => m.WorkspaceId == workspaceId && m.ConversationId == conversationId)
            .OrderByDescending(m => m.Timestamp).ThenByDescending(m => m.Id).Take(100).ToListAsync(ct);
        var selected = new List<ChatMessage>();
        var used = 0;
        foreach (var message in messages)
        {
            // Conservative byte-fallback upper bound plus role/framing allowance, independent of provider tokenizer.
            var cost = System.Text.Encoding.UTF8.GetByteCount(message.Content) + 16;
            if (used + cost > tokenBudget) break;
            selected.Add(message); used += cost;
        }
        selected.Reverse();
        while (selected.Count > 0 && selected[0].Role != DocuMind.Domain.Enums.MessageRole.User) selected.RemoveAt(0);
        return selected;
    }

    public async Task AppendAsync(ChatMessage user, ChatMessage assistant, CancellationToken ct = default)
    {
        await using var db = await contexts.CreateDbContextAsync(ct);
        db.ChatMessages.AddRange(user, assistant);
        await db.SaveChangesAsync(ct);
    }
}
