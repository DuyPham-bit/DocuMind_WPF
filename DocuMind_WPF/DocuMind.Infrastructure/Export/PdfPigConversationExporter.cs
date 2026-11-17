using System.Text;
using DocuMind.Application.DTOs;
using DocuMind.Application.Interfaces;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.Core;
using UglyToad.PdfPig.Fonts.Standard14Fonts;
using UglyToad.PdfPig.Writer;

namespace DocuMind.Infrastructure.Export;

/// <summary>
/// Exports a workspace conversation with citations to an A4 PDF document using PdfPig.
/// </summary>
public sealed class PdfPigConversationExporter : IChatPdfExporter
{
    private const double MarginX = 46;
    private const double TopMargin = 48;
    private const double BottomMargin = 52;
    private const double ContentWidth = 595 - (MarginX * 2);

    private static readonly (string Regular, string Bold)[] FontCandidates =
    [
        (@"C:\Windows\Fonts\segoeui.ttf", @"C:\Windows\Fonts\segoeuib.ttf"),
        (@"C:\Windows\Fonts\arial.ttf", @"C:\Windows\Fonts\arialbd.ttf"),
        (@"C:\Windows\Fonts\tahoma.ttf", @"C:\Windows\Fonts\tahomabd.ttf")
    ];

    public byte[] Export(string workspaceName, IReadOnlyList<ChatPdfMessage> messages)
    {
        using var builder = new PdfDocumentBuilder();
        var (regularFont, boldFont, supportsUnicode) = LoadFonts(builder);
        var state = new LayoutState(builder, regularFont, boldFont, supportsUnicode);

        DrawHeader(state, workspaceName, messages.Count);

        if (messages.Count == 0)
        {
            state.EnsureSpace(24);
            state.DrawText("No messages in this conversation yet.", 10.5, bold: false, 107, 94, 80);
        }
        else
        {
            foreach (var message in messages)
            {
                DrawMessage(state, message);
            }
        }

        DrawFooters(state);
        return builder.Build();
    }

    private static (PdfDocumentBuilder.AddedFont Regular, PdfDocumentBuilder.AddedFont Bold, bool SupportsUnicode) LoadFonts(PdfDocumentBuilder builder)
    {
        foreach (var (regularPath, boldPath) in FontCandidates)
        {
            if (!File.Exists(regularPath) || !File.Exists(boldPath)) continue;
            var regularBytes = File.ReadAllBytes(regularPath);
            var boldBytes = File.ReadAllBytes(boldPath);
            if (builder.CanUseTrueTypeFont(regularBytes, out _) && builder.CanUseTrueTypeFont(boldBytes, out _))
            {
                return (builder.AddTrueTypeFont(regularBytes), builder.AddTrueTypeFont(boldBytes), true);
            }
        }

        return (builder.AddStandard14Font(Standard14Font.Helvetica), builder.AddStandard14Font(Standard14Font.HelveticaBold), false);
    }

    private static void DrawHeader(LayoutState state, string workspaceName, int messageCount)
    {
        var page = state.CurrentPage;
        const double bannerHeight = 58;
        var bannerBottom = state.CursorY - bannerHeight + 12;

        page.SetTextAndFillColor(250, 246, 238);
        page.DrawRectangle(new PdfPoint(MarginX, bannerBottom), ContentWidth, bannerHeight, 0, fill: true);
        page.SetStrokeColor(227, 215, 195);
        page.DrawRectangle(new PdfPoint(MarginX, bannerBottom), ContentWidth, bannerHeight, 0.8, fill: false);
        page.SetTextAndFillColor(180, 83, 9);
        page.DrawRectangle(new PdfPoint(MarginX, bannerBottom), 4, bannerHeight, 0, fill: true);

        var title = string.IsNullOrWhiteSpace(workspaceName) ? "DocuMind" : $"DocuMind — {workspaceName.Trim()}";
        state.DrawTextAt(MarginX + 14, state.CursorY - 6, title, 15, bold: true, 31, 26, 20);
        var meta = $"Exported {DateTime.Now:yyyy-MM-dd HH:mm}  ·  {messageCount} message(s)";
        state.DrawTextAt(MarginX + 14, state.CursorY - 26, meta, 9.5, bold: false, 107, 94, 80);
        page.ResetColor();

        state.CursorY = bannerBottom - 20;
    }

