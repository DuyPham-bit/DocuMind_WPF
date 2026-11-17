using DocuMind.Application.Common;
using System.Collections.ObjectModel;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DocuMind.Application.DTOs;
using DocuMind.Application.Interfaces;
using DocuMind.Application.Services;

namespace DocuMind.WPF.ViewModels;

public partial class QuizDocumentItem(DocumentListItem document) : ObservableObject
{
    public Guid Id { get; } = document.Id;
    public string Name { get; } = document.DisplayName;
    public string SourcePath { get; } = document.SourcePath;
    public bool IsAvailable { get; } = !document.NeedsReindex;
    public string StatusLabel => IsAvailable ? AppText.T("Ready", "Sẵn sàng") : AppText.T("Needs re-index", "Cần nạp lại tài liệu");
    [ObservableProperty] private bool _isSelected = !document.NeedsReindex;
}

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
    public string Title => AppText.T($"Q{Number}. {Question.Question}", $"Câu {Number}. {Question.Question}");
    public string GroupName { get; } = "q" + Guid.NewGuid().ToString("N");
    public List<QuizOptionItem> Options { get; }
    public string Explanation => string.IsNullOrWhiteSpace(Question.Explanation) ? "" : AppText.T("Explanation: ", "Giải thích: ") + Question.Explanation;
    public string SourceLabel => Question.Source == null
        ? AppText.T("Source: (the AI did not name a document passage)", "Nguồn: (AI không chỉ rõ đoạn tài liệu)")
        : AppText.T($"Source: {Question.Source.FileName} – page {Question.Source.PageNumber}", $"Nguồn: {Question.Source.FileName} – trang {Question.Source.PageNumber}");
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
        ResultText = selected == null ? AppText.T("⚪ Skipped", "⚪ Bỏ trống")
            : selected == Question.AnswerIndex ? AppText.T("✅ Correct", "✅ Đúng") : AppText.T("❌ Wrong", "❌ Sai");
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
    public ObservableCollection<QuizDocumentItem> Documents { get; } = new();
    public ObservableCollection<QuizQuestionItem> Questions { get; } = new();
    public ObservableCollection<QuizAttemptSummary> History { get; } = new();
    public IReadOnlyList<int> QuestionCounts { get; } = [3, 5, 7, 10];
    [NotifyPropertyChangedFor(nameof(HeaderSubtitle))]
    [ObservableProperty] private string _workspaceName = "";
    public string HeaderSubtitle => AppText.T($"Workspace: {WorkspaceName} · questions come from your indexed documents; the app grades them",
        $"Workspace: {WorkspaceName} · câu hỏi sinh từ tài liệu đã nạp, app tự chấm điểm");
    [ObservableProperty] private string _topic = "";
    [ObservableProperty] private int _questionCount = 5;
    [ObservableProperty] private string _status = AppText.T("Pick documents, add a topic if you like, then press Generate quiz.", "Chọn tài liệu, nhập chủ đề nếu cần rồi bấm Tạo quiz.");
    [ObservableProperty] private string _scoreText = "";
    [ObservableProperty] private bool _isSubmitted;
    [NotifyCanExecuteChangedFor(nameof(GenerateCommand), nameof(SubmitCommand), nameof(RetryCommand), nameof(SelectAllDocumentsCommand), nameof(ClearDocumentSelectionCommand))]
    [NotifyPropertyChangedFor(nameof(IsSelectionEditable))]
    [ObservableProperty] private bool _isBusy;
    public bool IsSelectionEditable => !IsBusy;
    public string DocumentSelectionSummary => Documents.Count == 0
        ? AppText.T("This workspace has no documents yet. Upload documents before creating a quiz.", "Workspace chưa có tài liệu. Hãy nạp tài liệu trước khi tạo quiz.")
        : Documents.All(d => !d.IsAvailable)
            ? AppText.T("No document is ready. Re-index your documents with the current embedding settings.", "Chưa có tài liệu sẵn sàng. Hãy nạp lại tài liệu với cấu hình embedding hiện tại.")
            : AppText.T($"Selected {Documents.Count(d => d.IsAvailable && d.IsSelected)}/{Documents.Count(d => d.IsAvailable)} ready documents. The quiz only uses the selected files.",
                $"Đã chọn {Documents.Count(d => d.IsAvailable && d.IsSelected)}/{Documents.Count(d => d.IsAvailable)} tài liệu sẵn sàng. Quiz chỉ dùng các file được chọn.");

    public async Task InitializeAsync(Guid workspaceId, string workspaceName)
    {
        _workspaceId = workspaceId; WorkspaceName = workspaceName;
        IsBusy = true;
        try
        {
            foreach (var document in Documents) document.PropertyChanged -= DocumentSelectionChanged;
            Documents.Clear();
            NotifyDocumentSelectionChanged();
            var documents = await quiz.ListDocumentsAsync(workspaceId);
            foreach (var document in documents)
            {
                var item = new QuizDocumentItem(document);
                item.PropertyChanged += DocumentSelectionChanged;
                Documents.Add(item);
            }
            NotifyDocumentSelectionChanged();
            await ReloadHistoryAsync();
        }
        catch (Exception ex) { Status = UserErrors.Describe(ex); }
        finally { IsBusy = false; }
    }

    private void DocumentSelectionChanged(object? sender, PropertyChangedEventArgs args)
    {
        if (args.PropertyName == nameof(QuizDocumentItem.IsSelected)) NotifyDocumentSelectionChanged();
    }

    private void NotifyDocumentSelectionChanged()
    {
        OnPropertyChanged(nameof(DocumentSelectionSummary));
        GenerateCommand.NotifyCanExecuteChanged();
    }

    private bool CanEditDocumentSelection() => !IsBusy;
    [RelayCommand(CanExecute = nameof(CanEditDocumentSelection))]
    private void SelectAllDocuments()
    {
        foreach (var document in Documents) document.IsSelected = document.IsAvailable;
    }

    [RelayCommand(CanExecute = nameof(CanEditDocumentSelection))]
    private void ClearDocumentSelection()
    {
        foreach (var document in Documents) document.IsSelected = false;
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

    private bool CanGenerate() => !IsBusy && Documents.Any(d => d.IsAvailable && d.IsSelected);
    [RelayCommand(CanExecute = nameof(CanGenerate))]
    private async Task GenerateAsync()
    {
        if (!CanGenerate()) return;
        var selectedIds = Documents.Where(d => d.IsAvailable && d.IsSelected).Select(d => d.Id).ToArray();
        var topic = Topic;
        var questionCount = QuestionCount;
        IsBusy = true; IsSubmitted = false; ScoreText = ""; Questions.Clear(); _draft = null;
        Status = AppText.T($"The AI is writing {questionCount} questions from {selectedIds.Length} selected document(s)...", $"AI đang soạn {questionCount} câu hỏi từ {selectedIds.Length} tài liệu đã chọn...");
        using var operation = new CancellationTokenSource(TimeSpan.FromMinutes(10)); _operation = operation;
        try
        {
            _draft = await quiz.GenerateAsync(_workspaceId, topic, questionCount, operation.Token, selectedIds);
            var number = 1;
            foreach (var question in _draft.Questions) Questions.Add(new QuizQuestionItem(number++, question));
            Status = AppText.T($"Created {Questions.Count} questions ({_draft.Session.Provider} · {_draft.Session.Model}). Choose your answers, then press Submit.",
                         $"Đã tạo {Questions.Count} câu ({_draft.Session.Provider} · {_draft.Session.Model}). Chọn đáp án rồi bấm Nộp bài.") +
                     (Questions.Count < questionCount ? AppText.T($" The AI returned only {Questions.Count}/{questionCount} valid questions.", $" AI chỉ trả về {Questions.Count}/{questionCount} câu hợp lệ.") : "");
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
        ScoreText = AppText.T($"Score: {correct}/{answers.Count} ({10.0 * correct / answers.Count:0.#}/10)", $"Điểm: {correct}/{answers.Count} ({10.0 * correct / answers.Count:0.#}/10)")
            + (blank > 0 ? AppText.T($" · {blank} skipped", $" · bỏ trống {blank} câu") : "");
        IsSubmitted = true; IsBusy = true;
        try
        {
            await quiz.SaveAttemptAsync(_workspaceId, _draft, answers);
            Status = AppText.T("Saved the result to this workspace's quiz history.", "Đã lưu kết quả vào lịch sử ôn tập của workspace.");
            await ReloadHistoryAsync();
        }
        catch (Exception ex) { Status = AppText.T("Graded, but the history could not be saved: ", "Đã chấm điểm nhưng chưa lưu được lịch sử: ") + UserErrors.Describe(ex); }
        finally { IsBusy = false; SubmitCommand.NotifyCanExecuteChanged(); RetryCommand.NotifyCanExecuteChanged(); }
    }

    private bool CanRetry() => !IsBusy && IsSubmitted;
    [RelayCommand(CanExecute = nameof(CanRetry))]
    private void Retry()
    {
        foreach (var question in Questions) question.Reset();
        IsSubmitted = false; ScoreText = ""; Status = AppText.T("Try this set again. The new attempt is also saved to the history.", "Làm lại bộ câu hỏi này. Lượt nộp mới cũng được lưu vào lịch sử.");
        SubmitCommand.NotifyCanExecuteChanged(); RetryCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand] private void Cancel() => _operation?.Cancel();
}
