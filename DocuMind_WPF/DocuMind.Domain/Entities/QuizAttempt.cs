namespace DocuMind.Domain.Entities;

/// <summary>Một lượt làm Quiz ôn tập đã nộp bài, lưu theo workspace.</summary>
public class QuizAttempt
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid WorkspaceId { get; set; }
    public Workspace? Workspace { get; set; }
    public string Topic { get; set; } = "";
    public int QuestionCount { get; set; }
    public int CorrectCount { get; set; }
    /// <summary>JSON gồm câu hỏi, đáp án đúng, lựa chọn của người học và nguồn trích dẫn.</summary>
    public string QuestionsJson { get; set; } = "[]";
    public string Provider { get; set; } = "";
    public string Model { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
