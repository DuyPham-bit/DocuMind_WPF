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
    public ObservableCollection<PromptTemplate> PromptTemplates { get; } = new();
    [ObservableProperty] private Workspace? _selectedWorkspace;
    [ObservableProperty] private string _workspaceName = "";
    [ObservableProperty] private string _userInput = "";
    [ObservableProperty] private string _systemPrompt = "Bạn là trợ lý học tập và nghiên cứu tài liệu chuẩn xác.";
    [ObservableProperty] private bool _useRagMode = true;
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string _busyStatus = "";
    [ObservableProperty] private string _activeProviderInfo = "Chưa thiết lập Provider";
    public bool CanWork => !IsBusy && !IsLoading;
    public Action? OpenSettingsAction { get; set; }

    public MainViewModel(RagOrchestratorService rag, IChatProviderFactory providers, IRepository<Workspace> workspaces, IChatHistoryStore history)
    {
        _rag = rag; _providers = providers; _workspaces = workspaces; _history = history;
        PromptTemplates.Add(new("📌 Tóm tắt tài liệu", "Tóm tắt các ý chính", "Hãy tóm tắt các ý chính của tài liệu trong workspace."));
        PromptTemplates.Add(new("❓ Câu hỏi ôn tập", "Tạo câu hỏi kiểm tra", "Tạo 5 câu hỏi ôn tập kèm đáp án dựa trên tài liệu."));
        PromptTemplates.Add(new("💡 Giải thích khái niệm", "Làm rõ thuật ngữ", "Giải thích các khái niệm quan trọng trong tài liệu."));
    }

    partial void OnIsBusyChanged(bool value) => RefreshCommands();
    partial void OnIsLoadingChanged(bool value) => RefreshCommands();
    private void RefreshCommands()
    {
        OnPropertyChanged(nameof(CanWork));
        UploadDocumentCommand.NotifyCanExecuteChanged(); DeleteDocumentCommand.NotifyCanExecuteChanged();
        SendMessageCommand.NotifyCanExecuteChanged(); ClearChatCommand.NotifyCanExecuteChanged();
        CreateWorkspaceCommand.NotifyCanExecuteChanged(); SaveWorkspaceCommand.NotifyCanExecuteChanged(); DeleteWorkspaceCommand.NotifyCanExecuteChanged();
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
            ActiveProviderInfo = active == null ? "Chưa cấu hình (Bấm Cài đặt)" : $"{active.ProviderType} ({active.ChatModel})";
        }
        catch (Exception ex) { ActiveProviderInfo = UserErrors.Describe(ex); }
    }

    partial void OnSelectedWorkspaceChanged(Workspace? value) => _ = ReloadSelectedWorkspaceAsync();
    public async Task ReloadSelectedWorkspaceAsync()
    {
        var generation = ++_selectionGeneration;
        _reload?.Cancel();
        using var reload = new CancellationTokenSource();
        _reload = reload;
        var workspace = SelectedWorkspace;
        ChatMessages.Clear(); UploadedFiles.Clear();
        WorkspaceName = workspace?.Name ?? ""; SystemPrompt = workspace?.SystemPrompt ?? "";
        IsLoading = true;
        try
        {
            if (workspace == null) return;
            var filesTask = _rag.GetWorkspaceFilesAsync(workspace.Id, reload.Token);
            var historyTask = _history.LoadAsync(workspace.Id, workspace.ConversationId, ct: reload.Token);
            await Task.WhenAll(filesTask, historyTask);
            if (!Matches(workspace.Id, workspace.ConversationId, generation)) return;
            foreach (var file in await filesTask) UploadedFiles.Add(file);
            foreach (var message in await historyTask) ChatMessages.Add(ChatMessageDisplayModel.FromEntity(message));
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
        var dialog = new OpenFileDialog { Title = "Chọn tài liệu", Filter = "Tài liệu (*.pdf;*.txt;*.md)|*.pdf;*.txt;*.md|Tất cả (*.*)|*.*", Multiselect = true };
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
                catch (OperationCanceledException) { failures.Add(Path.GetFileName(path) + ": đã hủy/hết thời gian"); break; }
                catch (Exception ex) { failures.Add(Path.GetFileName(path) + ": " + UserErrors.Describe(ex)); }
            }
            var files = await _rag.GetWorkspaceFilesAsync(id);
            if (!Matches(id, conversation, generation)) return;
            UploadedFiles.Clear(); foreach (var file in files) UploadedFiles.Add(file);
            BusyStatus = $"Thành công {success.Count}: {string.Join(", ", success)}. Lỗi/hủy {failures.Count}: {string.Join("; ", failures)}";
        }
        catch (Exception ex) { if (Matches(id, conversation, generation)) BusyStatus = UserErrors.Describe(ex); }
        finally { _operation = null; IsBusy = false; }
    }

    [RelayCommand] private void Cancel() => _operation?.Cancel();

    [RelayCommand(CanExecute = nameof(CanWork))]
    private async Task DeleteDocumentAsync(DocumentListItem? document)
    {
        if (SelectedWorkspace == null || document == null) return;
        var id = SelectedWorkspace.Id; var conversation = SelectedWorkspace.ConversationId; var generation = _selectionGeneration;
        if (MessageBox.Show($"Xóa '{document.DisplayName}' ({document.SourcePath}) cùng toàn bộ phiên bản index?", "Xóa tài liệu", MessageBoxButton.YesNo) != MessageBoxResult.Yes) return;
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
        UserInput = ""; IsBusy = true; BusyStatus = "Đang xử lý câu hỏi...";
        using var operation = new CancellationTokenSource(TimeSpan.FromMinutes(2)); _operation = operation;
        try
        {
            var session = await _providers.GetChatSessionAsync(operation.Token);
            var previous = await _history.LoadAsync(id, conversation, ct: operation.Token);
            // History store applies a conservative UTF-8 byte/token budget before building provider turns.
            var history = previous.Where(m => m.Mode == mode).Select(m => new ChatTurn(m.Role == MessageRole.User ? "user" : "assistant", m.Content)).ToList();
            var user = Message(MessageRole.User, question, []);
            if (Matches(id, conversation, generation)) ChatMessages.Add(ChatMessageDisplayModel.FromEntity(user));
            ChatResultDto result = mode == ChatMode.Rag
                ? await _rag.ChatWithRagAsync(id, prompt, question, ct: operation.Token, session: session, history: history)
                : new(await _rag.ChatExternalAsync(prompt, question, operation.Token, session, history), []);
            var assistant = Message(MessageRole.Assistant, result.ResponseText, result.Sources);
            if (assistant.Timestamp <= user.Timestamp) assistant.Timestamp = user.Timestamp.AddTicks(1);
            await _history.AppendAsync(user, assistant, operation.Token);
            if (Matches(id, conversation, generation)) ChatMessages.Add(ChatMessageDisplayModel.FromEntity(assistant));
            if (Matches(id, conversation, generation)) BusyStatus = "";

            ChatMessage Message(MessageRole role, string text, List<RagSourceDto> sources) => new()
            {
                WorkspaceId = id, ConversationId = conversation, Role = role, Content = text, SourcesJson = JsonSerializer.Serialize(sources),
                Mode = mode, Provider = session.Provider.ToString(), Model = session.Model, Temperature = session.Temperature, Timestamp = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            if (Matches(id, conversation, generation)) BusyStatus = UserErrors.Describe(ex);
        }
        finally { _operation = null; IsBusy = false; }
    }

    [RelayCommand] private void ApplyTemplate(PromptTemplate? template) { if (template != null) UserInput = template.Prompt; }

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
    private async Task CreateWorkspaceAsync()
    {
        var generation = _selectionGeneration;
        IsBusy = true;
        try
        {
            var workspace = new Workspace { Name = "Workspace mới", SystemPrompt = SystemPrompt };
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
        if (MessageBox.Show($"Xóa workspace '{workspace.Name}'? Toàn bộ tài liệu, mọi phiên bản index và lịch sử chat trong workspace sẽ bị xóa vĩnh viễn.",
            "Xác nhận cascade", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;
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
                text.AppendLine($"\n- Nguồn: {source.FileName}, trang {source.PageNumber}, score {source.Score:F3}, document {source.DocumentId}\n  {source.Content}");
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
        if (source?.SourcePath == null || !File.Exists(source.SourcePath)) { BusyStatus = "Tệp nguồn không còn tồn tại trên máy."; return; }
        try
        {
            var uri = new Uri(Path.GetFullPath(source.SourcePath)).AbsoluteUri;
            if (Path.GetExtension(source.SourcePath).Equals(".pdf", StringComparison.OrdinalIgnoreCase))
            {
                var start = new ProcessStartInfo("msedge.exe") { UseShellExecute = false, CreateNoWindow = true };
                start.ArgumentList.Add(uri + "#page=" + Math.Max(1, source.PageNumber));
                Process.Start(start);
            }
            else Process.Start(new ProcessStartInfo(source.SourcePath) { UseShellExecute = true });
        }
        catch (Exception ex) { BusyStatus = UserErrors.Describe(ex); }
    }
    [RelayCommand] private void OpenSettings() => OpenSettingsAction?.Invoke();
}
