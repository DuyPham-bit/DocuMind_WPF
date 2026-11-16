# DocuMind — User Guide về setup và code

DocuMind là app WPF trợ lý học tập: sinh viên nạp tài liệu môn học (PDF, TXT, MD) theo từng workspace, hỏi đáp với AI theo hai chế độ, đổi nền tảng AI bằng API key và tự ôn tập bằng quiz chấm điểm.

Tài liệu này mô tả **bản hiện hành** (nhánh `main` cộng tính năng Quiz, ngày 08/11/2026). `01_Setup_va_Code` và `02_Luong_hoat_dong` là tư liệu của bản gốc `355df3d`, không còn đúng với code hiện tại.

## 1. App đáp ứng đề bài thế nào

| Yêu cầu đồ án | Trong DocuMind | Code chính |
| --- | --- | --- |
| Chat với AI (chat ra ngoài) | Tắt "Tra cứu RAG", hỏi trực tiếp model; có lịch sử hội thoại | `RagOrchestratorService.ChatExternalAsync` |
| Chat với AI (RAG) | Bật "Tra cứu RAG": tìm đoạn tài liệu liên quan, AI trả lời kèm nguồn file/trang | `RagOrchestratorService.ChatWithRagAsync` |
| Cấu hình API key để hoán chuyển nền tảng | Cài đặt: Gemini, OpenAI, Groq, OllamaLocal, OpenRouter; key mã hóa DPAPI | `SettingsViewModel`, `ChatProviderFactory` |
| Tính năng theo chủ đề (học tập) | Workspace theo môn, prompt mẫu, **Quiz ôn tập chấm điểm**, export Markdown, mở đúng trang nguồn | `QuizService`, `MainViewModel` |

## 2. Chuẩn bị môi trường

| Thành phần | Yêu cầu |
| --- | --- |
| Hệ điều hành | Windows 10/11 (WPF và DPAPI chỉ chạy trên Windows) |
| SDK | **.NET 10 SDK** (`net10.0`, `net10.0-windows`). Chỉ có runtime thì không build được |
| IDE | **Visual Studio 2026** (hoặc bản 2022 đủ mới để mở `.slnx` và target .NET 10), workload ".NET desktop development" |
| AI | Một trong: API key OpenRouter (có model miễn phí), Gemini, OpenAI, Groq; hoặc Ollama cài trên máy |
| Công cụ tùy chọn | `dotnet-ef` 10.0.14 (chỉ cần khi tạo migration mới) |

```powershell
dotnet --list-sdks          # phải có dòng 10.0.x
dotnet tool install --global dotnet-ef --version 10.0.14   # tùy chọn
```

> Ghi chú: quy định ".NET 8, VS 2022, không thêm NuGet" trong đề là luật của bài thi PE. Đồ án nhóm đang dùng .NET 10; nếu giảng viên yêu cầu .NET 8 cho đồ án thì phải hạ cả EF Core/Microsoft.Extensions về 8.x và tạo lại migrations, hãy hỏi trước khi đổi.

## 3. Chạy lần đầu

### 3.1 Bằng Visual Studio

1. `git clone https://github.com/DuyPham-bit/DocuMind_WPF` (không đặt trong thư mục có dấu tiếng Việt hoặc dấu cách).
2. Mở `DocuMind_WPF/DocuMind.slnx` (repo gốc chứa ASM1, ASM2 và DocuMind_WPF).
3. Chuột phải `DocuMind.WPF` → **Set as Startup Project**, chọn Debug rồi **F5**.
4. Chạy test: **Test → Run All Tests** (Test Explorer).

### 3.2 Bằng dòng lệnh

```powershell
cd DocuMind_WPF
dotnet restore DocuMind.slnx
dotnet build DocuMind.slnx
dotnet test DocuMind.slnx
dotnet run --project DocuMind.WPF
```

Kết quả mong đợi: build 0 warning, 0 error; 97 test pass. Test dùng AI/HTTP giả và SQLite tạm, không cần key và không gọi API thật.

### 3.3 Dữ liệu nằm ở đâu

- Database: `%LOCALAPPDATA%\DocuMind\documind_local.db` (SQLite). Không phụ thuộc thư mục build, nên Debug/Release dùng chung dữ liệu.
- Khi khởi động, app tự chạy EF migrations. Nếu DB đã có dữ liệu mà schema cần nâng cấp, app **tự backup** thành `documind_local.db.backup-<thời gian UTC>` trước khi migrate.
- App chỉ cho chạy một cửa sổ cho mỗi tài khoản Windows.
- Không commit DB/backup lên Git: trong đó có lịch sử chat và key đã mã hóa.

