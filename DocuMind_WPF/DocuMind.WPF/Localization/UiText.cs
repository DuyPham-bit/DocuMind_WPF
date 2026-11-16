using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace DocuMind.WPF.Localization;

/// <summary>
/// Quản lý chuyển đổi ngôn ngữ EN / VI trực tiếp trên toàn bộ giao diện WPF.
/// </summary>
public sealed partial class UiText : ObservableObject
{
    public static UiText Current { get; } = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(LanguageButtonLabel))]
    [NotifyPropertyChangedFor(nameof(BrandSubtitle))]
    [NotifyPropertyChangedFor(nameof(WorkspaceHeader))]
    [NotifyPropertyChangedFor(nameof(WorkspaceSettingsHeader))]
    [NotifyPropertyChangedFor(nameof(WorkspaceNameLabel))]
    [NotifyPropertyChangedFor(nameof(SystemInstructionLabel))]
    [NotifyPropertyChangedFor(nameof(NewButton))]
    [NotifyPropertyChangedFor(nameof(SaveButton))]
    [NotifyPropertyChangedFor(nameof(DeleteButton))]
    [NotifyPropertyChangedFor(nameof(UploadDocumentsButton))]
    [NotifyPropertyChangedFor(nameof(DocumentsHeader))]
    [NotifyPropertyChangedFor(nameof(DocIndexedLabel))]
    [NotifyPropertyChangedFor(nameof(DocPendingLabel))]
    [NotifyPropertyChangedFor(nameof(RemoveDocTooltip))]
    [NotifyPropertyChangedFor(nameof(AiProviderHeader))]
    [NotifyPropertyChangedFor(nameof(ManageProvidersButton))]
    [NotifyPropertyChangedFor(nameof(KnowledgeWorkspaceHeader))]
    [NotifyPropertyChangedFor(nameof(SearchDocumentsLabel))]
    [NotifyPropertyChangedFor(nameof(ExportButton))]
    [NotifyPropertyChangedFor(nameof(NewChatButton))]
    [NotifyPropertyChangedFor(nameof(EmptyTitle))]
    [NotifyPropertyChangedFor(nameof(EmptySubtitle))]
    [NotifyPropertyChangedFor(nameof(YouLabel))]
    [NotifyPropertyChangedFor(nameof(AiAssistantLabel))]
    [NotifyPropertyChangedFor(nameof(SourcesHeader))]
    [NotifyPropertyChangedFor(nameof(OpenSourceButton))]
    [NotifyPropertyChangedFor(nameof(CancelButton))]
    [NotifyPropertyChangedFor(nameof(QuickStartHeader))]
    [NotifyPropertyChangedFor(nameof(InputTooltip))]
    [NotifyPropertyChangedFor(nameof(SendButton))]
    [NotifyPropertyChangedFor(nameof(ComposerHint))]
    [NotifyPropertyChangedFor(nameof(SettingsTitle))]
    [NotifyPropertyChangedFor(nameof(SettingsSubtitle))]
    [NotifyPropertyChangedFor(nameof(OpenRouterQuickTitle))]
    [NotifyPropertyChangedFor(nameof(OpenRouterQuickDesc))]
    [NotifyPropertyChangedFor(nameof(UseOpenRouterButton))]
    [NotifyPropertyChangedFor(nameof(ProviderLabel))]
    [NotifyPropertyChangedFor(nameof(ApiKeyLabel))]
    [NotifyPropertyChangedFor(nameof(ApiKeyHint))]
    [NotifyPropertyChangedFor(nameof(HoldToRevealButton))]
    [NotifyPropertyChangedFor(nameof(ChatModelLabel))]
    [NotifyPropertyChangedFor(nameof(TemperatureLabel))]
    [NotifyPropertyChangedFor(nameof(EmbeddingModelLabel))]
    [NotifyPropertyChangedFor(nameof(UseForEmbeddingLabel))]
    [NotifyPropertyChangedFor(nameof(ReindexHint))]
    [NotifyPropertyChangedFor(nameof(TestEmbeddingButton))]
    [NotifyPropertyChangedFor(nameof(BaseUrlLabel))]
    [NotifyPropertyChangedFor(nameof(TestConnectionButton))]
    [NotifyPropertyChangedFor(nameof(SaveProviderButton))]
    private bool _isVietnamese;

    [RelayCommand]
    private void ToggleLanguage() => IsVietnamese = !IsVietnamese;

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
    public string DocIndexedLabel => IsVietnamese ? "Đã nạp · Sẵn sàng" : "Indexed · Ready";
    public string DocPendingLabel => IsVietnamese ? "Chờ nạp lại" : "Pending · Re-index";
    public string RemoveDocTooltip => IsVietnamese ? "Xóa tài liệu" : "Remove document";
    public string AiProviderHeader => IsVietnamese ? "NỀN TẢNG AI" : "AI PROVIDER";
    public string ManageProvidersButton => IsVietnamese ? "Cài đặt API & Model" : "Manage providers";
    public string KnowledgeWorkspaceHeader => IsVietnamese ? "KHÔNG GIAN TRI THỨC" : "KNOWLEDGE WORKSPACE";
    public string SearchDocumentsLabel => IsVietnamese ? "Tra cứu tài liệu (RAG)" : "Search documents";
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
}
