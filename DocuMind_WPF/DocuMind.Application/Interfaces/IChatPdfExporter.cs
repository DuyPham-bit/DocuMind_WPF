using DocuMind.Application.DTOs;

namespace DocuMind.Application.Interfaces;

public interface IChatPdfExporter
{
    byte[] Export(string workspaceName, IReadOnlyList<ChatPdfMessage> messages);
}
