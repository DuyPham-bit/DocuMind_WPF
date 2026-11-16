using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DocuMind.Application.DTOs;
using DocuMind.Application.Interfaces;
using DocuMind.Application.Services;
using DocuMind.Domain.Enums;

namespace DocuMind.WPF.ViewModels;

public partial class SettingsViewModel(IChatProviderFactory providerFactory) : ObservableObject, IDisposable
{
    private int _generation;
    private bool _initializing;
    private CancellationTokenSource? _operation;
    public ObservableCollection<AiProviderType> AvailableProviders { get; } = new(Enum.GetValues<AiProviderType>());
    [ObservableProperty] private AiProviderType _selectedProvider = AiProviderType.OpenRouter;
    [ObservableProperty] private string _apiKey = "";
    [ObservableProperty] private string _chatModel = "";
    [ObservableProperty] private string _embeddingModel = "";
    [ObservableProperty] private string _baseUrl = "";
    [ObservableProperty] private double _temperature = 0.7;
    [ObservableProperty] private bool _useForEmbedding;
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private string _statusMessage = "";
    [ObservableProperty] private bool _isSuccessStatus;
    public Action? CloseAction { get; set; }
    public bool SavedSuccessfully { get; private set; }
    private AiProviderType? _activeEmbeddingProvider;
    public bool IsEditable => !IsBusy && !IsLoading;
    public ProviderConfigDraft CaptureDraft() => new(SelectedProvider, ApiKey, ChatModel, EmbeddingModel, BaseUrl, Temperature, UseForEmbedding);

    partial void OnIsBusyChanged(bool value) => RefreshCommands();
    partial void OnIsLoadingChanged(bool value) => RefreshCommands();
    private void RefreshCommands()
    {
        OnPropertyChanged(nameof(IsEditable));
        TestConnectionCommand.NotifyCanExecuteChanged();
        TestEmbeddingCommand.NotifyCanExecuteChanged();
        SaveCommand.NotifyCanExecuteChanged();
        UseOpenRouterCommand.NotifyCanExecuteChanged();
    }

    public async Task InitializeAsync()
    {
        var generation = _generation;
        IsLoading = true;
        try
        {
            var active = await providerFactory.GetActiveSettingAsync();
            if (generation != _generation) return;
            _initializing = true;
            if (active != null) SelectedProvider = active.ProviderType;
            _initializing = false;
            await LoadDraftAsync(SelectedProvider);
        }
        catch (Exception ex) { StatusMessage = UserErrors.Describe(ex); IsLoading = false; }
        finally { _initializing = false; }
    }

    partial void OnSelectedProviderChanged(AiProviderType value)
    {
        if (!_initializing) _ = LoadDraftAsync(value);
    }

    [RelayCommand(CanExecute = nameof(IsEditable))]
    private async Task UseOpenRouterAsync()
    {
        if (SelectedProvider == AiProviderType.OpenRouter) return;
        _initializing = true;
        try { SelectedProvider = AiProviderType.OpenRouter; }
        finally { _initializing = false; }
        await LoadDraftAsync(AiProviderType.OpenRouter);
    }

    public async Task LoadDraftAsync(AiProviderType provider)
    {
        var generation = ++_generation;
        _operation?.Cancel();
        ApiKey = ""; ChatModel = ""; EmbeddingModel = ""; BaseUrl = ""; Temperature = 0.7; UseForEmbedding = false;
        IsLoading = true; IsSuccessStatus = false; StatusMessage = AppText.T("Loading provider settings…", "Đang tải cấu hình provider…");
        try
        {
            var draft = await providerFactory.LoadDraftAsync(provider);
            var activeEmbedding = await providerFactory.GetActiveEmbeddingSettingAsync();
            if (generation != _generation || provider != SelectedProvider) return;
            _activeEmbeddingProvider = activeEmbedding?.ProviderType;
            ApiKey = draft.ApiKey; ChatModel = draft.ChatModel; EmbeddingModel = draft.EmbeddingModel;
            BaseUrl = draft.BaseUrl; Temperature = draft.Temperature; UseForEmbedding = draft.UseForEmbedding;
            StatusMessage = draft.KeyNeedsReplacement ? AppText.T("The stored key cannot be decrypted for this Windows user. Enter a replacement key and save; your other settings were preserved.", "Không giải mã được key đã lưu cho tài khoản Windows này. Hãy nhập key mới rồi lưu; các cài đặt khác vẫn được giữ.")
                : provider == AiProviderType.OpenRouter ? AppText.T("Paste an OpenRouter API key, test the connection and embeddings, then save. Free models have rate limits.", "Dán API key OpenRouter, kiểm tra kết nối và embedding rồi lưu. Model miễn phí có giới hạn số lượt gọi.")
                : AppText.T("This is a draft. Tests do not save changes; saving activates the chat provider.", "Đây là bản nháp. Kiểm tra không lưu thay đổi; bấm lưu sẽ kích hoạt provider chat.");
            if (EmbeddingSwitchWarning() is { } warning) StatusMessage += " " + warning;
        }
        catch (Exception ex)
        {
            if (generation == _generation) StatusMessage = UserErrors.Describe(ex) + AppText.T(" If the saved key cannot be decrypted, enter a replacement and save.", " Nếu không giải mã được key đã lưu, hãy nhập key mới rồi lưu.");
        }
        finally { if (generation == _generation) IsLoading = false; }
    }

