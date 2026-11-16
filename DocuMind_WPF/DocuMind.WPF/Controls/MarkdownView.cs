using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace DocuMind.WPF.Controls;

// A text-only Markdown renderer: never executes HTML, scripts or remote links from model output.
public sealed partial class MarkdownView : RichTextBox
{
    private static readonly Brush TextBrush = CreateBrush(0x1F, 0x1A, 0x14);
    private static readonly Brush MutedBrush = CreateBrush(0x7B, 0x6A, 0x56);
    private static readonly Brush AccentBrush = CreateBrush(0x9A, 0x34, 0x12);
    private static readonly Brush DividerBrush = CreateBrush(0xE5, 0xDA, 0xC7);
    private static readonly Brush TableHeaderBrush = CreateBrush(0xF3, 0xEB, 0xDC);
    private static readonly Brush TableAltRowBrush = CreateBrush(0xFA, 0xF5, 0xEC);
    private static readonly Brush TableRowBrush = CreateBrush(0xFF, 0xFD, 0xF9);
    private static readonly Brush TableBorderBrush = CreateBrush(0xDF, 0xD1, 0xB8);
    private static readonly Brush CodeBackgroundBrush = CreateBrush(0xF5, 0xEF, 0xE2);
    private static readonly Brush CodeForegroundBrush = CreateBrush(0x7C, 0x2D, 0x12);
    private static readonly Brush QuoteBackgroundBrush = CreateBrush(0xFA, 0xF5, 0xEB);
    private static readonly Brush QuoteBorderBrush = CreateBrush(0xB4, 0x53, 0x09);

    private static readonly FontFamily MonoFont = new("Consolas");

    public static readonly DependencyProperty MarkdownProperty = DependencyProperty.Register(nameof(Markdown), typeof(string),
        typeof(MarkdownView), new PropertyMetadata("", (owner, _) => ((MarkdownView)owner).Render()));
    public string Markdown { get => (string)GetValue(MarkdownProperty); set => SetValue(MarkdownProperty, value); }

    public MarkdownView()
    {
        IsReadOnly = true;
        IsDocumentEnabled = false;
        BorderThickness = new Thickness(0);
        Padding = new Thickness(0);
        Background = Brushes.Transparent;
        Foreground = TextBrush;
        VerticalScrollBarVisibility = ScrollBarVisibility.Disabled;
    }

    private void Render()
    {
        var document = new FlowDocument
        {
            PagePadding = new Thickness(0),
            FontSize = 13.5,
            LineHeight = 22,
            Foreground = TextBrush
        };

        var lines = (Markdown ?? "").Replace("\r", "").Split('\n');
        for (var i = 0; i < lines.Length; i++)
        {
            var raw = lines[i];
            var trimmed = raw.Trim();

            if (string.IsNullOrEmpty(trimmed))
            {
                continue;
            }

            if (trimmed.StartsWith("```", StringComparison.Ordinal))
            {
                document.Blocks.Add(BuildCodeBlock(lines, ref i, trimmed[3..].Trim()));
                continue;
            }

            if (IsHorizontalRule(trimmed))
            {
                document.Blocks.Add(BuildDivider());
                continue;
            }

            if (IsTableHeader(lines, i))
            {
                document.Blocks.Add(BuildTable(lines, ref i));
                continue;
            }

            document.Blocks.Add(BuildTextParagraph(raw, trimmed));
        }

        Document = document;
    }

    private static Section BuildCodeBlock(string[] lines, ref int index, string language)
    {
        var section = new Section
        {
            Background = CodeBackgroundBrush,
            BorderBrush = TableBorderBrush,
            BorderThickness = new Thickness(1),
            Padding = new Thickness(12, 9, 12, 9),
            Margin = new Thickness(0, 6, 0, 10)
        };

        if (!string.IsNullOrWhiteSpace(language))
        {
            section.Blocks.Add(new Paragraph(new Run(language.ToUpperInvariant()))
            {
                FontSize = 9.5,
                FontWeight = FontWeights.Bold,
                Foreground = MutedBrush,
                Margin = new Thickness(0, 0, 0, 4),
                LineHeight = 14
            });
        }

        var codeLines = new List<string>();
        while (index + 1 < lines.Length)
        {
            index++;
            if (lines[index].TrimStart().StartsWith("```", StringComparison.Ordinal))
            {
                break;
            }
            codeLines.Add(lines[index]);
        }

        var codeParagraph = new Paragraph(new Run(string.Join("\n", codeLines)))
        {
            FontFamily = MonoFont,
            FontSize = 12.5,
            LineHeight = 19,
            Foreground = CodeForegroundBrush,
            Margin = new Thickness(0)
        };
        section.Blocks.Add(codeParagraph);
        return section;
    }