## 4. Cấu hình nền tảng AI

Mở **⚙️ Cài đặt API Key & Nền tảng** ở góc dưới thanh bên.

| Provider | Chat model mặc định | Embedding mặc định | Base URL mặc định | Ghi chú |
| --- | --- | --- | --- | --- |
| OpenRouter | `openrouter/free` | `nvidia/nemotron-3-embed-1b:free` | `https://openrouter.ai/api/v1` | Nút **Dùng OpenRouter** điền sẵn; model free có quota |
| Gemini | `gemini-2.5-flash` | `text-embedding-004` | `https://generativelanguage.googleapis.com/v1beta` | Embedding mặc định này Google đã thông báo ngừng; nên nhập model embedding còn hỗ trợ |
| OpenAI | `gpt-4o-mini` | `text-embedding-3-small` | `https://api.openai.com/v1` | Tính phí |
| Groq | `llama-3.3-70b-versatile` | (không có) | `https://api.groq.com/openai/v1` | Chỉ chat; RAG cần provider embedding khác |
| OllamaLocal | `llama3` (nhóm dùng `qwen3:4b-instruct`) | nhóm dùng `bge-m3` | `http://localhost:11434/v1` | Không cần key, chạy offline; xem [06_Ollama_Local.md](06_Ollama_Local.md) |

Các bước:

1. Chọn **Provider**, dán **API Key** (ô PasswordBox che ký tự; giữ chuột vào nút hiện key để xem tạm).
2. Kiểm tra **Chat Model**, **Embedding Model**, **Base URL** (kết thúc ở `/v1`, không thêm `/chat/completions`) và **Temperature** (0–2).
3. Muốn dùng RAG/Quiz bằng provider này thì bật **Dùng provider này cho embedding/RAG**.
4. Bấm **Kiểm tra kết nối** và **Kiểm tra embedding**. Hai nút này chỉ thử cấu hình nháp, **không lưu**.
5. Bấm **Lưu cấu hình**: chỉ lúc này provider mới được kích hoạt.

**Hoán chuyển nền tảng:** provider chat và provider embedding là hai vai trò riêng. Có thể chat bằng Groq nhưng embedding bằng Ollama. Đổi provider/model **chat** không ảnh hưởng tài liệu đã nạp. Đổi provider/model/URL **embedding** thì phải nạp lại tài liệu (re-index): tài liệu cũ hiện "(cần re-index)" và không được dùng làm nguồn cho tới khi nạp lại, vì vector của hai model không so sánh được với nhau.

**Bảo vệ key:** key được mã hóa bằng Windows DPAPI (CurrentUser) trước khi lưu DB; chỉ đúng tài khoản Windows đó giải mã được. Mã hóa lỗi thì app báo lỗi và không lưu. Chuyển máy hoặc tài khoản thì nhập lại key. Không đưa key vào source code, screenshot hay script SQL.

## 5. Hướng dẫn sử dụng

1. **Workspace (mỗi môn một workspace):** chọn ở thanh bên; sửa tên/system prompt rồi **Lưu**; **Tạo** workspace mới; **Xóa workspace** xóa kèm tài liệu, lịch sử chat và lịch sử quiz (có hộp xác nhận).
2. **Nạp tài liệu:** **➕ Nạp tài liệu**, chọn một hoặc nhiều file PDF/TXT/MD. Thanh trạng thái hiện tiến độ từng đoạn; bấm **Hủy** để dừng. Nạp lại cùng file sẽ thay phiên bản cũ chỉ khi phiên bản mới hoàn tất; lỗi giữa chừng thì bản cũ vẫn dùng được. PDF scan (ảnh) cần OCR trước.
3. **Chat ra ngoài:** tắt **🔍 Tra cứu RAG**, gõ câu hỏi, Enter để gửi (Shift+Enter xuống dòng).
4. **Chat RAG:** bật **🔍 Tra cứu RAG**. Câu trả lời có khung **Nguồn trích dẫn** (file, trang, độ khớp); **Mở nguồn/trang** mở PDF đúng trang bằng Edge. Nếu không có đoạn đủ liên quan, app nói rõ là không đủ căn cứ và không gọi AI.
5. **Lịch sử:** chat được lưu theo workspace; mở lại app vẫn thấy. **Cuộc chat mới** bắt đầu hội thoại mới, không xóa lịch sử cũ trong DB.
6. **Gợi ý câu hỏi nhanh:** các chip tóm tắt, câu hỏi ôn tập, giải thích khái niệm điền sẵn prompt vào ô nhập.
7. **Quiz ôn tập / Study quiz:** nhập chủ đề (hoặc để trống), chọn số câu, **Tạo quiz**, chọn đáp án, **Nộp bài** để xem điểm, giải thích và nguồn từng câu; có **Làm lại** và lịch sử điểm. Chi tiết ở [09_Quiz_On_Tap.md](09_Quiz_On_Tap.md).
8. **Xuất MD / Export:** lưu toàn bộ hội thoại kèm mode/provider/model/temperature và nguồn của từng tin nhắn.

