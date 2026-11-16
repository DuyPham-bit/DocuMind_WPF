using DocuMind.Domain.Enums;

namespace DocuMind.Domain.Entities;

public class EmbeddingProfile
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public AiProviderType Provider { get; set; }
    public string Model { get; set; } = "";
    public string Endpoint { get; set; } = "";
    public int Dimension { get; set; }
    public string Normalization { get; set; } = "";
    public string PipelineVersion { get; set; } = "";
}
