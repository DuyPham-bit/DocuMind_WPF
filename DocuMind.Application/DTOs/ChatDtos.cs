using DocuMind.Domain.Enums;

namespace DocuMind.Application.DTOs;

public record RagSourceDto(
    string FileName,
    int PageNumber,
    string Content,
    float Score,
    Guid? DocumentId = null,
    string? SourcePath = null
);

public record ChatResultDto(
    string ResponseText,
    List<RagSourceDto> Sources
);

public record ProviderConfigDto(
    AiProviderType ProviderType,
    string ApiKey,
    string ChatModel,
    string BaseUrl = "",
    double Temperature = 0.7,
    bool IsActive = true
);