    private static Paragraph BuildDivider() => new()
    {
        BorderBrush = DividerBrush,
        BorderThickness = new Thickness(0, 1, 0, 0),
        Margin = new Thickness(0, 10, 0, 10),
        Padding = new Thickness(0),
        FontSize = 1,
        LineHeight = 1
    };

    private static bool IsHorizontalRule(string trimmed)
        => trimmed.Length >= 3 && (trimmed.All(c => c == '-') || trimmed.All(c => c == '*') || trimmed.All(c => c == '_'));

    private static bool IsTableHeader(string[] lines, int index)
    {
        if (index + 1 >= lines.Length) return false;
        var header = lines[index].Trim();
        var separator = lines[index + 1].Trim();
        return header.Contains('|') && IsTableSeparatorLine(separator);
    }

    private static bool IsTableSeparatorLine(string line)
    {
        if (!line.Contains('|') || !line.Contains('-')) return false;
        var cells = SplitTableRow(line);
        return cells.Count > 0 && cells.All(c => TableSeparatorRegex().IsMatch(c.Trim()));
    }

    private static Table BuildTable(string[] lines, ref int index)
    {
        var headers = SplitTableRow(lines[index]);
        var alignments = SplitTableRow(lines[index + 1]).Select(ParseAlignment).ToList();
        index += 1;

        var columnCount = Math.Max(headers.Count, alignments.Count);
        var table = new Table
        {
            CellSpacing = 0,
            BorderBrush = TableBorderBrush,
            BorderThickness = new Thickness(1),
            Margin = new Thickness(0, 6, 0, 12)
        };

        for (var c = 0; c < columnCount; c++)
        {
            table.Columns.Add(new TableColumn());
        }

        var group = new TableRowGroup();
        group.Rows.Add(BuildTableRow(headers, alignments, columnCount, isHeader: true, isAlt: false));

        var bodyRowIndex = 0;
        while (index + 1 < lines.Length)
        {
            var next = lines[index + 1].Trim();
            if (string.IsNullOrEmpty(next) || !next.Contains('|') || next.StartsWith("```", StringComparison.Ordinal))
            {
                break;
            }

            index++;
            var cells = SplitTableRow(next);
            group.Rows.Add(BuildTableRow(cells, alignments, columnCount, isHeader: false, isAlt: bodyRowIndex % 2 == 1));
            bodyRowIndex++;
        }

        table.RowGroups.Add(group);
        return table;
    }

    private static TableRow BuildTableRow(
        IReadOnlyList<string> cells,
        IReadOnlyList<TextAlignment> alignments,
        int columnCount,
        bool isHeader,
        bool isAlt)
    {
        var row = new TableRow
        {
            Background = isHeader ? TableHeaderBrush : (isAlt ? TableAltRowBrush : TableRowBrush)
        };

        for (var c = 0; c < columnCount; c++)
        {
            var content = c < cells.Count ? cells[c] : string.Empty;
            var paragraph = new Paragraph
            {
                Margin = new Thickness(0),
                FontSize = 12.5,
                LineHeight = 19,
                FontWeight = isHeader ? FontWeights.Bold : FontWeights.Normal,
                TextAlignment = c < alignments.Count ? alignments[c] : TextAlignment.Left
            };
            AppendFormattedInlines(paragraph, content);

            row.Cells.Add(new TableCell(paragraph)
            {
                BorderBrush = DividerBrush,
                BorderThickness = new Thickness(0, 0, c < columnCount - 1 ? 1 : 0, 1),
                Padding = new Thickness(10, 7, 10, 7)
            });
        }

        return row;
    }

    private static List<string> SplitTableRow(string line)
    {
        var trimmed = line.Trim();
        if (trimmed.StartsWith('|')) trimmed = trimmed[1..];
        if (trimmed.EndsWith('|')) trimmed = trimmed[..^1];
        return trimmed.Split('|').Select(cell => cell.Trim()).ToList();
    }

    private static TextAlignment ParseAlignment(string cell)
    {
        var trimmed = cell.Trim();
        var left = trimmed.StartsWith(':');
        var right = trimmed.EndsWith(':');
        if (left && right) return TextAlignment.Center;
        if (right) return TextAlignment.Right;
        return TextAlignment.Left;
    }