## 6. Kiến trúc code

Solution theo Clean Architecture, phụ thuộc chỉ hướng vào trong:

```
DocuMind.WPF  ──►  DocuMind.Infrastructure  ──►  DocuMind.Application  ──►  DocuMind.Domain
   (View, ViewModel,      (EF Core SQLite, HTTP AI,       (Interface, DTO,             (Entity, Enum)
    composition root)      PdfPig, DPAPI)                  RAG, Quiz service)
DocuMind.Tests: xUnit, test mọi tầng bằng SQLite file thật và AI/HTTP giả
```

| Project | Nội dung chính |
| --- | --- |
| `DocuMind.Domain` | `Workspace`, `Document`/`DocumentVersion`/`DocumentChunk`, `EmbeddingProfile`, `ProviderSetting`, `ChatMessage`, `QuizAttempt`; enum `AiProviderType`, `ChatMode`, `MessageRole` |
| `DocuMind.Application` | Interface `IChatCompletionService`, `IEmbeddingService`, `IChatProviderFactory`, `IDocumentStore`, `IChatHistoryStore`, `IQuizStore`, `IDocumentParser`, `ISecretProtector`; `RagOrchestratorService`, `QuizService`, `VectorMathHelper`, `UserErrors` |
| `DocuMind.Infrastructure` | `AppDbContext` + migrations, `EfDocumentStore`, `EfChatHistoryStore`, `EfQuizStore`, `EfRepository<T>`; `OpenAiCompatibleService` (OpenAI/Groq/Ollama/OpenRouter), `GeminiAiService`, `ChatProviderFactory`; `PdfPigDocumentParser`; `WindowsDpapiSecretProtector` |
| `DocuMind.WPF` | `App.xaml.cs` (DI), `MainWindow`, `Views/SettingsDialog`, `Views/QuizWindow`, ViewModels, `Controls/MarkdownView` |

Package: EF Core SQLite 10.0.14, PdfPig 0.1.16, System.Security.Cryptography.ProtectedData 10.0.14, CommunityToolkit.Mvvm 8.4.2, Microsoft.Extensions.DependencyInjection 10.0.14.

### 6.1 Dependency Injection (composition root)

Toàn bộ object được ghép tại `DocuMind.WPF/App.xaml.cs`. ViewModel và Window chỉ nhận interface qua constructor, nên test có thể thay bằng bản giả.

```csharp
services.AddDbContextFactory<AppDbContext>(o => o.UseSqlite(DatabaseMaintenance.ConnectionString(dbPath)));
services.AddSingleton<HttpClient>();
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

Dùng `IDbContextFactory`: mỗi thao tác DB tạo và dispose một `DbContext` riêng, không giữ context dài hạn qua các lời gọi HTTP, tránh lỗi "a second operation was started on this context".

### 6.2 MVVM với CommunityToolkit

```csharp
public partial class QuizViewModel(QuizService quiz) : ObservableObject
{
    [ObservableProperty] private string _topic = "";        // sinh property Topic + PropertyChanged
    [NotifyCanExecuteChangedFor(nameof(GenerateCommand))]
    [ObservableProperty] private bool _isBusy;

