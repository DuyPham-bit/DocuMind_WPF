using System.IO;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DocuMind.Application.Services;

namespace DocuMind.WPF.Localization;

/// <summary>
/// Quản lý chuyển đổi ngôn ngữ EN / VI trực tiếp trên toàn bộ giao diện WPF.
/// </summary>
public sealed partial class UiText : ObservableObject
{
    public static UiText Current { get; } = new();

    [ObservableProperty]
    private bool _isVietnamese;

    private string? _settingsPath;

    public static string DefaultSettingsPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DocuMind", "ui-settings.json");

    [RelayCommand]
    private void ToggleLanguage() => IsVietnamese = !IsVietnamese;

    partial void OnIsVietnameseChanged(bool value)
    {
        AppText.Vietnamese = value;
        // An empty property name tells WPF that every label changed, so new labels need no extra attribute.
        OnPropertyChanged(string.Empty);
        Save();
    }

    /// <summary>Loads the saved language and saves every later change. The app calls this once at startup;
    /// tests leave it off so they never touch the real settings file.</summary>
    public void EnablePersistence(string path)
    {
        _settingsPath = path;
        try
        {
            if (!File.Exists(path)) return;
            using var json = JsonDocument.Parse(File.ReadAllText(path));
            if (json.RootElement.TryGetProperty("language", out var language))
                IsVietnamese = language.GetString() == "vi";
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            // A missing or damaged settings file only means the default language is used.
        }
    }