    private static void DrawMessage(LayoutState state, ChatPdfMessage message)
    {
        state.EnsureSpace(54);

        var senderColor = message.IsUser ? ((byte)154, (byte)52, (byte)18) : ((byte)31, (byte)26, (byte)20);
        var header = $"{message.SenderName}  ·  {message.Timestamp:yyyy-MM-dd HH:mm}";
        state.DrawWrappedBlock(header, 11, bold: true, senderColor.Item1, senderColor.Item2, senderColor.Item3, lineHeight: 15);

        var meta = $"Mode: {message.Mode} · Provider: {message.Provider} · Model: {message.Model} · Temperature: {message.Temperature:0.##}";
        state.DrawWrappedBlock(meta, 8.5, bold: false, 107, 94, 80, lineHeight: 12);
        state.CursorY -= 4;

        DrawBodyContent(state, message.Content);

        if (message.Sources.Count > 0)
        {
            state.CursorY -= 4;
            state.EnsureSpace(28);
            state.DrawText("Sources", 9.5, bold: true, 154, 74, 24);
            state.CursorY -= 13;

            foreach (var source in message.Sources)
            {
                var citation = $"• {source.FileName} · Page {source.PageNumber} · Score {source.Score:F3}";
                state.DrawWrappedBlock(citation, 9, bold: true, 74, 63, 53, lineHeight: 12.5, indent: 8);
                if (!string.IsNullOrWhiteSpace(source.Content))
                {
                    state.DrawWrappedBlock(source.Content, 8.8, bold: false, 107, 94, 80, lineHeight: 12, indent: 16);
                }
            }
        }

        state.CursorY -= 6;
        state.EnsureSpace(16);
        state.CurrentPage.SetStrokeColor(227, 215, 195);
        state.CurrentPage.DrawLine(new PdfPoint(MarginX, state.CursorY), new PdfPoint(MarginX + ContentWidth, state.CursorY), 0.7);
        state.CurrentPage.ResetColor();
        state.CursorY -= 16;
    }

    private static void DrawBodyContent(LayoutState state, string content)
    {
        if (string.IsNullOrWhiteSpace(content)) return;
        var lines = content.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
        foreach (var rawLine in lines)
        {
            var trimmed = rawLine.TrimEnd();
            if (string.IsNullOrWhiteSpace(trimmed))
            {
                state.CursorY -= 6;
                continue;
            }

            if (trimmed.StartsWith("### ", StringComparison.Ordinal) ||
                trimmed.StartsWith("## ", StringComparison.Ordinal) ||
                trimmed.StartsWith("# ", StringComparison.Ordinal))
            {
                var headingText = trimmed.TrimStart('#', ' ');
                state.DrawWrappedBlock(headingText, 11, bold: true, 120, 53, 15, lineHeight: 15);
                continue;
            }

            if (trimmed.StartsWith("- ", StringComparison.Ordinal) || trimmed.StartsWith("* ", StringComparison.Ordinal))
            {
                state.DrawWrappedBlock("• " + trimmed[2..].TrimStart(), 10, bold: false, 31, 26, 20, lineHeight: 14, indent: 10);
                continue;
            }

            state.DrawWrappedBlock(trimmed, 10, bold: false, 31, 26, 20, lineHeight: 14.2);
        }
    }

    private static void DrawFooters(LayoutState state)
    {
        var total = state.Pages.Count;
        for (var i = 0; i < total; i++)
        {
            var page = state.Pages[i];
            page.SetTextAndFillColor(107, 94, 80);
            page.AddText(state.Sanitize("DocuMind AI Studio"), 8.5, new PdfPoint(MarginX, 26), state.RegularFont);
            var label = $"Page {i + 1} / {total}";
            var width = state.Measure(page, label, 8.5, state.RegularFont);
            page.AddText(label, 8.5, new PdfPoint(MarginX + ContentWidth - width, 26), state.RegularFont);
            page.ResetColor();
        }
    }

    private sealed class LayoutState
    {
        private readonly PdfDocumentBuilder _builder;
        private readonly bool _supportsUnicode;
        public PdfDocumentBuilder.AddedFont RegularFont { get; }
        public PdfDocumentBuilder.AddedFont BoldFont { get; }
        public List<PdfPageBuilder> Pages { get; } = [];
        public PdfPageBuilder CurrentPage { get; private set; }
        public double CursorY { get; set; }

