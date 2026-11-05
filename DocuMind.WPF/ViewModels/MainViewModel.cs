using System.Collections.ObjectModel;
using System.IO;
using System.Runtime.Versioning;
using System.Text;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DocuMind.Application.DTOs;
using DocuMind.Application.Interfaces;
using DocuMind.Application.Services;
using DocuMind.Domain.Entities;
using DocuMind.Domain.Enums;
using DocuMind.WPF.Models;
using Microsoft.Win32;

namespace DocuMind.WPF.ViewModels;

[SupportedOSPlatform("windows")]
public partial class MainViewModel : ObservableObject
{
    private readonly RagOrchestratorService _ragService;
    private readonly IChatProviderFactory _providerFactory;
    private readonly IRepository<Workspace> _workspaceRepo;

    public ObservableCollection<Workspace> Workspaces { get; } = new();
    public ObservableCollection<ChatMessageDisplayModel> ChatMessages { get; } = new();
    public ObservableCollection<string> UploadedFiles { get; } = new();
    public ObservableCollection<PromptTemplate> PromptTemplates { get; } = new();

    [ObservableProperty]
    private Workspace? _selectedWorkspace;

    [ObservableProperty]
    private string _userInput = string.Empty;

    [ObservableProperty]
    private string _systemPrompt = "Bạn là trợ lý học tập và nghiên cứu tài liệu chuẩn xác. Hãy trả lời súc tích, mạch lạc và bám sát tài liệu.";

    [ObservableProperty]
    private bool _useRagMode = true;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string _busyStatus = string.Empty;

    [ObservableProperty]
    private string _activeProviderInfo = "Chưa thiết lập Provider";

    public Action? OpenSettingsAction { get; set; }

    public MainViewModel(
        RagOrchestratorService ragService,
        IChatProviderFactory providerFactory,
        IRepository<Workspace> workspaceRepo)
    {
        _ragService = ragService;
        _providerFactory = providerFactory;
        _workspaceRepo = workspaceRepo;

        InitializeTemplates();
    }

    private void InitializeTemplates()
    {
        PromptTemplates.Add(new PromptTemplate(
            "📌 Tóm tắt tài liệu",
            "Tóm tắt ngắn gọn các ý chính trong tài liệu vừa nạp.",
            "Hãy tóm tắt ngắn gọn và súc tích nội dung chính của các tài liệu trong workspace này theo dạng gạch đầu dòng."));

        PromptTemplates.Add(new PromptTemplate(
            "❓ Tạo câu hỏi ôn tập",
            "Tạo 5 câu hỏi trắc nghiệm kiểm tra kiến thức từ tài liệu.",
            "Dựa vào nội dung tài liệu, hãy tạo 5 câu hỏi ôn tập trắc nghiệm (có 4 đáp án A, B, C, D) kèm đáp án đúng và giải thích ngắn gọn."));

        PromptTemplates.Add(new PromptTemplate(
            "💡 Giải thích khái niệm",
            "Làm rõ các định nghĩa hoặc thuật ngữ trọng tâm.",
            "Hãy liệt kê và giải thích 3-5 thuật ngữ hoặc khái niệm quan trọng nhất được nhắc đến trong tài liệu bằng ngôn ngữ dễ hiểu."));

        PromptTemplates.Add(new PromptTemplate(
            "🔍 Phân tích ưu/nhược điểm",
            "Đánh giá các khía cạnh nổi bật trong văn bản.",
            "Dựa vào thông tin trong tài liệu, hãy phân tích các ưu điểm, nhược điểm và rủi ro tiềm ẩn được đề cập."));
    }