    private void Save()
    {
        if (_settingsPath == null) return;
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(_settingsPath))!);
            File.WriteAllText(_settingsPath, JsonSerializer.Serialize(new { language = IsVietnamese ? "vi" : "en" }));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Not being able to remember the language must never break the app.
        }
    }

    public string LanguageButtonLabel => IsVietnamese ? "VI / EN" : "EN / VI";
    public string BrandSubtitle => IsVietnamese ? "TRỢ LÝ TRI THỨC AI" : "KNOWLEDGE INTELLIGENCE";
    public string WorkspaceHeader => IsVietnamese ? "KHÔNG GIAN LÀM VIỆC" : "WORKSPACE";
    public string WorkspaceSettingsHeader => IsVietnamese ? "Cài đặt workspace" : "Workspace settings";
    public string WorkspaceNameLabel => IsVietnamese ? "TÊN WORKSPACE" : "NAME";
    public string SystemInstructionLabel => IsVietnamese ? "CHỈ DẪN HỆ THỐNG" : "SYSTEM INSTRUCTION";
    public string NewButton => IsVietnamese ? "Tạo mới" : "New";
    public string SaveButton => IsVietnamese ? "Lưu" : "Save";
    public string DeleteButton => IsVietnamese ? "Xóa" : "Delete";
    public string UploadDocumentsButton => IsVietnamese ? "＋  Nạp tài liệu" : "＋  Upload documents";
    public string DocumentsHeader => IsVietnamese ? "TÀI LIỆU" : "DOCUMENTS";
    public string HistoryHeader => IsVietnamese ? "LỊCH SỬ" : "HISTORY";
    public string NoHistoryText => IsVietnamese ? "Chưa có cuộc trò chuyện nào được lưu." : "No saved conversations yet.";
    public string DeleteHistoryTooltip => IsVietnamese ? "Xóa cuộc trò chuyện" : "Delete conversation";
    public string MessagesSuffix => IsVietnamese ? "tin" : "msgs";
    public string DocIndexedLabel => IsVietnamese ? "Đã nạp · Sẵn sàng" : "Indexed · Ready";
    public string DocPendingLabel => IsVietnamese ? "Khác model embedding · Nạp lại ↻" : "Embedding changed · Re-index ↻";
    public string DocFailedLabel => IsVietnamese ? "Nạp lỗi · Thử lại ↻" : "Index failed · Retry ↻";
    public string DocIndexingLabel => IsVietnamese ? "Đang nạp…" : "Indexing…";
    public string ReindexTooltip => IsVietnamese ? "Bấm để nạp lại tài liệu này từ file gốc" : "Click to index this document again from its original file";
    public string RemoveDocTooltip => IsVietnamese ? "Xóa tài liệu" : "Remove document";
    public string AiProviderHeader => IsVietnamese ? "NỀN TẢNG AI" : "AI PROVIDER";
    public string ManageProvidersButton => IsVietnamese ? "Cài đặt API & Model" : "Manage providers";
    public string KnowledgeWorkspaceHeader => IsVietnamese ? "KHÔNG GIAN TRI THỨC" : "KNOWLEDGE WORKSPACE";
    public string SearchDocumentsLabel => IsVietnamese ? "Tra cứu tài liệu (RAG)" : "Search documents";
    public string QuizButton => IsVietnamese ? "Quiz ôn tập" : "Study quiz";
    public string QuizButtonTooltip => IsVietnamese
        ? "AI tạo câu trắc nghiệm từ tài liệu của workspace, app chấm điểm và lưu lịch sử"
        : "AI writes multiple-choice questions from this workspace's documents; the app grades them and keeps a history";
    public string ExportButton => IsVietnamese ? "Xuất MD" : "Export";
    public string NewChatButton => IsVietnamese ? "Chat mới" : "New chat";
    public string EmptyTitle => IsVietnamese ? "Sẵn sàng hỗ trợ bạn" : "Ready when you are";
    public string EmptySubtitle => IsVietnamese
        ? "Hãy nạp tài liệu ở cột trái, bật Tra cứu tài liệu (RAG) và đặt câu hỏi về nội dung file của bạn."
        : "Upload a document, keep Search documents enabled, and ask DocuMind a question about your files.";
    public string YouLabel => IsVietnamese ? "BẠN" : "YOU";
    public string AiAssistantLabel => IsVietnamese ? "TRỢ LÝ AI" : "AI ASSISTANT";
    public string SourcesHeader => IsVietnamese ? "NGUỒN TRÍCH DẪN" : "SOURCES";
    public string OpenSourceButton => IsVietnamese ? "Mở file nguồn" : "Open source";
    public string CancelButton => IsVietnamese ? "Hủy" : "Cancel";
    public string QuickStartHeader => IsVietnamese ? "GỢI Ý NHANH" : "QUICK START";
    public string InputTooltip => IsVietnamese ? "Nhập câu hỏi về tài liệu của bạn" : "Ask about your uploaded documents";
    public string SendButton => IsVietnamese ? "Gửi  →" : "Send  →";
    public string ComposerHint => IsVietnamese ? "Nhấn Enter để gửi · Shift + Enter để xuống dòng" : "Press Enter to send · Shift + Enter for a new line";

    // Settings Dialog
    public string SettingsTitle => IsVietnamese ? "Kết nối mô hình AI" : "Connect your models";
    public string SettingsSubtitle => IsVietnamese ? "API key được mã hóa bằng Windows DPAPI cho tài khoản máy này." : "Credentials are encrypted with Windows DPAPI for your account.";
    public string OpenRouterQuickTitle => IsVietnamese ? "THIẾT LẬP NHANH OPENROUTER" : "OPENROUTER QUICK SETUP";
    public string OpenRouterQuickDesc => IsVietnamese ? "Tự điền sẵn endpoint cùng cặp model chat & embedding miễn phí." : "Prefill the endpoint and recommended chat + embedding models.";
    public string UseOpenRouterButton => IsVietnamese ? "Dùng OpenRouter" : "Use OpenRouter";
    public string ProviderLabel => IsVietnamese ? "Nhà cung cấp (Provider)" : "Provider";
    public string ApiKeyLabel => "API key";
    public string ApiKeyHint => IsVietnamese ? "Chỉ lưu cục bộ trên máy này, mã hóa theo tài khoản Windows." : "Stored only on this device, encrypted for the current Windows user.";
    public string HoldToRevealButton => IsVietnamese ? "Giữ để xem" : "Hold to reveal";
    public string ChatModelLabel => IsVietnamese ? "Model Chat" : "Chat model";
    public string TemperatureLabel => "Temperature";
    public string EmbeddingModelLabel => IsVietnamese ? "Model Embedding" : "Embedding model";
    public string UseForEmbeddingLabel => IsVietnamese ? "Dùng provider này cho embedding tài liệu & RAG" : "Use this provider for document embeddings and RAG";
    public string ReindexHint => IsVietnamese ? "Đổi embedding model sẽ cần nạp lại tài liệu." : "Changing the embedding profile requires re-indexing your documents.";
    public string TestEmbeddingButton => IsVietnamese ? "Test embedding" : "Test embedding";
    public string BaseUrlLabel => "Base URL";
    public string TestConnectionButton => IsVietnamese ? "Kiểm tra kết nối" : "Test connection";
    public string SaveProviderButton => IsVietnamese ? "Lưu cấu hình" : "Save provider";

    // Quiz window (modal: language is fixed while it is open, so values are read once when it renders)
    public string QuizWindowTitle => IsVietnamese ? "DocuMind – Quiz ôn tập" : "DocuMind – Study quiz";
    public string QuizHeading => IsVietnamese ? "📝 Quiz ôn tập từ tài liệu" : "📝 Study quiz from your documents";
    public string QuizTopicLabel => IsVietnamese ? "Chủ đề:" : "Topic:";
    public string QuizTopicTooltip => IsVietnamese
        ? "Ví dụ: Dependency Injection, vòng đời DbContext... Để trống để lấy mẫu trong các tài liệu đã chọn"
        : "For example: Dependency Injection, DbContext lifetime... Leave empty to sample the selected documents";
    public string QuizCountLabel => IsVietnamese ? "Số câu:" : "Questions:";
    public string QuizGenerateButton => IsVietnamese ? "✨ Tạo quiz" : "✨ Generate quiz";
    public string QuizDocumentsHeader => IsVietnamese ? "Tài liệu làm quiz" : "Quiz documents";
    public string QuizSelectAllButton => IsVietnamese ? "Chọn tất cả" : "Select all";
    public string QuizClearSelectionButton => IsVietnamese ? "Bỏ chọn" : "Clear";
    public string QuizHistoryHeader => IsVietnamese ? "LỊCH SỬ ÔN TẬP" : "QUIZ HISTORY";
    public string QuizRetryButton => IsVietnamese ? "↺ Làm lại" : "↺ Try again";
    public string QuizSubmitButton => IsVietnamese ? "Nộp bài" : "Submit";
}