        public LayoutState(PdfDocumentBuilder builder, PdfDocumentBuilder.AddedFont regularFont,
            PdfDocumentBuilder.AddedFont boldFont, bool supportsUnicode)
        {
            _builder = builder;
            RegularFont = regularFont;
            BoldFont = boldFont;
            _supportsUnicode = supportsUnicode;
            CurrentPage = NewPage();
        }

        private PdfPageBuilder NewPage()
        {
            var page = _builder.AddPage(PageSize.A4, isPortrait: true);
            Pages.Add(page);
            CurrentPage = page;
            CursorY = page.PageSize.Top - TopMargin;
            return page;
        }

        public void EnsureSpace(double neededHeight)
        {
            if (CursorY - neededHeight < BottomMargin)
            {
                NewPage();
            }
        }

        public void DrawText(string text, double fontSize, bool bold, byte r, byte g, byte b, double indent = 0)
        {
            DrawTextAt(MarginX + indent, CursorY, text, fontSize, bold, r, g, b);
        }

        public void DrawTextAt(double x, double y, string text, double fontSize, bool bold, byte r, byte g, byte b)
        {
            var clean = Sanitize(text);
            if (string.IsNullOrEmpty(clean)) return;
            CurrentPage.SetTextAndFillColor(r, g, b);
            CurrentPage.AddText(clean, fontSize, new PdfPoint(x, y), bold ? BoldFont : RegularFont);
            CurrentPage.ResetColor();
        }

        public void DrawWrappedBlock(string text, double fontSize, bool bold, byte r, byte g, byte b,
            double lineHeight, double indent = 0)
        {
            var font = bold ? BoldFont : RegularFont;
            var maxWidth = Math.Max(60, ContentWidth - indent);
            foreach (var line in WrapText(Sanitize(text), fontSize, font, maxWidth))
            {
                EnsureSpace(lineHeight + 2);
                DrawText(line, fontSize, bold, r, g, b, indent);
                CursorY -= lineHeight;
            }
        }

        public double Measure(PdfPageBuilder page, string text, double fontSize, PdfDocumentBuilder.AddedFont font)
        {
            if (string.IsNullOrEmpty(text)) return 0;
            var letters = page.MeasureText(text, fontSize, new PdfPoint(0, 0), font);
            return letters.Count == 0 ? 0 : letters[^1].BoundingBox.Right;
        }

        private List<string> WrapText(string text, double fontSize, PdfDocumentBuilder.AddedFont font, double maxWidth)
        {
            var lines = new List<string>();
            if (string.IsNullOrWhiteSpace(text)) return lines;

            var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var current = new StringBuilder();
            foreach (var word in words)
            {
                var candidate = current.Length == 0 ? word : $"{current} {word}";
                if (Measure(CurrentPage, candidate, fontSize, font) <= maxWidth)
                {
                    current.Clear();
                    current.Append(candidate);
                    continue;
                }

                if (current.Length > 0)
                {
                    lines.Add(current.ToString());
                    current.Clear();
                }

                if (Measure(CurrentPage, word, fontSize, font) <= maxWidth)
                {
                    current.Append(word);
                    continue;
                }

                // Break unspaced long tokens (e.g., URLs or file paths) by character.
                foreach (var ch in word)
                {
                    var next = current.ToString() + ch;
                    if (current.Length > 0 && Measure(CurrentPage, next, fontSize, font) > maxWidth)
                    {
                        lines.Add(current.ToString());
                        current.Clear();
                    }
                    current.Append(ch);
                }
            }

            if (current.Length > 0) lines.Add(current.ToString());
            return lines;
        }

        public string Sanitize(string input)
        {
            if (string.IsNullOrEmpty(input)) return "";
            var sb = new StringBuilder(input.Length);
            foreach (var ch in input)
            {
                if (ch == '\t') sb.Append("  ");
                else if (char.IsControl(ch)) continue;
                else if (!_supportsUnicode && ch > 0x00FF) sb.Append('?');
                else sb.Append(ch);
            }
            return sb.ToString();
        }
    }
}
