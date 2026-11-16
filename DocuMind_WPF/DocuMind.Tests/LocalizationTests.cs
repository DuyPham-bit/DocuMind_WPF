using System.Net.Http;
using DocuMind.Application.Services;
using DocuMind.Domain.Enums;
using DocuMind.Infrastructure.AiProviders;
using DocuMind.WPF.Localization;

namespace DocuMind.Tests;

// The language switch is process-wide (AppText.Vietnamese), so every test that flips it runs alone,
// after the parallel tests, and puts English back when it finishes.
[CollectionDefinition("Language", DisableParallelization = true)]
public sealed class LanguageCollection;

[Collection("Language")]
public class LocalizationTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "DocuMindTests-lang-" + Guid.NewGuid().ToString("N"));

    [Fact]
    public void LanguageChoiceIsSavedAndRestoredOnTheNextStart()
    {
        var path = Path.Combine(_root, "ui-settings.json");
        var first = new UiText(); first.EnablePersistence(path);
        Assert.False(first.IsVietnamese);
        first.ToggleLanguageCommand.Execute(null);
        Assert.Contains("\"vi\"", File.ReadAllText(path));

        var next = new UiText(); next.EnablePersistence(path);
        Assert.True(next.IsVietnamese);
        Assert.Equal("KHÔNG GIAN LÀM VIỆC", next.WorkspaceHeader);
    }

    [Fact]
    public void DamagedSettingsFileFallsBackToEnglish()
    {
        Directory.CreateDirectory(_root);
        var path = Path.Combine(_root, "ui-settings.json");
        File.WriteAllText(path, "{ not json");
        var text = new UiText(); text.EnablePersistence(path);
        Assert.False(text.IsVietnamese);
    }

    [Fact]
    public void SwitchingLabelsRaisesOneRefreshForEveryBinding()
    {
        var text = new UiText(); var changed = new List<string?>();
        text.PropertyChanged += (_, e) => changed.Add(e.PropertyName);
        text.IsVietnamese = true;
        Assert.Contains(string.Empty, changed); // WPF re-reads every label, including ones added later
        Assert.Equal("Gửi  →", text.SendButton);
    }

    [Fact]
    public async Task StatusAndValidationMessagesFollowTheSelectedLanguage()
    {
        AppText.Vietnamese = true;
        Assert.Equal("Thao tác đã bị hủy hoặc hết thời gian chờ. Dữ liệu đã lưu vẫn được giữ nguyên.", UserErrors.Describe(new OperationCanceledException()));

        using var fixture = new SqliteFixture(); using var http = new HttpClient(new FakeHandler());
        var factory = new ChatProviderFactory(fixture, http, new FakeSecrets());
        var missingKey = await Assert.ThrowsAsync<UserFacingException>(() => factory.SaveAsync(new(AiProviderType.OpenAI, "", "gpt-4o-mini")));
        Assert.Equal("Hãy nhập API key cho provider này.", UserErrors.Describe(missingKey));

        var rag = new RagOrchestratorService(new FakeProviders(), new FakeParser(), new DocuMind.Infrastructure.Persistence.EfDocumentStore(fixture));
        Assert.StartsWith("Tài liệu tương thích chưa có đủ thông tin", (await rag.ChatWithRagAsync(SqliteFixture.WorkspaceId, "system", "question")).ResponseText);
    }

    public void Dispose()
    {
        AppText.Vietnamese = false;
        UiText.Current.IsVietnamese = false;
        try { if (Directory.Exists(_root)) Directory.Delete(_root, true); } catch (IOException) { }
    }
}
