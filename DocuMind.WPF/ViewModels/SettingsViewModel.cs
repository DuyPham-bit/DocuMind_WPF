using System.Collections.ObjectModel;
using System.Runtime.Versioning;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DocuMind.Application.Interfaces;
using DocuMind.Domain.Enums;

namespace DocuMind.WPF.ViewModels;

[SupportedOSPlatform("windows")]
public partial class SettingsViewModel : ObservableObject
{
    private readonly IChatProviderFactory _providerFactory;

    public ObservableCollection<AiProviderType> AvailableProviders { get; } = new()
    {
        AiProviderType.Gemini,
        AiProviderType.OpenAI,
        AiProviderType.Groq,
        AiProviderType.OllamaLocal
    };

    [ObservableProperty]
    private AiProviderType _selectedProvider = AiProviderType.Gemini;

    [ObservableProperty]
    private string _apiKey = string.Empty;

    [ObservableProperty]
    private string _baseUrl = string.Empty;

    [ObservableProperty]
    private string _chatModel = "gemini-2.5-flash";

    [ObservableProperty]
    private double _temperature = 0.7;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private bool _isSuccessStatus;

    public Action? CloseAction { get; set; }

    public SettingsViewModel(IChatProviderFactory providerFactory)
    {
        _providerFactory = providerFactory;
    }

    public async Task InitializeAsync()
    {
        IsBusy = true;
        StatusMessage = "Đang tải cấu hình...";

        try
        {
            var active = await _providerFactory.GetActiveSettingAsync();
            if (active != null)
            {
                SelectedProvider = active.ProviderType;
                ChatModel = active.ChatModel;
                BaseUrl = active.BaseUrl;
                Temperature = active.Temperature;
                ApiKey = await _providerFactory.GetDecryptedApiKeyAsync(active.ProviderType);
                StatusMessage = $"Đang hoạt động: {active.ProviderType} ({active.ChatModel})";
                IsSuccessStatus = true;
            }
            else
            {
                OnSelectedProviderChanged(SelectedProvider);
                StatusMessage = "Chưa có Provider nào được cấu hình.";
                IsSuccessStatus = false;
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Lỗi đọc cấu hình: {ex.Message}";
            IsSuccessStatus = false;
        }
        finally
        {
            IsBusy = false;
        }
    }

    partial void OnSelectedProviderChanged(AiProviderType value)
    {
        // Gợi ý giá trị mặc định theo từng nền tảng
        switch (value)
        {
            case AiProviderType.Gemini:
                ChatModel = "gemini-2.5-flash";
                BaseUrl = "";
                break;
            case AiProviderType.OpenAI:
                ChatModel = "gpt-4o-mini";
                BaseUrl = "https://api.openai.com/v1";
                break;
            case AiProviderType.Groq:
                ChatModel = "llama-3.3-70b-versatile";
                BaseUrl = "https://api.groq.com/openai/v1";
                break;
            case AiProviderType.OllamaLocal:
                ChatModel = "llama3";
                BaseUrl = "http://localhost:11434/v1";
                break;
        }

        // Tải API Key đã lưu (nếu có)
        _ = LoadSavedKeyForCurrentProviderAsync();
    }

    private async Task LoadSavedKeyForCurrentProviderAsync()
    {
        try
        {
            var key = await _providerFactory.GetDecryptedApiKeyAsync(SelectedProvider);
            if (!string.IsNullOrEmpty(key))
            {
                ApiKey = key;
            }
        }
        catch { }
    }

    [RelayCommand]
    private async Task TestConnectionAsync()
    {
        if (string.IsNullOrWhiteSpace(ApiKey) && SelectedProvider != AiProviderType.OllamaLocal)
        {
            StatusMessage = "Vui lòng nhập API Key trước khi kiểm tra.";
            IsSuccessStatus = false;
            return;
        }

        IsBusy = true;
        StatusMessage = "Đang kiểm tra kết nối API...";

        try
        {
            // Tạm thời chuyển provider để test
            await _providerFactory.SwitchProviderAsync(SelectedProvider, ApiKey, ChatModel, BaseUrl, Temperature);
            var provider = await _providerFactory.GetActiveProviderAsync();
            bool ok = await provider.TestConnectionAsync();

            if (ok)
            {
                StatusMessage = "Kết nối thành công! API Key và Model hợp lệ.";
                IsSuccessStatus = true;
            }
            else
            {
                StatusMessage = "Không nhận được phản hồi từ AI. Vui lòng kiểm tra lại Key hoặc Model.";
                IsSuccessStatus = false;
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Lỗi kết nối: {ex.Message}";
            IsSuccessStatus = false;
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (string.IsNullOrWhiteSpace(ApiKey) && SelectedProvider != AiProviderType.OllamaLocal)
        {
            StatusMessage = "Vui lòng nhập API Key.";
            IsSuccessStatus = false;
            return;
        }

        IsBusy = true;
        try
        {
            await _providerFactory.SwitchProviderAsync(SelectedProvider, ApiKey, ChatModel, BaseUrl, Temperature);
            StatusMessage = "Đã lưu cấu hình thành công!";
            IsSuccessStatus = true;
            await Task.Delay(300);
            CloseAction?.Invoke();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Lỗi khi lưu: {ex.Message}";
            IsSuccessStatus = false;
        }
        finally
        {
            IsBusy = false;
        }
    }
}