    [RelayCommand(CanExecute = nameof(CanGenerate))]          // sinh GenerateCommand
    private async Task GenerateAsync() { ... }
}
```

XAML chỉ binding: `Text="{Binding Topic, UpdateSourceTrigger=PropertyChanged}"`, `Command="{Binding GenerateCommand}"`. Class phải `partial`; không sửa file sinh tự động trong `obj`.

## 7. Luồng code chính

### 7.1 Khởi động

`App.OnStartup` → mutex một instance → `DatabaseMaintenance.InitializeAsync` (backup nếu cần, nhận baseline DB cũ, `MigrateAsync`, chuyển phiên bản Staging dở dang thành Failed) → resolve `MainWindow` → `MainViewModel.InitializeAsync` tải workspace, lịch sử, tài liệu và provider đang dùng.

### 7.2 Chat ra ngoài

`MainViewModel.SendMessageAsync` chụp workspace/conversation/mode hiện tại → `GetChatSessionAsync` (provider, model, temperature đã lưu) → tải lịch sử đã cắt theo budget ~4.000 token → `ChatExternalAsync` → `IChatCompletionService.GenerateResponseAsync` gửi system + history + câu hỏi → lưu cặp user/assistant vào `ChatMessages`. Timeout 2 phút, có Hủy.

### 7.3 Nạp tài liệu (ingest)

`RagOrchestratorService.IngestDocumentAsync`: `BeginAsync` tạo version **Staging** → copy file sang bản tạm, tính SHA-256 → `IDocumentParser` tách theo trang → `VectorMathHelper.ChunkText` (180 từ, chồng 30 từ) → gọi embedding từng đoạn, kiểm tra vector hợp lệ và cùng chiều → `ActivateAsync` ghi chunks + `EmbeddingProfile` và thay bản **Ready** cũ trong một transaction ngắn. Lỗi ở bất kỳ bước nào → `FailAsync`, bản Ready cũ giữ nguyên. Embedding retry tối đa 3 lần cho 408/429/5xx, không retry 401/403.

### 7.4 Chat RAG

`ChatWithRagAsync` → `RetrieveSourcesAsync`: embedding câu hỏi → chỉ lấy chunk của version Ready có **cùng profile** (provider, model, endpoint, số chiều, chuẩn hóa, pipeline) → cosine similarity, giữ điểm ≥ 0.3, lấy top 4 → nếu rỗng thì trả "không đủ căn cứ" và không gọi chat. Có nguồn thì đoạn tài liệu được đóng gói JSON trong khối `DOCUMENT_DATA_<guid ngẫu nhiên>`, system prompt dặn coi đó là dữ liệu không tin cậy (chống prompt injection) và yêu cầu trích dẫn `[Nguồn: file - trang]`.

### 7.5 Lưu và hoán chuyển provider

`SettingsViewModel` giữ một **draft**. Mỗi lần đổi provider, form xóa key ngay rồi tải đúng cấu hình đã lưu của provider đó (bỏ kết quả tải cũ nếu người dùng đổi nhanh). Kiểm tra tạo service tạm từ draft (`CreateTemporary`), không ghi DB. `ChatProviderFactory.SaveAsync` validate (HTTPS hoặc HTTP localhost, không key/query trong URL), mã hóa key **trước** khi mở transaction, rồi đặt `IsActive`/`IsEmbeddingActive` trong cùng một transaction; DB có unique index bảo đảm chỉ một provider chat và một provider embedding được active.

### 7.6 Quiz ôn tập

`QuizService.GenerateAsync` lấy căn cứ (`RetrieveSourcesAsync` theo chủ đề, hoặc `SampleSourcesAsync` rải đều toàn workspace) → prompt yêu cầu JSON `{"questions":[...]}` → `Parse` đọc phòng thủ và bỏ câu sai cấu trúc → người học làm bài → `Grade` chấm tại app → `SaveAttemptAsync` lưu `QuizAttempts`.

## 8. Database

| Bảng | Vai trò |
| --- | --- |
| `Workspaces` | Không gian theo môn, system prompt, conversation hiện tại |
| `Documents` | Tài liệu theo workspace + đường dẫn (duy nhất), hash |
| `DocumentVersions` | Staging / Ready / Failed, gắn `EmbeddingProfile`; mỗi tài liệu tối đa một Ready |
| `DocumentChunks` | Đoạn văn, số trang, thứ tự, vector JSON |
| `EmbeddingProfiles` | Không gian vector: provider, model, endpoint, số chiều, pipeline |
| `ProviderSettings` | Cấu hình từng provider, key đã mã hóa, cờ active chat/embedding |
| `ChatMessages` | Lịch sử chat kèm mode, provider, model, temperature, nguồn |
| `QuizAttempts` | Lượt nộp quiz: chủ đề, điểm, câu hỏi và lựa chọn |

Xóa workspace sẽ cascade xóa tài liệu, version, chunk, chat và quiz. Script tạo DB trống từ migrations: [Database_SQLite.txt](Database_SQLite.txt) (chạy một lần trên DB trống; app tự nâng cấp DB cũ).

Thêm/sửa entity thì tạo migration rồi sinh lại script:

```powershell
dotnet ef migrations add TenMigration --project DocuMind.Infrastructure
dotnet ef migrations has-pending-model-changes --project DocuMind.Infrastructure
dotnet ef migrations script --project DocuMind.Infrastructure --output docs/Database_SQLite.txt
dotnet run --project docs/tools/DbCheck -- sqlite      # kiểm tra script, không mở DB thật
```

Backup/restore ngoại tuyến (đóng app trước): `dotnet run --project docs/tools/DbCheck -- backup <db nguồn> <file backup>` và `-- restore <file backup> <db đích>`.

## 9. Mở rộng

**Thêm provider AI:** thêm giá trị vào `AiProviderType`; nếu API tương thích OpenAI chỉ cần thêm mặc định trong `ChatProviderFactory.Defaults` và `EffectiveEndpoint` (cách OpenRouter được thêm); nếu khác thì viết class mới implement `IChatCompletionService` (và `IEmbeddingService` nếu có), rẽ nhánh trong `CreateTemporary`. Viết test bằng `FakeHandler` để kiểm tra URL, header, payload mà không cần key thật.

**Thêm tính năng theo kiểu Quiz:** entity ở Domain → interface store ở Application → service ở Application (chỉ phụ thuộc interface) → store EF + migration ở Infrastructure → đăng ký DI trong `App.xaml.cs` → ViewModel + View ở WPF → test ở `DocuMind.Tests`.

**Thêm định dạng tài liệu:** implement `IDocumentParser` (trả danh sách `(PageNumber, Text)`), đăng ký DI và mở rộng filter trong `UploadDocumentAsync`.

## 10. Kiểm thử và xử lý sự cố

Bộ 97 test bao phủ: chunking và cosine; DPAPI; migration DB cũ, backup/restore; lưu cấu hình provider (test không ghi DB, không lẫn key A/B, header/URL/payload, temperature, retry); ingest giữ bản cũ khi lỗi, chặn index không tương thích, ranh giới prompt-injection; workspace và lịch sử; Quiz (parser, chấm điểm, lưu, ViewModel); smoke test STA mở MainWindow, SettingsDialog, QuizWindow. Test không gọi API thật, nên vẫn cần thử tay với provider thật trước khi demo.

| Triệu chứng | Nguyên nhân thường gặp | Cách xử lý |
| --- | --- | --- |
| Không mở được `.slnx` / không nhận `net10.0` | IDE hoặc SDK cũ | Cài .NET 10 SDK, cập nhật Visual Studio hoặc dùng CLI |
| "Hãy mở Cài đặt và lưu provider chat" | Chưa Lưu cấu hình | Cài đặt → Kiểm tra → **Lưu cấu hình** |
| "Chưa chọn provider embedding" khi bật RAG/Quiz | Chưa bật vai trò embedding | Bật **Dùng provider này cho embedding/RAG** và Lưu |
| Tài liệu hiện "(cần re-index)" hoặc RAG báo không đủ căn cứ | Đổi model embedding, hoặc index của bản cũ | Nạp lại file nguồn |
| HTTP 401/403 | Key sai, sai provider, hết quyền model | Nhập lại key đúng provider |
| HTTP 404 | Sai model hoặc Base URL | Đối chiếu tên model; URL kết thúc ở `/v1` |
| HTTP 429 | Hết quota (model free) | Đợi, đổi model, hoặc dùng Ollama local |
| Không kết nối Ollama | Ollama chưa chạy hoặc chưa pull model | Mở Ollama, `ollama list`, `ollama pull <model>` |
| PDF nạp 0 đoạn | PDF scan/ảnh | OCR trước khi nạp |
| "Không thể giải mã key cũ" | DB copy từ máy/tài khoản khác | Nhập lại key và Lưu |

**Checklist trước demo:** build và test từ code mới nhất; Lưu provider có chat và embedding; nạp một file nhỏ, hỏi một câu có trong tài liệu và một câu ngoài tài liệu (RAG phải báo không đủ căn cứ); tắt RAG hỏi tiếp một câu để thấy lịch sử hội thoại; đổi sang provider khác để demo hoán chuyển; tạo một quiz 5 câu và nộp bài; xuất Markdown; che key khi trình chiếu.

## 11. Tài liệu liên quan

- [05_Fixes_va_Nghiem_thu.md](05_Fixes_va_Nghiem_thu.md): chi tiết các bản sửa và tiêu chí nghiệm thu.
- [06_Ollama_Local.md](06_Ollama_Local.md): cài Ollama và model chạy offline.
- [09_Quiz_On_Tap.md](09_Quiz_On_Tap.md): tính năng Quiz.
- CommunityToolkit.Mvvm: [ObservableProperty](https://learn.microsoft.com/en-us/dotnet/communitytoolkit/mvvm/generators/observableproperty), [RelayCommand](https://learn.microsoft.com/en-us/dotnet/communitytoolkit/mvvm/generators/relaycommand).
- EF Core: [DbContext lifetime và factory](https://learn.microsoft.com/en-us/ef/core/dbcontext-configuration/), [Migrations](https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/).
