using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DocuMind.Application.DTOs;
using DocuMind.Application.Services;

namespace DocuMind.WPF.ViewModels;

public partial class QuizOptionItem(int index, string text) : ObservableObject
{
    public int Index { get; } = index;
    public string Text { get; } = text;
    public string Label => $"{(char)('A' + Index)}. {Text}";
    [ObservableProperty] private bool _isSelected;
    [ObservableProperty] private bool _isCorrectAnswer;
    [ObservableProperty] private bool _isWrongChoice;
}

public partial class QuizQuestionItem : ObservableObject
{
    public QuizQuestionItem(int number, QuizQuestion question)
    {
        Number = number; Question = question;
        Options = question.Options.Select((text, i) => new QuizOptionItem(i, text)).ToList();
    }
    public int Number { get; }
    public QuizQuestion Question { get; }
    public string Title => $"Câu {Number}. {Question.Question}";
    public string GroupName { get; } = "q" + Guid.NewGuid().ToString("N");
    public List<QuizOptionItem> Options { get; }
    public string Explanation => string.IsNullOrWhiteSpace(Question.Explanation) ? "" : "Giải thích: " + Question.Explanation;
    public string SourceLabel => Question.Source == null ? "Nguồn: (AI không chỉ rõ đoạn tài liệu)" : $"Nguồn: {Question.Source.FileName} – trang {Question.Source.PageNumber}";
    [ObservableProperty] private bool _isRevealed;
    [ObservableProperty] private string _resultText = "";
    public int? SelectedIndex => Options.FirstOrDefault(o => o.IsSelected)?.Index;

    public void Reveal()
    {
        var selected = SelectedIndex;
        foreach (var option in Options)
        {
            option.IsCorrectAnswer = option.Index == Question.AnswerIndex;
            option.IsWrongChoice = option.IsSelected && option.Index != Question.AnswerIndex;
        }
        ResultText = selected == null ? "⚪ Bỏ trống" : selected == Question.AnswerIndex ? "✅ Đúng" : "❌ Sai";
        IsRevealed = true;
    }

    public void Reset()
    {
        foreach (var option in Options) { option.IsSelected = false; option.IsCorrectAnswer = false; option.IsWrongChoice = false; }
        ResultText = ""; IsRevealed = false;
    }
}

public partial class QuizViewModel(QuizService quiz) : ObservableObject
{
    private Guid _workspaceId;
    private QuizDraft? _draft;
    private CancellationTokenSource? _operation;
    public ObservableCollection<QuizQuestionItem> Questions { get; } = new();
    public ObservableCollection<QuizAttemptSummary> History { get; } = new();
    public IReadOnlyList<int> QuestionCounts { get; } = [3, 5, 7, 10];
    [ObservableProperty] private string _workspaceName = "";
    [ObservableProperty] private string _topic = "";
    [ObservableProperty] private int _questionCount = 5;
    [ObservableProperty] private string _status = "Nhập chủ đề (hoặc để trống để ôn toàn bộ tài liệu) rồi bấm Tạo quiz.";
    [ObservableProperty] private string _scoreText = "";
    [ObservableProperty] private bool _isSubmitted;
    [NotifyCanExecuteChangedFor(nameof(GenerateCommand), nameof(SubmitCommand), nameof(RetryCommand))]
    [ObservableProperty] private bool _isBusy;

    public async Task InitializeAsync(Guid workspaceId, string workspaceName)
    {
        _workspaceId = workspaceId; WorkspaceName = workspaceName;
        await ReloadHistoryAsync();
    }

    private async Task ReloadHistoryAsync()
    {
        try
        {
            var items = await quiz.ListAttemptsAsync(_workspaceId);
            History.Clear(); foreach (var item in items) History.Add(item);
        }
        catch (Exception ex) { Status = UserErrors.Describe(ex); }
    }

    private bool CanGenerate() => !IsBusy;
    [RelayCommand(CanExecute = nameof(CanGenerate))]
    private async Task GenerateAsync()
    {
        IsBusy = true; IsSubmitted = false; ScoreText = ""; Questions.Clear(); _draft = null;
        Status = "AI đang đọc tài liệu và soạn câu hỏi...";
        using var operation = new CancellationTokenSource(TimeSpan.FromMinutes(3)); _operation = operation;
        try
        {
            _draft = await quiz.GenerateAsync(_workspaceId, Topic, QuestionCount, operation.Token);
            var number = 1;
            foreach (var question in _draft.Questions) Questions.Add(new QuizQuestionItem(number++, question));
            Status = $"Đã tạo {Questions.Count} câu ({_draft.Session.Provider} · {_draft.Session.Model}). Chọn đáp án rồi bấm Nộp bài." +
                     (Questions.Count < QuestionCount ? $" AI chỉ trả về {Questions.Count}/{QuestionCount} câu hợp lệ." : "");
        }
        catch (QuizUnavailableException ex) { Status = ex.Message; }
        catch (Exception ex) { Status = UserErrors.Describe(ex); }
        finally { _operation = null; IsBusy = false; SubmitCommand.NotifyCanExecuteChanged(); RetryCommand.NotifyCanExecuteChanged(); }
    }

    private bool CanSubmit() => !IsBusy && !IsSubmitted && Questions.Count > 0;
    [RelayCommand(CanExecute = nameof(CanSubmit))]
    private async Task SubmitAsync()
    {
        if (_draft == null) return;
        var answers = Questions.Select(q => new QuizAnswer(q.Question, q.SelectedIndex)).ToList();
        foreach (var question in Questions) question.Reveal();
        var correct = QuizService.Grade(answers);
        var blank = answers.Count(a => a.SelectedIndex == null);
        ScoreText = $"Điểm: {correct}/{answers.Count} ({10.0 * correct / answers.Count:0.#}/10)" + (blank > 0 ? $" · bỏ trống {blank} câu" : "");
        IsSubmitted = true; IsBusy = true;
        try
        {
            await quiz.SaveAttemptAsync(_workspaceId, _draft, answers);
            Status = "Đã lưu kết quả vào lịch sử ôn tập của workspace.";
            await ReloadHistoryAsync();
        }
        catch (Exception ex) { Status = "Đã chấm điểm nhưng chưa lưu được lịch sử: " + UserErrors.Describe(ex); }
        finally { IsBusy = false; SubmitCommand.NotifyCanExecuteChanged(); RetryCommand.NotifyCanExecuteChanged(); }
    }

    private bool CanRetry() => !IsBusy && IsSubmitted;
    [RelayCommand(CanExecute = nameof(CanRetry))]
    private void Retry()
    {
        foreach (var question in Questions) question.Reset();
        IsSubmitted = false; ScoreText = ""; Status = "Làm lại bộ câu hỏi này. Lượt nộp mới cũng được lưu vào lịch sử.";
        SubmitCommand.NotifyCanExecuteChanged(); RetryCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand] private void Cancel() => _operation?.Cancel();
}
