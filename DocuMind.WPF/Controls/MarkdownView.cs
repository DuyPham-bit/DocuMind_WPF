using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace DocuMind.WPF.Controls;

// A text-only Markdown renderer: never executes HTML, scripts or remote links from model output.
public sealed class MarkdownView : RichTextBox
{
    public static readonly DependencyProperty MarkdownProperty = DependencyProperty.Register(nameof(Markdown), typeof(string),
        typeof(MarkdownView), new PropertyMetadata("", (owner, _) => ((MarkdownView)owner).Render()));
    public string Markdown { get => (string)GetValue(MarkdownProperty); set => SetValue(MarkdownProperty, value); }
    public MarkdownView()
    {
        IsReadOnly = true; IsDocumentEnabled = false; BorderThickness = new Thickness(0); Padding = new Thickness(0);
        Background = Brushes.Transparent; VerticalScrollBarVisibility = ScrollBarVisibility.Disabled;
    }
    private void Render()
    {
        var document = new FlowDocument { PagePadding = new Thickness(0), FontSize = 13, Foreground = Brushes.Black };
        var code = false;
        foreach (var line in (Markdown ?? "").Replace("\r", "").Split('\n'))
        {
            if (line.StartsWith("```")) { code = !code; continue; }
            var paragraph = new Paragraph { Margin = new Thickness(0, 2, 0, 4) };
            var text = line;
            if (code)
            {
                paragraph.FontFamily = new FontFamily("Consolas"); paragraph.Background = Brushes.WhiteSmoke;
                paragraph.Inlines.Add(new Run(text));
            }
            else
            {
                var heading = Regex.Match(text, @"^(#{1,6})\s+(.+)$");
                if (heading.Success) { text = heading.Groups[2].Value; paragraph.FontSize = 22 - heading.Groups[1].Length; paragraph.FontWeight = FontWeights.Bold; }
                if (text.StartsWith("- ") || text.StartsWith("* ")) text = "• " + text[2..];
                if (text.StartsWith("> ")) { text = text[2..]; paragraph.BorderBrush = Brushes.LightGray; paragraph.BorderThickness = new Thickness(3, 0, 0, 0); paragraph.Padding = new Thickness(8, 0, 0, 0); }
                foreach (var part in Regex.Split(text, @"(\*\*[^*]+\*\*|`[^`]+`|\*[^*]+\*)"))
                {
                    if (part.StartsWith("**") && part.EndsWith("**") && part.Length > 4) paragraph.Inlines.Add(new Bold(new Run(part[2..^2])));
                    else if (part.StartsWith('`') && part.EndsWith('`') && part.Length > 2) paragraph.Inlines.Add(new Run(part[1..^1]) { FontFamily = new FontFamily("Consolas"), Background = Brushes.WhiteSmoke });
                    else if (part.StartsWith('*') && part.EndsWith('*') && part.Length > 2) paragraph.Inlines.Add(new Italic(new Run(part[1..^1])));
                    else paragraph.Inlines.Add(new Run(part));
                }
            }
            document.Blocks.Add(paragraph);
        }
        Document = document;
    }
}
