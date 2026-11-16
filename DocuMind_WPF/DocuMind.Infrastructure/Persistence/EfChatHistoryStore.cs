using DocuMind.Application.Interfaces;
using DocuMind.Domain.Entities;
using DocuMind.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace DocuMind.Infrastructure.Persistence;

public sealed class EfChatHistoryStore(IDbContextFactory<AppDbContext> contexts) : IChatHistoryStore
{
    public async Task<List<ChatMessage>> LoadForDisplayAsync(Guid workspaceId, Guid conversationId, int limit = 200, CancellationToken ct = default)
    {
        await using var db = await contexts.CreateDbContextAsync(ct);
        var messages = await db.ChatMessages.AsNoTracking().Where(m => m.WorkspaceId == workspaceId && m.ConversationId == conversationId)
            .OrderByDescending(m => m.Timestamp).ThenByDescending(m => m.Id).Take(Math.Max(1, limit)).ToListAsync(ct);
        messages.Reverse();
        return messages;
    }

    public async Task<List<ChatMessage>> LoadAsync(Guid workspaceId, Guid conversationId, int tokenBudget = 16000, CancellationToken ct = default)
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
        while (selected.Count > 0 && selected[0].Role != MessageRole.User) selected.RemoveAt(0);
        return selected;
    }

    public async Task<List<ConversationSummary>> ListConversationsAsync(Guid workspaceId, CancellationToken ct = default)
    {
        await using var db = await contexts.CreateDbContextAsync(ct);
        var rows = await db.ChatMessages.AsNoTracking()
            .Where(m => m.WorkspaceId == workspaceId)
            .OrderBy(m => m.Timestamp).ThenBy(m => m.Id)
            .Select(m => new { m.ConversationId, m.Role, m.Content, m.Timestamp })
            .ToListAsync(ct);

        return rows
            .GroupBy(r => r.ConversationId)
            .Select(g =>
            {
                var firstUser = g.FirstOrDefault(x => x.Role == MessageRole.User)?.Content
                    ?? g.First().Content;
                var normalized = string.Join(" ", firstUser.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
                var title = normalized.Length > 52 ? normalized[..52] + "…" : normalized;
                if (string.IsNullOrWhiteSpace(title)) title = "Conversation";
                return new ConversationSummary(g.Key, title, g.Max(x => x.Timestamp).ToLocalTime(), g.Count());
            })
            .OrderByDescending(c => c.LastMessageAt)
            .ToList();
    }

    public async Task AppendAsync(ChatMessage user, ChatMessage assistant, CancellationToken ct = default)
    {
        await using var db = await contexts.CreateDbContextAsync(ct);
        db.ChatMessages.AddRange(user, assistant);
        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteConversationAsync(Guid workspaceId, Guid conversationId, CancellationToken ct = default)
    {
        await using var db = await contexts.CreateDbContextAsync(ct);
        await db.ChatMessages
            .Where(m => m.WorkspaceId == workspaceId && m.ConversationId == conversationId)
            .ExecuteDeleteAsync(ct);
    }
}
