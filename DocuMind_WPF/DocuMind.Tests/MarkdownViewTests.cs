using System.Windows.Documents;
using System.Windows.Threading;
using DocuMind.WPF.Controls;

namespace DocuMind.Tests;

public class MarkdownViewTests
{
    [Fact]
    public async Task MarkdownView_RendersTablesDividersCodeBlocksAndListsCleanly()
    {
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var thread = new Thread(() =>
        {
            try
            {
                var view = new MarkdownView
                {
                    Markdown = """
                        ---
                        ## 2. Kiểu dữ liệu và Bộ nhớ

                        | Đặc điểm | Value Types | Reference Types |
                        | :--- | :--- | :--- |
                        | **Ví dụ** | `int`, `bool` | `string`, `class` |

                        - Mục chính
                          - Mục con

                        ```csharp
                        int x = 42;
                        ```

                        ---
                        """
                };

                var blocks = view.Document.Blocks.ToList();
                var text = new TextRange(view.Document.ContentStart, view.Document.ContentEnd).Text;

                Assert.Contains(blocks, b => b is Table);
                Assert.DoesNotContain("| :--- |", text);
                Assert.DoesNotContain("---", text);

                var table = blocks.OfType<Table>().Single();
                Assert.Equal(2, table.RowGroups[0].Rows.Count);
                Assert.Contains("Value Types", text);
                Assert.Contains("int, bool", text);
                Assert.Contains("int x = 42;", text);
                Assert.Contains("• Mục chính", text);

                completion.SetResult();
            }
            catch (Exception ex) { completion.SetException(ex); }
            finally { Dispatcher.CurrentDispatcher.InvokeShutdown(); }
        }) { IsBackground = true };

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        await completion.Task.WaitAsync(TimeSpan.FromSeconds(10));
    }
}