    partial void OnUseForEmbeddingChanged(bool value)
    {
        if (!IsLoading && EmbeddingSwitchWarning() is { } warning) { IsSuccessStatus = false; StatusMessage = warning; }
    }

    // Moving embeddings to another provider makes every existing index unusable for RAG until it is rebuilt.
    public string? EmbeddingSwitchWarning() =>
        UseForEmbedding && _activeEmbeddingProvider is { } current && current != SelectedProvider
            ? AppText.T($"Saving moves document embeddings from {current} to {SelectedProvider}. Documents in the open workspace are re-indexed automatically after saving; other workspaces show \"Pending · Re-index\" until you upload their files again.",
                $"Lưu sẽ chuyển embedding tài liệu từ {current} sang {SelectedProvider}. Tài liệu trong workspace đang mở sẽ tự được nạp lại sau khi lưu; các workspace khác hiện \"Chờ nạp lại\" cho tới khi bạn nạp lại file.")
            : null;

    [RelayCommand(CanExecute = nameof(IsEditable))]
    private Task TestConnectionAsync() => RunTestAsync(false);
    [RelayCommand(CanExecute = nameof(IsEditable))]
    private Task TestEmbeddingAsync() => RunTestAsync(true);
    private async Task RunTestAsync(bool embedding)
    {
        var draft = CaptureDraft();
        var generation = _generation;
        IsBusy = true; IsSuccessStatus = false; StatusMessage = AppText.T("Testing the draft configuration…", "Đang kiểm tra cấu hình nháp…");
        using var operation = new CancellationTokenSource(TimeSpan.FromMinutes(2));
        _operation = operation;
        try
        {
            var ok = embedding
                ? VectorMathHelper.IsValidVector(await providerFactory.CreateTemporaryEmbedding(draft).GenerateEmbeddingAsync("test", operation.Token))
                : await providerFactory.CreateTemporary(draft).TestConnectionAsync(operation.Token);
            if (generation != _generation) return;
            IsSuccessStatus = ok;
            StatusMessage = ok ? AppText.T("Connection successful. Your configuration has not been saved yet.", "Kết nối thành công. Cấu hình của bạn chưa được lưu.") : AppText.T("No valid response was received. Your configuration has not been saved.", "Không nhận được phản hồi hợp lệ. Cấu hình của bạn chưa được lưu.");
        }
        catch (Exception ex) { if (generation == _generation) StatusMessage = UserErrors.Describe(ex); }
        finally { _operation = null; IsBusy = false; }
    }

    [RelayCommand] private void Cancel() => _operation?.Cancel();

    [RelayCommand(CanExecute = nameof(IsEditable))]
    private async Task SaveAsync()
    {
        var draft = CaptureDraft();
        SavedSuccessfully = false;
        IsBusy = true; IsSuccessStatus = false;
        try
        {
            await providerFactory.SaveAsync(draft);
            SavedSuccessfully = true;
            IsSuccessStatus = true; StatusMessage = AppText.T("Provider settings saved.", "Đã lưu cấu hình provider.");
            CloseAction?.Invoke();
        }
        catch (Exception ex) { StatusMessage = UserErrors.Describe(ex); }
        finally { IsBusy = false; }
    }

    public void Dispose() { ++_generation; _operation?.Cancel(); ApiKey = ""; }
}
