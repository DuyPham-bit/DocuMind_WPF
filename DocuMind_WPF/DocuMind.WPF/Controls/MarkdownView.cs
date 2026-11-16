using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace DocuMind.WPF.Controls;

// A text-only Markdown renderer: never executes HTML, scripts or remote links from model output.
public sealed class MarkdownView : RichTextBox
{
    private static readonly Brush TextBrush = CreateBrush(0x1F, 0x1A, 0x14);
    private static readonly Brush CodeBackgroundBrush = CreateBrush(0xF3, 0xEB, 0xDC);
    private static readonly Brush CodeForegroundBrush = CreateBrush(0x7C, 0x2D, 0x12);
    private static readonly Brush QuoteBorderBrush = CreateBrush(0xB4, 0x53, 0x09);

    public static readonly DependencyProperty MarkdownProperty = DependencyProperty.Register(nameof(Markdown), typeof(string),
        typeof(MarkdownView), new PropertyMetadata("", (owner, _) => ((MarkdownView)owner).Render()));
    public string Markdown { get => (string)GetValue(MarkdownProperty); set => SetValue(MarkdownProperty, value); }

    public MarkdownView()
    {
        IsReadOnly = true; IsDocumentEnabled = false; BorderThickness = new Thickness(0); Padding = new Thickness(0);
        Background = Brushes.Transparent; Foreground = TextBrush;
        VerticalScrollBarVisibility = ScrollBarVisibility.Disabled;
    }

    private void Render()
    {
        var document = new FlowDocument { PagePadding = new Thickness(0), FontSize = 13, Foreground = TextBrush };
        var code = false;
        foreach (var line in (Markdown ?? "").Replace("\r", "").Split('\n'))
        {
            if (line.StartsWith("```")) { code = !code; continue; }
            var paragraph = new Paragraph { Margin = new Thickness(0, 2, 0, 5) };
            var text = line;
            if (code)
            {
                paragraph.FontFamily = new FontFamily("Consolas");
                paragraph.Background = CodeBackgroundBrush;
                paragraph.Foreground = CodeForegroundBrush;
                paragraph.Padding = new Thickness(8, 3, 8, 3);
                paragraph.Inlines.Add(new Run(text));
            }
            else
            {
                var heading = Regex.Match(text, @"^(#{1,6})\s+(.+)$");
                if (heading.Success)
                {
                    text = heading.Groups[2].Value;
                    paragraph.FontSize = 21 - heading.Groups[1].Length;
                    paragraph.FontWeight = FontWeights.SemiBold;
                }
                if (text.StartsWith("- ") || text.StartsWith("* ")) text = "• " + text[2..];
                if (text.StartsWith("> "))
                {
                    text = text[2..];
                    paragraph.BorderBrush = QuoteBorderBrush;
                    paragraph.BorderThickness = new Thickness(3, 0, 0, 0);
                    paragraph.Padding = new Thickness(10, 2, 0, 2);
                }
                foreach (var part in Regex.Split(text, @"(\*\*[^*]+\*\*|`[^`]+`|\*[^*]+\*)"))
                {
                    if (part.StartsWith("**") && part.EndsWith("**") && part.Length > 4)
                        paragraph.Inlines.Add(new Bold(new Run(part[2..^2])));
                    else if (part.StartsWith('`') && part.EndsWith('`') && part.Length > 2)
                        paragraph.Inlines.Add(new Run(part[1..^1]) { FontFamily = new FontFamily("Consolas"), Background = CodeBackgroundBrush, Foreground = CodeForegroundBrush });
                    else if (part.StartsWith('*') && part.EndsWith('*') && part.Length > 2)
                        paragraph.Inlines.Add(new Italic(new Run(part[1..^1])));
                    else
                        paragraph.Inlines.Add(new Run(part));
                }
            }
            document.Blocks.Add(paragraph);
        }
        Document = document;
    }

    private static SolidColorBrush CreateBrush(byte r, byte g, byte b)
    {
        var brush = new SolidColorBrush(Color.FromRgb(r, g, b));
        brush.Freeze();
        return brush;
    }
}
