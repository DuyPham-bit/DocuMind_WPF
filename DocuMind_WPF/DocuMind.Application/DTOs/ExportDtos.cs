using DocuMind.Domain.Enums;

namespace DocuMind.Application.DTOs;

public sealed record ChatPdfMessage(
    string SenderName,
    bool IsUser,
    DateTime Timestamp,
    ChatMode Mode,
    string Provider,
    string Model,
    double Temperature,
    string Content,
    IReadOnlyList<RagSourceDto> Sources);
