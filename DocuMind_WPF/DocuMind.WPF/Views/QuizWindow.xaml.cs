using System.Globalization;
using System.Runtime.Versioning;
using System.Windows;
using System.Windows.Data;
using DocuMind.WPF.ViewModels;

namespace DocuMind.WPF.Views;

[SupportedOSPlatform("windows")]
public partial class QuizWindow : Window
{
    public QuizWindow(QuizViewModel viewModel, Guid workspaceId, string workspaceName)
    {
        InitializeComponent();
        DataContext = viewModel;
        Closed += (_, _) => viewModel.CancelCommand.Execute(null);
        Loaded += async (_, _) => await viewModel.InitializeAsync(workspaceId, workspaceName);
    }
}

/// <summary>Khóa lựa chọn sau khi nộp bài.</summary>
public sealed class InverseBoolConverter : IValueConverter
{
    public static InverseBoolConverter Instance { get; } = new();
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) => value is not true;
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => value is not true;
}
