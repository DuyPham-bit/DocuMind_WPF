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
        IsLoading = true; IsSuccessStatus = false; StatusMessage = "Loading provider settings…";
        try
        {
            var draft = await providerFactory.LoadDraftAsync(provider);
            var activeEmbedding = await providerFactory.GetActiveEmbeddingSettingAsync();
            if (generation != _generation || provider != SelectedProvider) return;
            _activeEmbeddingProvider = activeEmbedding?.ProviderType;
            ApiKey = draft.ApiKey; ChatModel = draft.ChatModel; EmbeddingModel = draft.EmbeddingModel;
            BaseUrl = draft.BaseUrl; Temperature = draft.Temperature; UseForEmbedding = draft.UseForEmbedding;
            StatusMessage = draft.KeyNeedsReplacement ? "The stored key cannot be decrypted for this Windows user. Enter a replacement key and save; your other settings were preserved."
                : provider == AiProviderType.OpenRouter ? "Paste an OpenRouter API key, test the connection and embeddings, then save. Free models have rate limits."
                : "This is a draft. Tests do not save changes; saving activates the chat provider.";
            if (EmbeddingSwitchWarning() is { } warning) StatusMessage += " " + warning;
        }
        catch (Exception ex)
        {
            if (generation == _generation) StatusMessage = UserErrors.Describe(ex) + " If the saved key cannot be decrypted, enter a replacement and save.";
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
            ? $"Saving moves document embeddings from {current} to {SelectedProvider}. Documents in the open workspace are re-indexed automatically after saving; other workspaces show \"Pending · Re-index\" until you upload their files again."
            : null;

    [RelayCommand(CanExecute = nameof(IsEditable))]
    private Task TestConnectionAsync() => RunTestAsync(false);
    [RelayCommand(CanExecute = nameof(IsEditable))]
    private Task TestEmbeddingAsync() => RunTestAsync(true);
    private async Task RunTestAsync(bool embedding)
    {
        var draft = CaptureDraft();
        var generation = _generation;
        IsBusy = true; IsSuccessStatus = false; StatusMessage = "Testing the draft configuration…";
        using var operation = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        _operation = operation;
        try
        {
            var ok = embedding
                ? VectorMathHelper.IsValidVector(await providerFactory.CreateTemporaryEmbedding(draft).GenerateEmbeddingAsync("test", operation.Token))
                : await providerFactory.CreateTemporary(draft).TestConnectionAsync(operation.Token);
            if (generation != _generation) return;
            IsSuccessStatus = ok;
            StatusMessage = ok ? "Connection successful. Your configuration has not been saved yet." : "No valid response was received. Your configuration has not been saved.";
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
            IsSuccessStatus = true; StatusMessage = "Provider settings saved.";
            CloseAction?.Invoke();
        }
        catch (Exception ex) { StatusMessage = UserErrors.Describe(ex); }
        finally { IsBusy = false; }
    }

    public void Dispose() { ++_generation; _operation?.Cancel(); ApiKey = ""; }
}
