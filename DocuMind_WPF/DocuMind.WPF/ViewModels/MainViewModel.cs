using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DocuMind.Application.DTOs;
using DocuMind.Application.Interfaces;
using DocuMind.Application.Services;
using DocuMind.Domain.Entities;
using DocuMind.Domain.Enums;
using DocuMind.WPF.Localization;
using DocuMind.WPF.Models;
using Microsoft.Win32;

namespace DocuMind.WPF.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly RagOrchestratorService _rag;
    private readonly IChatProviderFactory _providers;
    private readonly IRepository<Workspace> _workspaces;
    private readonly IChatHistoryStore _history;
    private int _selectionGeneration;
    private CancellationTokenSource? _reload, _operation;
    public ObservableCollection<Workspace> Workspaces { get; } = new();
    public ObservableCollection<ChatMessageDisplayModel> ChatMessages { get; } = new();
    public ObservableCollection<DocumentListItem> UploadedFiles { get; } = new();
    public ObservableCollection<ConversationSummary> Conversations { get; } = new();
    public ObservableCollection<PromptTemplate> PromptTemplates { get; } = new();
    public bool IsChatEmpty => ChatMessages.Count == 0;
    public bool HasConversationActivity { get; private set; }
    [ObservableProperty] private Workspace? _selectedWorkspace;
    [ObservableProperty] private string _workspaceName = "";
    [ObservableProperty] private string _userInput = "";
    [ObservableProperty] private string _systemPrompt = "You are a precise assistant for document research and learning.";
    [ObservableProperty] private bool _useRagMode = true;
    [ObservableProperty] private bool _isHistoryTabSelected;
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string _busyStatus = "";
    [ObservableProperty] private string _activeProviderInfo = "No provider configured";
    public bool CanWork => !IsBusy && !IsLoading;
    public Action? OpenSettingsAction { get; set; }

    public MainViewModel(RagOrchestratorService rag, IChatProviderFactory providers, IRepository<Workspace> workspaces, IChatHistoryStore history)
    {
        _rag = rag; _providers = providers; _workspaces = workspaces; _history = history;
        ChatMessages.CollectionChanged += (_, _) => OnPropertyChanged(nameof(IsChatEmpty));
        // Weak subscription: the static UiText.Current must not keep view models alive.
        System.ComponentModel.PropertyChangedEventManager.AddHandler(UiText.Current, OnLanguageChanged, nameof(UiText.IsVietnamese));
        RefreshPromptTemplates();
    }

    private void OnLanguageChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e) => RefreshPromptTemplates();

    private void RefreshPromptTemplates()
    {
        PromptTemplates.Clear();
        if (UiText.Current.IsVietnamese)
        {
            PromptTemplates.Add(new("Tóm tắt", "Tóm tắt ý chính", "Hãy tóm tắt các ý chính quan trọng nhất trong tài liệu thuộc workspace này."));
            PromptTemplates.Add(new("Câu hỏi ôn tập", "Tạo bộ câu hỏi kiểm tra", "Hãy tạo 5 câu hỏi ôn tập kèm đáp án ngắn gọn dựa trên tài liệu đã nạp."));
            PromptTemplates.Add(new("Giải thích khái niệm", "Làm rõ thuật ngữ", "Hãy giải thích các khái niệm quan trọng nhất có trong tài liệu đã nạp."));
        }
        else
        {
            PromptTemplates.Add(new("Summarize", "Summarize key ideas", "Summarize the key ideas in the documents in this workspace."));
            PromptTemplates.Add(new("Study questions", "Create a knowledge check", "Create five study questions with answers based on the uploaded documents."));
            PromptTemplates.Add(new("Explain concepts", "Clarify key terms", "Explain the most important concepts in the uploaded documents."));
        }
    }

    partial void OnIsBusyChanged(bool value) => RefreshCommands();
    partial void OnIsLoadingChanged(bool value) => RefreshCommands();
    private void RefreshCommands()
    {
        OnPropertyChanged(nameof(CanWork));
        UploadDocumentCommand.NotifyCanExecuteChanged(); DeleteDocumentCommand.NotifyCanExecuteChanged(); ReindexDocumentCommand.NotifyCanExecuteChanged();
        SendMessageCommand.NotifyCanExecuteChanged(); ClearChatCommand.NotifyCanExecuteChanged();
        OpenConversationCommand.NotifyCanExecuteChanged(); DeleteConversationCommand.NotifyCanExecuteChanged();
        CreateWorkspaceCommand.NotifyCanExecuteChanged(); SaveWorkspaceCommand.NotifyCanExecuteChanged(); DeleteWorkspaceCommand.NotifyCanExecuteChanged();
        OpenQuizCommand.NotifyCanExecuteChanged();
    }

    public async Task InitializeAsync()
    {
        try
        {
            var workspaces = await _workspaces.GetAllAsync();
            Workspaces.Clear();
            foreach (var ws in workspaces) Workspaces.Add(ws);
            SelectedWorkspace = Workspaces.FirstOrDefault();
            await RefreshActiveProviderInfoAsync();
        }
        catch (Exception ex) { BusyStatus = UserErrors.Describe(ex); }
    }

    public async Task RefreshActiveProviderInfoAsync()
    {
        try
        {
            var active = await _providers.GetActiveSettingAsync();
            ActiveProviderInfo = active == null ? AppText.T("Not configured", "Chưa cấu hình") : $"{active.ProviderType} · {active.ChatModel}";
        }
        catch (Exception ex) { ActiveProviderInfo = UserErrors.Describe(ex); }
    }

    public async Task OnProviderSettingsClosedAsync(bool savedSuccessfully)
    {
        await RefreshActiveProviderInfoAsync();
        if (!savedSuccessfully || SelectedWorkspace == null || !CanWork) return;
        var id = SelectedWorkspace.Id; var conversation = SelectedWorkspace.ConversationId; var generation = _selectionGeneration;

        // The saved provider may use a different embedding profile, so re-read the list:
        // documents indexed with the old profile now count as pending.
        List<DocumentListItem> files;
        try { files = await _rag.GetWorkspaceFilesAsync(id); }
        catch (Exception ex) { if (Matches(id, conversation, generation)) BusyStatus = UserErrors.Describe(ex); return; }
        if (!Matches(id, conversation, generation)) return;
        UploadedFiles.Clear(); foreach (var file in files) UploadedFiles.Add(file);

        var pending = files.Where(f => f.NeedsReindex && !string.IsNullOrWhiteSpace(f.SourcePath)).ToList();
        if (pending.Count == 0) return;
        var missing = pending.Where(f => !File.Exists(f.SourcePath)).Select(f => f.DisplayName).ToList();
        var pendingPaths = pending.Where(f => File.Exists(f.SourcePath)).Select(f => f.SourcePath).ToList();

        try { await _providers.GetEmbeddingSessionAsync(); }
        catch (Exception ex)
        {
            BusyStatus = AppText.T($"{pending.Count} document(s) need re-indexing, but no embedding provider is ready: {UserErrors.Describe(ex)}",
                $"{pending.Count} tài liệu cần nạp lại nhưng chưa có provider embedding sẵn sàng: {UserErrors.Describe(ex)}");
            return;
        }

        if (pendingPaths.Count > 0) await IngestFilesAsync(pendingPaths);
        if (missing.Count > 0 && Matches(id, conversation, generation))
            BusyStatus = (BusyStatus + AppText.T($" Original file not found, upload again: {string.Join(", ", missing)}.", $" Không tìm thấy file gốc, hãy nạp lại: {string.Join(", ", missing)}.")).Trim();
    }

    partial void OnSelectedWorkspaceChanged(Workspace? value) => _ = ReloadSelectedWorkspaceAsync();
    public async Task ReloadSelectedWorkspaceAsync()
    {
        var generation = ++_selectionGeneration;
        _reload?.Cancel();
        using var reload = new CancellationTokenSource();
        _reload = reload;
        var workspace = SelectedWorkspace;
        ChatMessages.Clear(); UploadedFiles.Clear(); Conversations.Clear();
        WorkspaceName = workspace?.Name ?? ""; SystemPrompt = workspace?.SystemPrompt ?? "";
        IsLoading = true;
        try
        {
            if (workspace == null) return;
            var filesTask = _rag.GetWorkspaceFilesAsync(workspace.Id, reload.Token);
            var historyTask = _history.LoadForDisplayAsync(workspace.Id, workspace.ConversationId, ct: reload.Token);
            var conversationsTask = _history.ListConversationsAsync(workspace.Id, reload.Token);
            await Task.WhenAll(filesTask, historyTask, conversationsTask);
            if (!Matches(workspace.Id, workspace.ConversationId, generation)) return;
            foreach (var file in await filesTask) UploadedFiles.Add(file);
            foreach (var message in await historyTask) ChatMessages.Add(ChatMessageDisplayModel.FromEntity(message));
            foreach (var item in await conversationsTask) Conversations.Add(item);
        }
        catch (OperationCanceledException) { /* Superseded selection; no UI mutation. */ }
        catch (Exception ex) { if (generation == _selectionGeneration) BusyStatus = UserErrors.Describe(ex); }
        finally
        {
            if (generation == _selectionGeneration) { IsLoading = false; _reload = null; }
        }
    }

    private bool Matches(Guid workspace, Guid conversation, int generation) => generation == _selectionGeneration &&
        SelectedWorkspace?.Id == workspace && SelectedWorkspace.ConversationId == conversation;

    [RelayCommand(CanExecute = nameof(CanWork))]
    private async Task UploadDocumentAsync()
    {
        var dialog = new OpenFileDialog { Title = AppText.T("Choose documents", "Chọn tài liệu"), Filter = "Documents (*.pdf;*.txt;*.md)|*.pdf;*.txt;*.md|All files (*.*)|*.*", Multiselect = true };
        if (SelectedWorkspace != null && dialog.ShowDialog() == true) await IngestFilesAsync(dialog.FileNames);
    }

    public async Task IngestFilesAsync(IEnumerable<string> paths)
    {
        if (!CanWork || SelectedWorkspace == null) return;
        var id = SelectedWorkspace.Id; var conversation = SelectedWorkspace.ConversationId; var generation = _selectionGeneration;
        IsBusy = true;
        using var operation = new CancellationTokenSource(); _operation = operation;
        var success = new List<string>(); var failures = new List<string>();
        var progress = new Progress<string>(message => { if (_operation == operation && !operation.IsCancellationRequested && Matches(id, conversation, generation)) BusyStatus = message; });
        try
        {
            foreach (var path in paths)
            {
                if (operation.IsCancellationRequested) break;
                try { await _rag.IngestDocumentAsync(id, path, progress, operation.Token); success.Add(Path.GetFileName(path)); }
                catch (OperationCanceledException) { failures.Add(Path.GetFileName(path) + AppText.T(": canceled or timed out", ": đã hủy hoặc hết thời gian chờ")); break; }
                catch (Exception ex) { failures.Add(Path.GetFileName(path) + ": " + UserErrors.Describe(ex)); }
            }
            var files = await _rag.GetWorkspaceFilesAsync(id);
            if (!Matches(id, conversation, generation)) return;
            UploadedFiles.Clear(); foreach (var file in files) UploadedFiles.Add(file);
            BusyStatus = AppText.T($"Completed {success.Count}: {string.Join(", ", success)}. Failed or canceled {failures.Count}: {string.Join("; ", failures)}",
                $"Hoàn tất {success.Count}: {string.Join(", ", success)}. Lỗi hoặc đã hủy {failures.Count}: {string.Join("; ", failures)}");
        }
        catch (Exception ex) { if (Matches(id, conversation, generation)) BusyStatus = UserErrors.Describe(ex); }
        finally { _operation = null; IsBusy = false; }
    }

    [RelayCommand] private void Cancel() => _operation?.Cancel();

    /// <summary>Nạp lại một tài liệu từ đường dẫn gốc (khi nạp lỗi hoặc đã đổi model embedding).</summary>
    [RelayCommand(CanExecute = nameof(CanWork))]
    private async Task ReindexDocumentAsync(DocumentListItem? document)
    {
        if (document == null || SelectedWorkspace == null) return;
        if (string.IsNullOrWhiteSpace(document.SourcePath) || !File.Exists(document.SourcePath))
        {
            BusyStatus = AppText.T($"Original file not found: {document.SourcePath}. Upload it again from its new location.",
                $"Không tìm thấy file gốc: {document.SourcePath}. Hãy nạp lại từ vị trí mới.");
            return;
        }
        await IngestFilesAsync([document.SourcePath]);
    }

    [RelayCommand(CanExecute = nameof(CanWork))]
    private async Task DeleteDocumentAsync(DocumentListItem? document)
    {
        if (SelectedWorkspace == null || document == null) return;
        var id = SelectedWorkspace.Id; var conversation = SelectedWorkspace.ConversationId; var generation = _selectionGeneration;
        if (MessageBox.Show(AppText.T($"Remove '{document.DisplayName}' ({document.SourcePath}) and all of its index versions?",
                $"Xóa '{document.DisplayName}' ({document.SourcePath}) cùng mọi phiên bản index của nó?"),
            AppText.T("Remove document", "Xóa tài liệu"), MessageBoxButton.YesNo) != MessageBoxResult.Yes) return;
        IsBusy = true;
        try
        {
            await _rag.DeleteDocumentAsync(id, document.Id);
            if (Matches(id, conversation, generation)) UploadedFiles.Remove(document);
        }
        catch (Exception ex) { if (Matches(id, conversation, generation)) BusyStatus = UserErrors.Describe(ex); }
        finally { IsBusy = false; }
    }

    [RelayCommand(CanExecute = nameof(CanWork))]
    private async Task SendMessageAsync()
    {
        if (string.IsNullOrWhiteSpace(UserInput) || SelectedWorkspace == null || !CanWork) return;
        var id = SelectedWorkspace.Id; var conversation = SelectedWorkspace.ConversationId; var generation = _selectionGeneration;
        var question = UserInput.Trim(); var prompt = SystemPrompt; var mode = UseRagMode ? ChatMode.Rag : ChatMode.Direct;
        UserInput = ""; IsBusy = true; BusyStatus = AppText.T("Thinking…", "Đang suy nghĩ…");
        ChatMessageDisplayModel? pending = null;
        using var operation = new CancellationTokenSource(TimeSpan.FromMinutes(10)); _operation = operation;
        try
        {
            var session = await _providers.GetChatSessionAsync(operation.Token);
            var previous = await _history.LoadAsync(id, conversation, ct: operation.Token);
            // History store applies a conservative UTF-8 byte/token budget before building provider turns.
            var history = previous.Where(m => m.Mode == mode).Select(m => new ChatTurn(m.Role == MessageRole.User ? "user" : "assistant", m.Content)).ToList();
            var user = Message(MessageRole.User, question, []);
            pending = ChatMessageDisplayModel.FromEntity(user);
            if (Matches(id, conversation, generation)) ChatMessages.Add(pending);
            ChatResultDto result = mode == ChatMode.Rag
                ? await _rag.ChatWithRagAsync(id, prompt, question, ct: operation.Token, session: session, history: history)
                : new(await _rag.ChatExternalAsync(prompt, question, operation.Token, session, history), []);
            var assistant = Message(MessageRole.Assistant, result.ResponseText, result.Sources);
            if (assistant.Timestamp <= user.Timestamp) assistant.Timestamp = user.Timestamp.AddTicks(1);
            await _history.AppendAsync(user, assistant, operation.Token);
            HasConversationActivity = true;
            pending = null;
            if (Matches(id, conversation, generation))
            {
                ChatMessages.Add(ChatMessageDisplayModel.FromEntity(assistant));
                BusyStatus = "";
                var conversations = await _history.ListConversationsAsync(id, operation.Token);
                if (Matches(id, conversation, generation))
                {
                    Conversations.Clear();
                    foreach (var item in conversations) Conversations.Add(item);
                }
            }

            ChatMessage Message(MessageRole role, string text, List<RagSourceDto> sources) => new()
            {
                WorkspaceId = id, ConversationId = conversation, Role = role, Content = text, SourcesJson = JsonSerializer.Serialize(sources),
                Mode = mode, Provider = session.Provider.ToString(), Model = session.Model, Temperature = session.Temperature, Timestamp = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            if (Matches(id, conversation, generation))
            {
                BusyStatus = UserErrors.Describe(ex);
                // The question was never saved: drop its bubble and give the text back so it can be resent.
                if (pending != null) ChatMessages.Remove(pending);
                if (string.IsNullOrEmpty(UserInput)) UserInput = question;
            }
        }
        finally { _operation = null; IsBusy = false; }
    }

    [RelayCommand] private void ApplyTemplate(PromptTemplate? template) { if (template != null) UserInput = template.Prompt; }

    [RelayCommand] private void SelectDocumentsTab() => IsHistoryTabSelected = false;
    [RelayCommand] private void SelectHistoryTab() => IsHistoryTabSelected = true;

    [RelayCommand(CanExecute = nameof(CanWork))]
    private async Task ClearChatAsync()
    {
        if (SelectedWorkspace == null) return;
        var workspace = SelectedWorkspace;
        var previous = workspace.ConversationId;
        IsBusy = true;
        workspace.ConversationId = Guid.NewGuid();
        try { await _workspaces.UpdateAsync(workspace); if (SelectedWorkspace?.Id == workspace.Id) await ReloadSelectedWorkspaceAsync(); }
        catch (Exception ex) { workspace.ConversationId = previous; BusyStatus = UserErrors.Describe(ex); }
        finally { IsBusy = false; }
    }

    [RelayCommand(CanExecute = nameof(CanWork))]
    private async Task OpenConversationAsync(ConversationSummary? summary)
    {
        if (SelectedWorkspace == null || summary == null) return;
        var workspace = SelectedWorkspace;
        if (workspace.ConversationId == summary.ConversationId && ChatMessages.Count > 0) return;

        var previous = workspace.ConversationId;
        IsBusy = true;
        workspace.ConversationId = summary.ConversationId;
        try
        {
            await _workspaces.UpdateAsync(workspace);
            if (SelectedWorkspace?.Id == workspace.Id) await ReloadSelectedWorkspaceAsync();
        }
        catch (Exception ex) { workspace.ConversationId = previous; BusyStatus = UserErrors.Describe(ex); }
        finally { IsBusy = false; }
    }

    [RelayCommand(CanExecute = nameof(CanWork))]
    private async Task DeleteConversationAsync(ConversationSummary? summary)
    {
        if (SelectedWorkspace == null || summary == null) return;
        var workspace = SelectedWorkspace;
        IsBusy = true;
        try
        {
            await _history.DeleteConversationAsync(workspace.Id, summary.ConversationId);
            Conversations.Remove(summary);
            if (workspace.ConversationId == summary.ConversationId)
            {
                workspace.ConversationId = Guid.NewGuid();
                await _workspaces.UpdateAsync(workspace);
                if (SelectedWorkspace?.Id == workspace.Id) await ReloadSelectedWorkspaceAsync();
            }
        }
        catch (Exception ex) { BusyStatus = UserErrors.Describe(ex); }
        finally { IsBusy = false; }
    }

    /// <summary>
    /// Khi tắt ứng dụng, giữ lại toàn bộ tin nhắn cũ trong lịch sử và tạo ConversationId mới
    /// để lần mở tiếp theo bắt đầu một cuộc trò chuyện mới sạch sẽ.
    /// </summary>
    public async Task StartNewConversationOnCloseAsync()
    {
        var targets = Workspaces.Count > 0
            ? Workspaces.ToList()
            : (SelectedWorkspace != null ? [SelectedWorkspace] : new List<Workspace>());

        foreach (var workspace in targets)
        {
            var existing = await _history.LoadForDisplayAsync(workspace.Id, workspace.ConversationId, limit: 1);
            if (existing.Count == 0) continue;

            workspace.ConversationId = Guid.NewGuid();
            await _workspaces.UpdateAsync(workspace);
        }
    }

    [RelayCommand(CanExecute = nameof(CanWork))]
    private async Task CreateWorkspaceAsync()
    {
        var generation = _selectionGeneration;
        IsBusy = true;
        try
        {
            var workspace = new Workspace { Name = AppText.T("New workspace", "Workspace mới"), SystemPrompt = SystemPrompt };
            await _workspaces.AddAsync(workspace); Workspaces.Add(workspace);
            if (generation == _selectionGeneration) SelectedWorkspace = workspace;
        }
        catch (Exception ex) { BusyStatus = UserErrors.Describe(ex); }
        finally { IsBusy = false; }
    }
    [RelayCommand(CanExecute = nameof(CanWork))]
    private async Task SaveWorkspaceAsync()
    {
        if (SelectedWorkspace == null || string.IsNullOrWhiteSpace(WorkspaceName)) return;
        var workspace = SelectedWorkspace;
        var generation = _selectionGeneration;
        var updated = new Workspace { Id = workspace.Id, ConversationId = workspace.ConversationId, Name = WorkspaceName.Trim(),
            Description = workspace.Description, SystemPrompt = SystemPrompt, CreatedAt = workspace.CreatedAt };
        IsBusy = true;
        try
        {
            await _workspaces.UpdateAsync(updated);
            var index = Workspaces.IndexOf(workspace);
            var restoreSelection = Matches(workspace.Id, workspace.ConversationId, generation);
            if (index >= 0) Workspaces[index] = updated;
            if (restoreSelection) SelectedWorkspace = updated;
        }
        catch (Exception ex) { BusyStatus = UserErrors.Describe(ex); }
        finally { IsBusy = false; }
    }
    [RelayCommand(CanExecute = nameof(CanWork))]
    private async Task DeleteWorkspaceAsync()
    {
        if (SelectedWorkspace == null) return;
        var workspace = SelectedWorkspace;
        if (MessageBox.Show(AppText.T($"Delete workspace '{workspace.Name}'? Its documents, index versions, and chat history will be permanently removed.",
                $"Xóa workspace '{workspace.Name}'? Toàn bộ tài liệu, phiên bản index và lịch sử chat sẽ bị xóa vĩnh viễn."),
            AppText.T("Delete workspace", "Xóa workspace"), MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;
        IsBusy = true;
        try
        {
            await _workspaces.DeleteAsync(workspace);
            var restoreSelection = SelectedWorkspace?.Id == workspace.Id;
            Workspaces.Remove(workspace);
            if (restoreSelection) SelectedWorkspace = Workspaces.FirstOrDefault();
        }
        catch (Exception ex) { BusyStatus = UserErrors.Describe(ex); }
        finally { IsBusy = false; }
    }

    public string BuildMarkdownExport()
    {
        var text = new StringBuilder($"# DocuMind — {SelectedWorkspace?.Name}\n\n");
        foreach (var message in ChatMessages)
        {
            text.AppendLine($"### {message.SenderName} — {message.Timestamp:O}");
            text.AppendLine($"Mode: {message.Mode}; Provider: {message.Provider}; Model: {message.Model}; Temperature: {message.Temperature}\n");
            text.AppendLine(message.Content);
            foreach (var source in message.Sources)
                text.AppendLine($"\n- Source: {source.FileName}, page {source.PageNumber}, score {source.Score:F3}, document {source.DocumentId}\n  {source.Content}");
            text.AppendLine("\n---\n");
        }
        return text.ToString();
    }
    [RelayCommand] private void ExportMarkdown()
    {
        var dialog = new SaveFileDialog { Filter = "Markdown (*.md)|*.md", FileName = $"DocuMind_{DateTime.Now:yyyyMMdd_HHmmss}.md" };
        try { if (dialog.ShowDialog() == true) File.WriteAllText(dialog.FileName, BuildMarkdownExport(), Encoding.UTF8); }
        catch (Exception ex) { BusyStatus = UserErrors.Describe(ex); }
    }
    [RelayCommand] private void OpenSource(RagSourceDto? source)
    {
        if (source?.SourcePath == null || !File.Exists(source.SourcePath)) { BusyStatus = AppText.T("The original source file is no longer available on this device.", "File nguồn gốc không còn trên máy này."); return; }
        try
        {
            var uri = new Uri(Path.GetFullPath(source.SourcePath)).AbsoluteUri;
            if (Path.GetExtension(source.SourcePath).Equals(".pdf", StringComparison.OrdinalIgnoreCase))
            {
                // Edge is not on PATH; the shell resolves it through App Paths. Without Edge, open the PDF in the default viewer (no page jump).
                try { Process.Start(new ProcessStartInfo("msedge.exe", "\"" + uri + "#page=" + Math.Max(1, source.PageNumber) + "\"") { UseShellExecute = true }); }
                catch (System.ComponentModel.Win32Exception) { Process.Start(new ProcessStartInfo(source.SourcePath) { UseShellExecute = true }); }
            }
            else Process.Start(new ProcessStartInfo(source.SourcePath) { UseShellExecute = true });
        }
        catch (Exception ex) { BusyStatus = UserErrors.Describe(ex); }
    }
    [RelayCommand] private void OpenSettings() => OpenSettingsAction?.Invoke();

    public Action<Workspace>? OpenQuizAction { get; set; }
    [RelayCommand(CanExecute = nameof(CanWork))]
    private void OpenQuiz() { if (SelectedWorkspace != null) OpenQuizAction?.Invoke(SelectedWorkspace); }
}
