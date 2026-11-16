# USER GUIDE: SETUP & CODE — DOCUMIND WPF (Project cuối kỳ PRN212)

DocuMind là app WPF trợ lý học tập: sinh viên nạp tài liệu môn học (PDF, TXT, MD) theo từng workspace, hỏi đáp với AI theo hai chế độ (chat ra ngoài và RAG), đổi nền tảng AI bằng API key và tự ôn tập bằng quiz có chấm điểm.

Đây là **user guide chính** của DocuMind, mô tả code hiện hành trên `main`. Tài liệu kỹ thuật sâu hơn nằm trong [docs/](docs/README.md): [đặc tả dự án](docs/07_Dac_ta_du_an.md), [Ollama local](docs/06_Ollama_Local.md), [Quiz ôn tập](docs/09_Quiz_On_Tap.md). Các file `docs/01`, `docs/02` là tư liệu của bản gốc `355df3d`, không còn đúng với code hiện tại.

---

## Mục lục

- [1. App đáp ứng đề bài thế nào](#1-app-đáp-ứng-đề-bài-thế-nào)
- [2. Chuẩn bị môi trường](#2-chuẩn-bị-môi-trường)
- [3. Build, test và chạy](#3-build-test-và-chạy)
- [4. Cấu hình nền tảng AI](#4-cấu-hình-nền-tảng-ai)
- [5. Hướng dẫn sử dụng](#5-hướng-dẫn-sử-dụng)
- [6. Kiến trúc code](#6-kiến-trúc-code)
- [7. Luồng code chính](#7-luồng-code-chính)
- [8. Database](#8-database)
- [9. Mở rộng](#9-mở-rộng)
- [10. Kiểm thử, xử lý sự cố và checklist demo](#10-kiểm-thử-xử-lý-sự-cố-và-checklist-demo)

---

## 1. App đáp ứng đề bài thế nào

| Yêu cầu đồ án | Trong DocuMind | Code chính |
| :--- | :--- | :--- |
| Chat với AI (chat ra ngoài) | Tắt **Search documents**, hỏi thẳng model; có lịch sử hội thoại | `RagOrchestratorService.ChatExternalAsync` |
| Chat với AI (RAG) | Bật **Search documents**: tìm đoạn tài liệu liên quan, AI trả lời kèm nguồn file/trang | `RagOrchestratorService.ChatWithRagAsync` |
| Cấu hình API key để hoán chuyển nền tảng | **Manage providers**: Gemini, OpenAI, Groq, OllamaLocal, OpenRouter; key mã hóa DPAPI | `SettingsViewModel`, `ChatProviderFactory` |
| Tính năng theo chủ đề (học tập) | Workspace theo môn, **Study quiz** chấm điểm, lịch sử hội thoại, gợi ý câu hỏi, export Markdown, mở đúng trang nguồn, giao diện EN/VI | `QuizService`, `MainViewModel`, `UiText` |

---

## 2. Chuẩn bị môi trường

| Thành phần | Yêu cầu |
| :--- | :--- |
| Hệ điều hành | **Windows 10/11** (WPF `net10.0-windows` và DPAPI chỉ chạy trên Windows) |
| SDK | **.NET 10 SDK**. Chỉ có runtime thì không build được |
| IDE (chọn 1) | **Visual Studio 2026** (hoặc bản 2022 đủ mới để mở `.slnx` và target .NET 10) với workload *.NET desktop development*; hoặc VS Code + C# Dev Kit dùng kèm CLI |
| AI (chọn 1) | **Ollama** trên máy (miễn phí, offline), hoặc API key OpenRouter (có model miễn phí), Gemini, OpenAI, Groq |
| Tùy chọn | `dotnet-ef` 10.0.14, chỉ cần khi tạo migration mới |

```powershell
dotnet --list-sdks          # phải có dòng 10.0.x
dotnet tool install --global dotnet-ef --version 10.0.14   # tùy chọn
```

> Quy định ".NET 8, Visual Studio 2022, không thêm NuGet" trong đề là luật của bài thi PE. Đồ án nhóm dùng .NET 10.

---

## 3. Build, test và chạy

Repo chứa 3 project (`ASM1`, `ASM2`, `DocuMind_WPF`). Clone vào thư mục **không có dấu tiếng Việt hoặc dấu cách**.

### 3.1 Bằng Visual Studio

1. Mở `DocuMind_WPF/DocuMind.slnx`.
2. Chuột phải `DocuMind.WPF` → **Set as Startup Project**, rồi nhấn **F5** (hoặc **Ctrl + F5**).
3. Chạy test: **Test → Run All Tests**.

### 3.2 Bằng dòng lệnh (từ thư mục gốc repo)

```powershell
cd DocuMind_WPF
dotnet restore DocuMind.slnx
dotnet build DocuMind.slnx --no-restore
dotnet test DocuMind.slnx --no-build
dotnet run --project DocuMind.WPF
```

Kết quả mong đợi: build 0 warning, 0 error; toàn bộ test pass (116 test tại thời điểm viết, ngày 17/11/2026). Test dùng AI/HTTP giả và SQLite tạm nên không cần API key và không gọi API thật.

### 3.3 Dữ liệu nằm ở đâu

- Database SQLite: `%LOCALAPPDATA%\DocuMind\documind_local.db`. Không phụ thuộc thư mục build, nên Debug/Release dùng chung dữ liệu.
- Khi khởi động, app tự chạy EF Core migrations. Nếu DB đã có dữ liệu và schema cần nâng cấp, app **tự backup** thành `documind_local.db.backup-<thời gian UTC>` trước khi migrate.
- Lần đầu chạy có sẵn workspace **My Workspace**.
- Ngôn ngữ giao diện được lưu ở `%LOCALAPPDATA%\DocuMind\ui-settings.json`.
- App chỉ cho mở một cửa sổ cho mỗi tài khoản Windows (`Mutex`).
- Không commit DB hoặc file backup lên Git: trong đó có lịch sử chat và API key đã mã hóa.

---

## 4. Cấu hình nền tảng AI

Lần đầu mở app, góc dưới thanh bên trái hiện **Not configured**. Bấm **Manage providers** (tiếng Việt: *Cài đặt API & Model*).

> **Nguyên tắc:** **Test connection** và **Test embedding** chỉ thử cấu hình nháp trên form, **không lưu**. Chỉ **Save provider** mới lưu và kích hoạt provider.

### 4.1 Cách 1 — Ollama local (miễn phí, chạy offline)

```powershell
winget install --id Ollama.Ollama --exact --accept-package-agreements --accept-source-agreements
ollama pull qwen3:4b-instruct
ollama pull bge-m3
ollama list
```

Trong **Manage providers** điền: Provider `OllamaLocal`, API key để trống, Chat model `qwen3:4b-instruct`, Embedding model `bge-m3`, Base URL `http://localhost:11434/v1`, Temperature `0.3`, tick **Use this provider for document embeddings and RAG**. Bấm **Test connection** → **Test embedding** → **Save provider**. Chi tiết và cách đổi thư mục chứa model: [docs/06_Ollama_Local.md](docs/06_Ollama_Local.md).

### 4.2 Cách 2 — OpenRouter (không cần cài model)

1. Tạo API key tại OpenRouter.
2. Bấm **Use OpenRouter** ở đầu hộp thoại: app tự điền chat `openrouter/free`, embedding `nvidia/nemotron-3-embed-1b:free`, Base URL `https://openrouter.ai/api/v1`, Temperature `0.3` và bật vai trò embedding/RAG.
3. Dán key vào ô API key (giữ chuột trên **Hold to reveal** để xem tạm).
4. **Test connection** → **Test embedding** → **Save provider**. Model miễn phí có giới hạn lượt gọi.

### 4.3 Các provider khác

| Provider | Chat model mặc định | Embedding mặc định | Base URL mặc định | Ghi chú |
| :--- | :--- | :--- | :--- | :--- |
| Gemini | `gemini-3.5-flash` | `gemini-embedding-001` | `https://generativelanguage.googleapis.com/v1beta` | Tự bật embedding/RAG. Nếu model chat chính lỗi, app thử lần lượt `gemini-flash-latest`, `gemini-flash-lite-latest` |
| OpenAI | `gpt-4o-mini` | `text-embedding-3-small` | `https://api.openai.com/v1` | Tính phí; cần quyền dùng cả chat và embeddings |
| Groq | `llama-3.3-70b-versatile` | (không có) | `https://api.groq.com/openai/v1` | Chỉ chat. Dùng RAG thì chọn provider embedding khác |

Base URL kết thúc ở `/v1` (Gemini là `/v1beta`), không thêm `/chat/completions`. App chỉ nhận HTTPS hoặc HTTP `localhost`, và không nhận URL có key hay query.

### 4.4 Hoán chuyển nền tảng và bảo vệ key

- **Chat và embedding là hai vai trò riêng.** Ví dụ chat bằng Groq nhưng embedding bằng Ollama: lưu Ollama với tick embedding trước, sau đó lưu Groq **không** tick, provider embedding cũ vẫn được giữ.
- Đổi provider/model **chat** không ảnh hưởng tài liệu đã nạp. Đổi provider/model/URL **embedding** thì phải nạp lại tài liệu: file cũ hiện **Embedding changed · Re-index ↻** và không được dùng làm nguồn cho tới khi nạp lại (bấm vào nhãn đó để nạp lại), vì vector của hai model khác nhau không so sánh được.
- Key được mã hóa bằng **Windows DPAPI (CurrentUser)** trước khi lưu DB, chỉ đúng tài khoản Windows đó giải mã được. Mã hóa lỗi thì app báo lỗi và không lưu. Chuyển máy hoặc tài khoản thì nhập lại key. Không đưa key vào source, ảnh chụp màn hình hay script SQL.

---

## 5. Hướng dẫn sử dụng

Nút **EN / VI** đổi toàn bộ giao diện chính giữa tiếng Anh và tiếng Việt. Bên dưới ghi tên nút tiếng Anh, tên tiếng Việt trong ngoặc.

1. **Workspace (mỗi môn một workspace):** chọn ở dropdown **WORKSPACE**; mở **Workspace settings** để sửa **NAME**, **SYSTEM INSTRUCTION** rồi **Save**, hoặc **New** / **Delete**. Xóa workspace sẽ xóa luôn tài liệu, index, lịch sử chat và lịch sử quiz của workspace đó (có hộp xác nhận).
2. **Nạp tài liệu:** **＋ Upload documents** (*Nạp tài liệu*), chọn một hoặc nhiều file `.pdf`, `.txt`, `.md`. Trạng thái hiện tiến độ từng đoạn; bấm **Cancel** để dừng. Nạp lại cùng file chỉ thay bản cũ khi bản mới hoàn tất, lỗi giữa chừng thì bản cũ vẫn dùng được. PDF scan dạng ảnh cần OCR trước.
   - Nhãn dưới tên file: **Indexed · Ready** (*Đã nạp · Sẵn sàng*) là dùng được; **Indexing…** là đang nạp; **Index failed · Retry ↻** (*Nạp lỗi · Thử lại*) là lần nạp bị lỗi hoặc bị hủy; **Embedding changed · Re-index ↻** là file được nạp bằng model embedding khác.
   - Hai nhãn có ↻ bấm được: app nạp lại file từ đường dẫn gốc. Nếu file đã bị di chuyển hoặc xóa, app báo và bạn dùng **Upload documents** để chọn lại.
3. **Chat ra ngoài:** tắt **Search documents** (*Tra cứu tài liệu (RAG)*), gõ câu hỏi, **Enter** để gửi, **Shift + Enter** để xuống dòng. Câu hỏi đi kèm system instruction và các lượt chat gần nhất cùng chế độ.
4. **Chat RAG:** bật **Search documents**. Câu trả lời có khung **SOURCES** (file, trang, độ khớp); **Open source** mở file gốc, PDF mở bằng Microsoft Edge đúng trang. Nếu không có đoạn đủ liên quan, app nói rõ là không đủ căn cứ và không gọi AI.
5. **Lịch sử hội thoại:** tab **HISTORY** ở thanh bên liệt kê các cuộc trò chuyện đã lưu của workspace; bấm để mở lại, nút xóa để xóa một cuộc. **New chat** (*Chat mới*) bắt đầu cuộc mới, không xóa cuộc cũ.
6. **QUICK START:** các gợi ý *Summarize*, *Study questions*, *Explain concepts* điền sẵn câu hỏi vào ô nhập để sửa rồi gửi.
7. **Study quiz** (*Quiz ôn tập*):
   - Trong **Tài liệu làm quiz**, tích file muốn dùng (mặc định chọn mọi file sẵn sàng; có **Chọn tất cả** / **Bỏ chọn**). File cần nạp lại bị khóa.
   - Nhập **Chủ đề** để lấy các đoạn liên quan nhất, hoặc để trống để lấy mẫu rải đều trong các file đã chọn. Chọn **Số câu** (3/5/7/10) rồi **✨ Tạo quiz**.
   - Chọn đáp án, **Nộp bài**: đáp án đúng tô xanh, lựa chọn sai tô đỏ, mỗi câu có giải thích và nguồn. **↺ Làm lại** để làm lại cùng bộ câu; mỗi lần nộp được lưu vào **LỊCH SỬ ÔN TẬP**.
   - Chi tiết: [docs/09_Quiz_On_Tap.md](docs/09_Quiz_On_Tap.md).
8. **Export** (*Xuất MD*): lưu hội thoại đang mở ra file `.md`, kèm mode, provider, model, temperature và nguồn của từng tin nhắn.

---

## 6. Kiến trúc code

Solution theo Clean Architecture, phụ thuộc chỉ hướng vào trong:

```text
DocuMind.WPF  ──►  DocuMind.Infrastructure  ──►  DocuMind.Application  ──►  DocuMind.Domain
 (View, ViewModel,     (EF Core SQLite, HTTP AI,      (Interface, DTO,            (Entity, Enum)
  composition root)     PdfPig, DPAPI)                 RAG, Quiz service)
DocuMind.Tests: xUnit, test mọi tầng bằng SQLite file thật và AI/HTTP giả
```

| Project | Target | Nội dung chính |
| :--- | :--- | :--- |
| `DocuMind.Domain` | `net10.0` | Entity `Workspace`, `Document`/`DocumentVersion`/`DocumentChunk`, `EmbeddingProfile`, `ProviderSetting`, `ChatMessage`, `QuizAttempt`; enum `AiProviderType`, `ChatMode`, `MessageRole`, `DocumentVersionStatus` |
| `DocuMind.Application` | `net10.0` | Interface `IChatCompletionService`, `IEmbeddingService`, `IChatProviderFactory`, `IDocumentStore`, `IChatHistoryStore`, `IQuizStore`, `IDocumentParser`, `ISecretProtector`, `IRepository<T>`; `RagOrchestratorService`, `QuizService`, `VectorMathHelper`, `UserErrors` |
| `DocuMind.Infrastructure` | `net10.0` | `AppDbContext` + migrations + `DatabaseMaintenance`; `EfDocumentStore`, `EfChatHistoryStore`, `EfQuizStore`, `EfRepository<T>`; `OpenAiCompatibleService` (OpenAI/Groq/Ollama/OpenRouter), `GeminiAiService`, `ChatProviderFactory`; `PdfPigDocumentParser`; `WindowsDpapiSecretProtector` |
| `DocuMind.WPF` | `net10.0-windows` | `App.xaml.cs` (DI), `MainWindow`, `Views/SettingsDialog`, `Views/QuizWindow`, ViewModels, `Controls/MarkdownView`, `Localization/UiText` (EN/VI), `Styles/FuturisticTheme.xaml` |
| `DocuMind.Tests` | `net10.0-windows` | xUnit: chunking, vector, DPAPI, migration, provider, RAG, workspace, lịch sử, Markdown, Quiz, smoke test WPF |

Package: EF Core SQLite 10.0.14, PdfPig 0.1.16, System.Security.Cryptography.ProtectedData 10.0.14, CommunityToolkit.Mvvm 8.4.2, Microsoft.Extensions.DependencyInjection 10.0.14.

### 6.1 Dependency Injection (composition root)

Toàn bộ object được ghép tại [`DocuMind.WPF/App.xaml.cs`](DocuMind.WPF/App.xaml.cs). ViewModel và Window chỉ nhận interface qua constructor, nên test có thể thay bằng bản giả.

```csharp
services.AddDbContextFactory<AppDbContext>(o => o.UseSqlite(DatabaseMaintenance.ConnectionString(dbPath)));
services.AddSingleton(_ => new HttpClient { Timeout = TimeSpan.FromMinutes(10) });
services.AddScoped<IDocumentParser, PdfPigDocumentParser>();
services.AddSingleton<ISecretProtector, WindowsDpapiSecretProtector>();
services.AddSingleton<IDocumentStore, EfDocumentStore>();
services.AddSingleton<IChatHistoryStore, EfChatHistoryStore>();
services.AddSingleton<IQuizStore, EfQuizStore>();
services.AddScoped<IChatProviderFactory, ChatProviderFactory>();
services.AddScoped<RagOrchestratorService>();
services.AddScoped<QuizService>();
services.AddTransient<MainViewModel>();   // + SettingsViewModel, QuizViewModel, MainWindow, SettingsDialog
```

`IDbContextFactory` cho mỗi thao tác DB một `DbContext` riêng rồi dispose ngay, không giữ context qua các lời gọi HTTP, tránh lỗi "a second operation was started on this context".

### 6.2 MVVM với CommunityToolkit

```csharp
public partial class QuizViewModel : ObservableObject
{
    [ObservableProperty] private string _topic = "";        // sinh property Topic + PropertyChanged
    [NotifyCanExecuteChangedFor(nameof(GenerateCommand))]
    [ObservableProperty] private bool _isBusy;

    [RelayCommand(CanExecute = nameof(CanGenerate))]          // sinh GenerateCommand
    private async Task GenerateAsync() { ... }
}
```

XAML chỉ binding: `Text="{Binding Topic, UpdateSourceTrigger=PropertyChanged}"`, `Command="{Binding GenerateCommand}"`. Class phải `partial`; không sửa file sinh tự động trong `obj`.

---

## 7. Luồng code chính

### 7.1 Khởi động

`App.OnStartup` → `Mutex` một instance → `DatabaseMaintenance.InitializeAsync` (backup nếu cần, nhận baseline DB cũ, `MigrateAsync`, chuyển phiên bản Staging dở dang thành Failed) → nạp ngôn ngữ đã lưu → resolve `MainWindow` → `MainViewModel.InitializeAsync` tải workspace, tài liệu, lịch sử và provider đang dùng.

### 7.2 Chat ra ngoài

`MainViewModel.SendMessageAsync` chụp workspace, conversation và mode hiện tại → `GetChatSessionAsync` (provider, model, temperature đã lưu) → tải lịch sử đã cắt theo ngân sách khoảng 16.000 byte → `ChatExternalAsync` → `IChatCompletionService.GenerateResponseAsync` gửi system + history + câu hỏi → lưu cặp user/assistant vào `ChatMessages`. Giới hạn chờ 10 phút, có **Cancel**.

### 7.3 Nạp tài liệu (ingest)

`RagOrchestratorService.IngestDocumentAsync`: `BeginAsync` tạo version **Staging** → copy file sang bản tạm, tính SHA-256 → `IDocumentParser` tách theo trang → `VectorMathHelper.ChunkText` (180 từ, chồng 30 từ) → gọi embedding từng đoạn, kiểm tra vector hợp lệ và cùng số chiều → `ActivateAsync` ghi chunk + `EmbeddingProfile` và thay bản **Ready** cũ trong một transaction ngắn. Lỗi ở bất kỳ bước nào thì `FailAsync`, bản Ready cũ giữ nguyên. Embedding thử lại tối đa 3 lần với lỗi 408/429/5xx, không thử lại với 401/403. Giới hạn 10 phút mỗi file.

### 7.4 Chat RAG

`ChatWithRagAsync` → `RetrieveSourcesAsync`: embedding câu hỏi → chỉ lấy chunk của version Ready có **cùng profile** (provider, model, endpoint, số chiều, chuẩn hóa, pipeline) → cosine similarity, giữ điểm ≥ 0.3, lấy top 4 → nếu rỗng thì trả "không đủ căn cứ" và không gọi chat. Có nguồn thì đoạn tài liệu được đóng gói JSON trong khối `DOCUMENT_DATA_<guid ngẫu nhiên>`; system prompt dặn coi đó là dữ liệu không tin cậy (chống prompt injection) và yêu cầu trích dẫn file, trang.

### 7.5 Lưu và hoán chuyển provider

`SettingsViewModel` giữ một **draft**. Mỗi lần đổi provider, form xóa key ngay rồi tải đúng cấu hình đã lưu của provider đó (bỏ kết quả tải cũ nếu người dùng đổi nhanh). Nút Test tạo service tạm từ draft (`CreateTemporary`), không ghi DB, giới hạn 2 phút. `ChatProviderFactory.SaveAsync` kiểm tra URL, mã hóa key **trước** khi mở transaction, rồi đặt `IsActive` / `IsEmbeddingActive` trong cùng transaction; unique index trong DB bảo đảm chỉ một provider chat và một provider embedding được active.

### 7.6 Quiz ôn tập

`QuizService.GenerateAsync` lấy căn cứ trong các tài liệu đã chọn (`RetrieveSourcesAsync` theo chủ đề, hoặc `SampleSourcesAsync` rải đều) → prompt yêu cầu JSON `{"questions":[...]}` → `Parse` đọc phòng thủ, bỏ câu sai cấu trúc và xáo thứ tự lựa chọn → người học làm bài → `Grade` chấm tại app → `SaveAttemptAsync` lưu `QuizAttempts`.

---

## 8. Database

| Bảng | Vai trò |
| :--- | :--- |
| `Workspaces` | Không gian theo môn, system prompt, conversation hiện tại |
| `Documents` | Tài liệu theo workspace + đường dẫn (duy nhất), hash |
| `DocumentVersions` | Staging / Ready / Failed, gắn `EmbeddingProfile`; mỗi tài liệu tối đa một Ready |
| `DocumentChunks` | Đoạn văn, số trang, thứ tự, vector JSON |
| `EmbeddingProfiles` | Không gian vector: provider, model, endpoint, số chiều, pipeline |
| `ProviderSettings` | Cấu hình từng provider, key đã mã hóa, cờ active chat/embedding |
| `ChatMessages` | Lịch sử chat kèm mode, provider, model, temperature, nguồn |
| `QuizAttempts` | Lượt nộp quiz: chủ đề, điểm, câu hỏi và lựa chọn |

Xóa workspace sẽ cascade xóa tài liệu, version, chunk, chat và quiz. Script tạo DB trống từ migrations: [docs/Database_SQLite.txt](docs/Database_SQLite.txt) (chạy một lần trên DB trống; app tự nâng cấp DB cũ).

Thêm/sửa entity thì tạo migration rồi sinh lại script (chạy trong `DocuMind_WPF`):

```powershell
dotnet ef migrations add TenMigration --project DocuMind.Infrastructure
dotnet ef migrations has-pending-model-changes --project DocuMind.Infrastructure
dotnet ef migrations script --project DocuMind.Infrastructure --output docs/Database_SQLite.txt
dotnet run --project docs/tools/DbCheck -- sqlite      # kiểm tra script, không mở DB thật
```

Backup/restore ngoại tuyến (đóng app trước): `dotnet run --project docs/tools/DbCheck -- backup <db nguồn> <file backup>` và `-- restore <file backup> <db đích>`.

---

## 9. Mở rộng

- **Thêm provider AI:** thêm giá trị vào `AiProviderType`. API tương thích OpenAI chỉ cần thêm mặc định trong `ChatProviderFactory.Defaults` và `EffectiveEndpoint` (cách OpenRouter được thêm). API khác thì viết class mới implement `IChatCompletionService` (và `IEmbeddingService` nếu có) rồi rẽ nhánh trong `CreateTemporary`. Viết test bằng `FakeHandler` để kiểm tra URL, header, payload mà không cần key thật.
- **Thêm tính năng theo kiểu Quiz:** entity ở Domain → interface store ở Application → service ở Application (chỉ phụ thuộc interface) → store EF + migration ở Infrastructure → đăng ký DI trong `App.xaml.cs` → ViewModel + View ở WPF → test ở `DocuMind.Tests`.
- **Thêm định dạng tài liệu:** implement `IDocumentParser` (trả danh sách `(PageNumber, Text)`), đăng ký DI và mở rộng bộ lọc file trong `UploadDocumentAsync`.
- **Thêm chữ giao diện:** thêm property vào `Localization/UiText.cs` (cặp tiếng Việt/tiếng Anh) rồi binding trong XAML.

---

## 10. Kiểm thử, xử lý sự cố và checklist demo

Bộ test bao phủ: chunking và cosine; DPAPI; migration DB cũ, backup/restore; lưu cấu hình provider (Test không ghi DB, không lẫn key giữa provider, URL/header/payload, temperature, retry); ingest giữ bản cũ khi lỗi, chặn index không tương thích, ranh giới prompt injection; workspace và lịch sử hội thoại; Markdown; Quiz (parser, chấm điểm, chọn tài liệu, lưu, ViewModel); smoke test mở `MainWindow`, `SettingsDialog`, `QuizWindow`. Test không gọi API thật, nên vẫn cần thử tay với provider thật trước khi demo.

| Triệu chứng | Nguyên nhân thường gặp | Cách xử lý |
| :--- | :--- | :--- |
| Không mở được `.slnx` / không nhận `net10.0` | IDE hoặc SDK cũ | Cài .NET 10 SDK, cập nhật Visual Studio hoặc dùng CLI |
| **Not configured** / yêu cầu mở Settings | Chưa lưu provider chat | **Manage providers** → Test → **Save provider** |
| Báo chưa chọn provider embedding khi bật RAG/Quiz | Chưa bật vai trò embedding | Tick **Use this provider for document embeddings and RAG** và lưu |
| File hiện **Index failed · Retry ↻** | Lần nạp bị lỗi (mạng, key, quota) hoặc bị hủy | Sửa nguyên nhân (thường là cấu hình provider) rồi bấm vào nhãn để nạp lại |
| File hiện **Embedding changed · Re-index ↻** hoặc RAG báo không đủ căn cứ | Đổi embedding model, index của bản cũ, hoặc PDF scan | Bấm nhãn để nạp lại; PDF scan cần OCR |
| HTTP 401/403 | Key sai, nhập nhầm provider, hết quyền model | Nhập lại key đúng provider |
| HTTP 404 | Sai tên model hoặc Base URL | Đối chiếu tên model; URL kết thúc ở `/v1` |
| HTTP 429 | Hết quota (model miễn phí) | Đợi, đổi model, hoặc dùng Ollama local |
| Không kết nối được Ollama | Ollama chưa chạy hoặc chưa pull đúng model | Mở Ollama ở khay hệ thống, `ollama list`, `ollama pull <model>` |
| Không giải mã được key cũ | DB copy từ máy/tài khoản Windows khác | Nhập lại key và **Save provider** |
| **Open source** không mở được | File gốc đã đổi tên, di chuyển hoặc xóa | Kiểm tra file gốc; lịch sử chat vẫn giữ đoạn trích |

**Giới hạn hiện tại:** chưa có OCR cho PDF scan, chưa đọc `.docx`, chưa streaming từng token khi trả lời; cửa sổ Quiz hiện chỉ có tiếng Việt.

**Checklist trước demo:**

1. `dotnet build` và `dotnet test` từ code mới nhất.
2. Lưu một provider có cả chat và embedding; che API key khi trình chiếu.
3. Nạp một file nhỏ (bộ PDF mẫu ở [`DemoProject/`](../DemoProject/README.md)); hỏi một câu có trong tài liệu và một câu ngoài tài liệu (RAG phải báo không đủ căn cứ).
4. Tắt **Search documents**, hỏi tiếp một câu để thấy chat ra ngoài có lịch sử hội thoại.
5. Đổi sang provider khác để demo hoán chuyển nền tảng.
6. Tạo một quiz 5 câu, nộp bài, xem lịch sử điểm.
7. Mở lại một cuộc trò chuyện cũ trong **HISTORY**, rồi **Export** ra Markdown.
