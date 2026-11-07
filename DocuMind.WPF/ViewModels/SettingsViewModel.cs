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
        IsLoading = true; IsSuccessStatus = false; StatusMessage = "Đang tải cấu hình...";
        try
        {
            var draft = await providerFactory.LoadDraftAsync(provider);
            if (generation != _generation || provider != SelectedProvider) return;
            ApiKey = draft.ApiKey; ChatModel = draft.ChatModel; EmbeddingModel = draft.EmbeddingModel;
            BaseUrl = draft.BaseUrl; Temperature = draft.Temperature; UseForEmbedding = draft.UseForEmbedding;
            StatusMessage = draft.KeyNeedsReplacement ? "Không thể giải mã key cũ bằng DPAPI CurrentUser. Hãy nhập lại key và Lưu; các trường cấu hình khác đã được giữ lại."
                : provider == AiProviderType.OpenRouter ? "Dán API key OpenRouter, kiểm tra kết nối và embedding rồi Lưu cấu hình. Model miễn phí có giới hạn lượt gọi."
                : "Cấu hình nháp. Kiểm tra không lưu cấu hình; chỉ Lưu mới kích hoạt provider chat.";
        }
        catch (Exception ex)
        {
            if (generation == _generation) StatusMessage = UserErrors.Describe(ex) + " Nếu key cũ không giải mã được, hãy nhập lại key và Lưu.";
        }
        finally { if (generation == _generation) IsLoading = false; }
    }

    [RelayCommand(CanExecute = nameof(IsEditable))]
    private Task TestConnectionAsync() => RunTestAsync(false);
    [RelayCommand(CanExecute = nameof(IsEditable))]
    private Task TestEmbeddingAsync() => RunTestAsync(true);
    private async Task RunTestAsync(bool embedding)
    {
        var draft = CaptureDraft();
        var generation = _generation;
        IsBusy = true; IsSuccessStatus = false; StatusMessage = "Đang kiểm tra cấu hình nháp...";
        using var operation = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        _operation = operation;
        try
        {
            var ok = embedding
                ? VectorMathHelper.IsValidVector(await providerFactory.CreateTemporaryEmbedding(draft).GenerateEmbeddingAsync("test", operation.Token))
                : await providerFactory.CreateTemporary(draft).TestConnectionAsync(operation.Token);
            if (generation != _generation) return;
            IsSuccessStatus = ok;
            StatusMessage = ok ? "Kết nối thành công. Cấu hình chưa được lưu." : "Không nhận được phản hồi hợp lệ. Cấu hình chưa được lưu.";
        }
        catch (Exception ex) { if (generation == _generation) StatusMessage = UserErrors.Describe(ex); }
        finally { _operation = null; IsBusy = false; }
    }

    [RelayCommand] private void Cancel() => _operation?.Cancel();

    [RelayCommand(CanExecute = nameof(IsEditable))]
    private async Task SaveAsync()
    {
        var draft = CaptureDraft();
        IsBusy = true; IsSuccessStatus = false;
        try
        {
            await providerFactory.SaveAsync(draft);
            IsSuccessStatus = true; StatusMessage = "Đã lưu cấu hình.";
            CloseAction?.Invoke();
        }
        catch (Exception ex) { StatusMessage = UserErrors.Describe(ex); }
        finally { IsBusy = false; }
    }

    public void Dispose() { ++_generation; _operation?.Cancel(); ApiKey = ""; }
}
