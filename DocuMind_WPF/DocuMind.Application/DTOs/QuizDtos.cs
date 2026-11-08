namespace DocuMind.Application.DTOs;

/// <summary>Một câu trắc nghiệm do AI sinh từ tài liệu; AnswerIndex tính từ 0.</summary>
public sealed record QuizQuestion(string Question, List<string> Options, int AnswerIndex, string Explanation, RagSourceDto? Source);

/// <summary>Câu hỏi kèm lựa chọn của người học (null = bỏ trống), dùng để chấm và lưu.</summary>
public sealed record QuizAnswer(QuizQuestion Question, int? SelectedIndex)
{
    public bool IsCorrect => SelectedIndex == Question.AnswerIndex;
}

public sealed record QuizDraft(string Topic, List<QuizQuestion> Questions, ChatSession Session);

public sealed record QuizAttemptSummary(Guid Id, string Topic, int CorrectCount, int QuestionCount, string Model, DateTime CreatedAt)
{
    public string Label => $"{CreatedAt.ToLocalTime():dd/MM HH:mm} · {CorrectCount}/{QuestionCount} · {Topic}";
}