    private static Paragraph BuildTextParagraph(string raw, string trimmed)
    {
        var paragraph = new Paragraph
        {
            Margin = new Thickness(0, 2, 0, 6),
            LineHeight = 22
        };

        var heading = HeadingRegex().Match(trimmed);
        if (heading.Success)
        {
            var level = heading.Groups[1].Value.Length;
            paragraph.FontSize = Math.Max(14, 21 - level * 1.3);
            paragraph.FontWeight = FontWeights.Bold;
            paragraph.Margin = new Thickness(0, level <= 2 ? 12 : 8, 0, 6);
            if (level <= 2)
            {
                paragraph.BorderBrush = DividerBrush;
                paragraph.BorderThickness = new Thickness(0, 0, 0, 1);
                paragraph.Padding = new Thickness(0, 0, 0, 4);
            }
            AppendFormattedInlines(paragraph, heading.Groups[2].Value);
            return paragraph;
        }

        if (trimmed.StartsWith("> ", StringComparison.Ordinal))
        {
            paragraph.Background = QuoteBackgroundBrush;
            paragraph.BorderBrush = QuoteBorderBrush;
            paragraph.BorderThickness = new Thickness(3, 0, 0, 0);
            paragraph.Padding = new Thickness(12, 6, 10, 6);
            paragraph.Margin = new Thickness(0, 4, 0, 8);
            AppendFormattedInlines(paragraph, trimmed[2..]);
            return paragraph;
        }

        var leadingSpaces = raw.Length - raw.TrimStart(' ').Length;
        var indent = Math.Min(leadingSpaces / 2, 4) * 16;

        var numbered = NumberedListRegex().Match(trimmed);
        if (numbered.Success)
        {
            paragraph.Margin = new Thickness(indent + 8, 2, 0, 4);
            paragraph.Inlines.Add(new Bold(new Run(numbered.Groups[1].Value + " ")) { Foreground = AccentBrush });
            AppendFormattedInlines(paragraph, numbered.Groups[2].Value);
            return paragraph;
        }

        if (trimmed.StartsWith("- ", StringComparison.Ordinal) || trimmed.StartsWith("* ", StringComparison.Ordinal))
        {
            paragraph.Margin = new Thickness(indent + 10, 2, 0, 4);
            paragraph.Inlines.Add(new Run("• ") { FontWeight = FontWeights.Bold, Foreground = AccentBrush });
            AppendFormattedInlines(paragraph, trimmed[2..]);
            return paragraph;
        }

        AppendFormattedInlines(paragraph, trimmed);
        return paragraph;
    }

    private static void AppendFormattedInlines(Paragraph paragraph, string text)
    {
        foreach (var part in InlineTokenRegex().Split(text))
        {
            if (string.IsNullOrEmpty(part)) continue;

            if (part.StartsWith("**", StringComparison.Ordinal) && part.EndsWith("**", StringComparison.Ordinal) && part.Length > 4)
            {
                paragraph.Inlines.Add(new Bold(new Run(part[2..^2])));
            }
            else if (part.StartsWith('`') && part.EndsWith('`') && part.Length > 2)
            {
                paragraph.Inlines.Add(new Run(part[1..^1])
                {
                    FontFamily = MonoFont,
                    FontSize = 12,
                    Background = CodeBackgroundBrush,
                    Foreground = CodeForegroundBrush
                });
            }
            else if (part.StartsWith('*') && part.EndsWith('*') && part.Length > 2)
            {
                paragraph.Inlines.Add(new Italic(new Run(part[1..^1])));
            }
            else if (part.StartsWith("[Source:", StringComparison.OrdinalIgnoreCase) && part.EndsWith(']'))
            {
                paragraph.Inlines.Add(new Bold(new Run(part)) { Foreground = AccentBrush });
            }
            else
            {
                paragraph.Inlines.Add(new Run(part));
            }
        }
    }

    private static SolidColorBrush CreateBrush(byte r, byte g, byte b)
    {
        var brush = new SolidColorBrush(Color.FromRgb(r, g, b));
        brush.Freeze();
        return brush;
    }

    [GeneratedRegex(@"^(#{1,6})\s+(.+)$")]
    private static partial Regex HeadingRegex();

    [GeneratedRegex(@"^(\d+\.)\s+(.+)$")]
    private static partial Regex NumberedListRegex();

    [GeneratedRegex(@"^:?-{2,}:?$")]
    private static partial Regex TableSeparatorRegex();

    [GeneratedRegex(@"(\*\*[^*]+\*\*|`[^`]+`|\*[^*]+\*|\[Source:[^\]]+\])", RegexOptions.IgnoreCase)]
    private static partial Regex InlineTokenRegex();
}
