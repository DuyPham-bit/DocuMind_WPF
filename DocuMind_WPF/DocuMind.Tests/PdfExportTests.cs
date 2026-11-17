using System.Text;
using DocuMind.Application.DTOs;
using DocuMind.Application.Interfaces;
using DocuMind.Application.Services;
using DocuMind.Domain.Enums;
using DocuMind.Infrastructure.Export;
using DocuMind.Infrastructure.Persistence;
using DocuMind.WPF.ViewModels;
using UglyToad.PdfPig;

namespace DocuMind.Tests;

public class PdfExportTests
{
    [Fact]
    public void ExporterProducesReadableMultiPagePdfWithVietnameseAndSources()
    {
        var exporter = new PdfPigConversationExporter();
        var longBody = string.Join(" ", Enumerable.Repeat("Kiến trúc Clean Architecture tách biệt Domain, Application, Infrastructure và WPF.", 65));
        var messages = new List<ChatPdfMessage>
        {
            new("You", true, new DateTime(2026, 11, 17, 10, 0, 0), ChatMode.Rag, "Gemini", "gemini-3.5-flash", 0.3,
                "Giải thích Dependency Injection trong .NET và vòng đời Scoped?", []),
            new("DocuMind AI", false, new DateTime(2026, 11, 17, 10, 0, 5), ChatMode.Rag, "Gemini", "gemini-3.5-flash", 0.3,
                longBody,
                [new RagSourceDto("PRN212_Slides.pdf", 12, "Scoped services are created once per request or scope.", 0.912f)])
        };

        var bytes = exporter.Export("Ôn tập PRN212", messages);

        Assert.True(bytes.Length > 500);
        Assert.Equal("%PDF-", Encoding.ASCII.GetString(bytes, 0, 5));

        using var doc = PdfDocument.Open(bytes);
        Assert.True(doc.NumberOfPages >= 2, $"Expected multi-page PDF for long conversation, got {doc.NumberOfPages} page(s).");
        var allText = string.Join("\n", doc.GetPages().Select(p => p.Text));
        Assert.Contains("Ôn tập PRN212", allText);
        Assert.Contains("Dependency Injection", allText);
        Assert.Contains("Clean Architecture", allText);
        Assert.Contains("PRN212_Slides.pdf", allText);
        Assert.Contains("0.912", allText);
    }

    [Fact]
    public void ExporterHandlesEmptyConversationCleanly()
    {
        var exporter = new PdfPigConversationExporter();
        var bytes = exporter.Export("Empty Workspace", []);

        using var doc = PdfDocument.Open(bytes);
        Assert.Equal(1, doc.NumberOfPages);
        var text = doc.GetPage(1).Text;
        Assert.Contains("Empty Workspace", text);
    }

    [Fact]
    public void MainViewModelExportsPdfAndMarkdownByExtension()
    {
        using var fixture = new SqliteFixture();
        var providers = new FakeProviders();
        var exporter = new PdfPigConversationExporter();
        var vm = new MainViewModel(
            new(providers, new FakeParser(), new EfDocumentStore(fixture)),
            providers,
            new EfRepository<DocuMind.Domain.Entities.Workspace>(fixture),
            new EfChatHistoryStore(fixture),
            exporter);
        vm.SelectedWorkspace = new DocuMind.Domain.Entities.Workspace { Name = "Export WS" };
        vm.ChatMessages.Add(new()
        {
            Role = MessageRole.Assistant,
            Mode = ChatMode.Direct,
            Provider = "Gemini",
            Model = "gemini-3.5-flash",
            Temperature = 0.23,
            Content = "Nội dung trả lời thử nghiệm."
        });

        var pdfPath = fixture.File("export_test.pdf");
        var mdPath = fixture.File("export_test.md");

        vm.ExportToFile(pdfPath);
        vm.ExportToFile(mdPath);

        Assert.True(File.Exists(pdfPath));
        using (var doc = PdfDocument.Open(pdfPath))
        {
            var text = string.Join("\n", doc.GetPages().Select(p => p.Text));
            Assert.Contains("Export WS", text);
            Assert.Contains("Nội dung trả lời thử nghiệm.", text);
        }
        Assert.Contains("Temperature: 0.23", File.ReadAllText(mdPath));
    }
}