    public async Task InitializeAsync()
    {
        IsBusy = true;
        BusyStatus = "Đang khởi tạo DocuMind...";

        try
        {
            var workspaces = await _workspaceRepo.GetAllAsync();
            Workspaces.Clear();
            foreach (var ws in workspaces)
            {
                Workspaces.Add(ws);
            }

            if (Workspaces.Count > 0)
            {
                SelectedWorkspace = Workspaces[0];
            }

            await RefreshActiveProviderInfoAsync();

            // Lời chào mở đầu
            ChatMessages.Add(new ChatMessageDisplayModel
            {
                Role = MessageRole.Assistant,
                Content = "Xin chào! Tôi là **DocuMind Assistant**.\n\nBạn có thể nạp các file **PDF, TXT, MD** vào Không gian làm việc bên trái để tra cứu ngữ cảnh chuẩn xác kèm trích dẫn nguồn, hoặc bật/tắt chế độ **RAG** để chat trực tiếp với AI.",
                Timestamp = DateTime.Now
            });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Khởi tạo thất bại: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task RefreshActiveProviderInfoAsync()
    {
        try
        {
            var active = await _providerFactory.GetActiveSettingAsync();
            if (active != null)
            {
                ActiveProviderInfo = $"{active.ProviderType} ({active.ChatModel})";
            }
            else
            {
                ActiveProviderInfo = "Chưa cấu hình API Key (Bấm Cài đặt)";
            }
        }
        catch
        {
            ActiveProviderInfo = "Lỗi đọc Provider";
        }
    }

    async partial void OnSelectedWorkspaceChanged(Workspace? value)
    {
        if (value == null) return;
        SystemPrompt = value.SystemPrompt;
        await ReloadWorkspaceFilesAsync(value.Id);
    }

    private async Task ReloadWorkspaceFilesAsync(Guid workspaceId)
    {
        try
        {
            var files = await _ragService.GetWorkspaceFilesAsync(workspaceId);
            UploadedFiles.Clear();
            foreach (var f in files)
            {
                UploadedFiles.Add(f);
            }
        }
        catch { }
    }

    [RelayCommand]
    private async Task UploadDocumentAsync()
    {
        if (SelectedWorkspace == null) return;

        var dialog = new OpenFileDialog
        {
            Title = "Chọn tài liệu nạp vào Workspace",
            Filter = "Tài liệu học tập (*.pdf;*.txt;*.md;*.json;*.cs)|*.pdf;*.txt;*.md;*.json;*.cs|Tất cả tệp (*.*)|*.*",
            Multiselect = true
        };

        if (dialog.ShowDialog() == true)
        {
            IsBusy = true;
            var progress = new Progress<string>(msg => BusyStatus = msg);

            try
            {
                foreach (var file in dialog.FileNames)
                {
                    await _ragService.IngestDocumentAsync(SelectedWorkspace.Id, file, progress);
                }

                await ReloadWorkspaceFilesAsync(SelectedWorkspace.Id);

                ChatMessages.Add(new ChatMessageDisplayModel
                {
                    Role = MessageRole.Assistant,
                    Content = $"✅ Đã bóc tách văn bản, tính toán vector embedding và lưu trữ thành công {dialog.FileNames.Length} tệp vào Không gian làm việc.",
                    Timestamp = DateTime.Now
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi nạp tài liệu: {ex.Message}", "Lỗi Nạp File", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            finally
            {
                IsBusy = false;
                BusyStatus = string.Empty;
            }
        }
    }

    [RelayCommand]
    private async Task DeleteDocumentAsync(string fileName)
    {
        if (SelectedWorkspace == null || string.IsNullOrWhiteSpace(fileName)) return;

        var confirm = MessageBox.Show(
            $"Bạn có chắc chắn muốn xóa tài liệu '{fileName}' khỏi không gian này?",
            "Xác nhận xóa",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (confirm == MessageBoxResult.Yes)
        {
            await _ragService.DeleteDocumentAsync(SelectedWorkspace.Id, fileName);
            UploadedFiles.Remove(fileName);
            ChatMessages.Add(new ChatMessageDisplayModel
            {
                Role = MessageRole.Assistant,
                Content = $"Đã xóa các đoạn vector liên quan tới tệp '{fileName}'.",
                Timestamp = DateTime.Now
            });
        }
    }

    [RelayCommand]
    private async Task SendMessageAsync()
    {
        if (string.IsNullOrWhiteSpace(UserInput) || IsBusy || SelectedWorkspace == null)
            return;

        var question = UserInput.Trim();
        UserInput = string.Empty;

        // Thêm tin nhắn của User
        ChatMessages.Add(new ChatMessageDisplayModel
        {
            Role = MessageRole.User,
            Content = question,
            Timestamp = DateTime.Now
        });

        IsBusy = true;
        BusyStatus = UseRagMode ? "Đang tìm kiếm ngữ cảnh & gọi AI..." : "Đang hỏi AI trực tiếp...";

        try
        {
            if (UseRagMode)
            {
                ChatResultDto result = await _ragService.ChatWithRagAsync(
                    SelectedWorkspace.Id,
                    SystemPrompt,
                    question);

                ChatMessages.Add(new ChatMessageDisplayModel
                {
                    Role = MessageRole.Assistant,
                    Content = result.ResponseText,
                    Sources = result.Sources,
                    Timestamp = DateTime.Now
                });
            }
            else
            {
                string reply = await _ragService.ChatExternalAsync(SystemPrompt, question);
                ChatMessages.Add(new ChatMessageDisplayModel
                {
                    Role = MessageRole.Assistant,
                    Content = reply,
                    Timestamp = DateTime.Now
                });
            }
        }
        catch (Exception ex)
        {
            ChatMessages.Add(new ChatMessageDisplayModel
            {
                Role = MessageRole.Assistant,
                Content = $"⚠️ Lỗi khi phản hồi: {ex.Message}\n\n*Gợi ý: Mở Cài đặt để kiểm tra API Key hoặc chuyển sang Provider khác.*",
                Timestamp = DateTime.Now
            });
        }
        finally
        {
            IsBusy = false;
            BusyStatus = string.Empty;
        }
    }

    [RelayCommand]
    private void ApplyTemplate(PromptTemplate template)
    {
        if (template != null)
        {
            UserInput = template.Prompt;
        }
    }

    [RelayCommand]
    private void ClearChat()
    {
        ChatMessages.Clear();
        ChatMessages.Add(new ChatMessageDisplayModel
        {
            Role = MessageRole.Assistant,
            Content = "Đã xóa lịch sử trò chuyện phiên này.",
            Timestamp = DateTime.Now
        });
    }

    [RelayCommand]
    private void ExportMarkdown()
    {
        if (ChatMessages.Count == 0)
        {
            MessageBox.Show("Chưa có tin nhắn nào để xuất.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var saveDialog = new SaveFileDialog
        {
            Title = "Xuất lịch sử trò chuyện",
            Filter = "Markdown Document (*.md)|*.md|Text Document (*.txt)|*.txt",
            FileName = $"DocuMind_Chat_{DateTime.Now:yyyyMMdd_HHmmss}.md"
        };

        if (saveDialog.ShowDialog() == true)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"# DocuMind Chat Export - {SelectedWorkspace?.Name ?? "Workspace"}");
            sb.AppendLine($"*Thời gian xuất: {DateTime.Now:yyyy-MM-dd HH:mm:ss}*");
            sb.AppendLine($"*Chế độ: {(UseRagMode ? "RAG (Hỏi đáp tài liệu)" : "Direct LLM Chat")}*");
            sb.AppendLine();
            sb.AppendLine("---");
            sb.AppendLine();

            foreach (var msg in ChatMessages)
            {
                sb.AppendLine($"### [{msg.FormattedTime}] {msg.SenderName}:");
                sb.AppendLine(msg.Content);

                if (msg.HasSources)
                {
                    sb.AppendLine();
                    sb.AppendLine("**Nguồn trích dẫn:**");
                    foreach (var s in msg.Sources)
                    {
                        sb.AppendLine($"- `{s.FileName}` (Trang {s.PageNumber}, Độ khớp: {s.Score:P0})");
                    }
                }
                sb.AppendLine();
                sb.AppendLine("---");
                sb.AppendLine();
            }

            File.WriteAllText(saveDialog.FileName, sb.ToString(), Encoding.UTF8);
            MessageBox.Show($"Đã xuất thành công tại:\n{saveDialog.FileName}", "Xuất Markdown", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    [RelayCommand]
    private void OpenSettings()
    {
        OpenSettingsAction?.Invoke();
    }
}

