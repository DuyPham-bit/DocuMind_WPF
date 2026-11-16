# USER GUIDE: SETUP, CẤU HÌNH AI & SỬ DỤNG DOCUMIND WPF

Tài liệu hướng dẫn từng bước cho phiên bản hiện hành của **DocuMind WPF** (commit `c3520cb` — `.NET 10`, Windows WPF + MVVM + Clean Architecture + SQLite + RAG), giúp bạn:
1. Cài đặt môi trường, build, chạy kiểm thử và khởi động ứng dụng desktop **DocuMind**.
2. Cấu hình nhà cung cấp AI (khuyên dùng **Ollama local miễn phí** hoặc **OpenRouter**, hỗ trợ thêm Gemini, OpenAI, Groq).
3. Sử dụng đầy đủ quy trình làm việc: tạo Workspace, nạp tài liệu (`PDF`, `TXT`, `MD`), hỏi đáp **Direct Chat** vs **RAG** (`Search documents`), xem nguồn trích dẫn và xuất hội thoại Markdown.
4. Nắm bản đồ kiến trúc 5 project (`Domain`, `Application`, `Infrastructure`, `WPF`, `Tests`) và xử lý lỗi thường gặp.

> **Tài liệu kỹ thuật chuyên sâu trong repo:** Nếu cần xem đặc tả đầy đủ hoặc lịch sử nghiệm thu, mở thư mục [docs/](file:///d:/CN5.1/PRN212/26.FA.PRN212/Project-Prn212/DocuMind_WPF/docs/README.md), đặc biệt là [07_Dac_ta_du_an.md](file:///d:/CN5.1/PRN212/26.FA.PRN212/Project-Prn212/DocuMind_WPF/docs/07_Dac_ta_du_an.md) và [06_Ollama_Local.md](file:///d:/CN5.1/PRN212/26.FA.PRN212/Project-Prn212/DocuMind_WPF/docs/06_Ollama_Local.md).

---

## Mục lục
- [I. Yêu cầu hệ thống & Chạy nhanh](#i-yêu-cầu-hệ-thống--chạy-nhanh)
  - [1. Yêu cầu môi trường](#1-yêu-cầu-môi-trường)
  - [2. Restore, Build, Test & Run](#2-restore-build-test--run)
  - [3. Vị trí lưu Database SQLite & Bảo mật API Key](#3-vị-trí-lưu-database-sqlite--bảo-mật-api-key)
- [II. Cấu hình AI Provider (Bắt buộc trước khi Chat)](#ii-cấu-hình-ai-provider-bắt-buộc-trước-khi-chat)
  - [1. Cách 1 (Khuyên dùng miễn phí offline): Ollama Local](#1-cách-1-khuyên-dùng-miễn-phí-offline-ollama-local)
  - [2. Cách 2 (Không cần cài model nặng): OpenRouter](#2-cách-2-không-cần-cài-model-nặng-openrouter)
  - [3. Các Provider khác (Gemini, OpenAI, Groq) & Quy tắc lưu cấu hình](#3-các-provider-khác-gemini-openai-groq--quy-tắc-lưu-cấu-hình)
- [III. Hướng dẫn sử dụng giao diện DocuMind](#iii-hướng-dẫn-sử-dụng-giao-diện-documind)
  - [1. Quản lý Workspace](#1-quản-lý-workspace)
  - [2. Nạp tài liệu (Ingest & Re-index)](#2-nạp-tài-liệu-ingest--re-index)
  - [3. Hỏi đáp: Direct Chat vs Search documents (RAG)](#3-hỏi-đáp-direct-chat-vs-search-documents-rag)
  - [4. Quick Start, Mở nguồn trích dẫn, New chat & Export Markdown](#4-quick-start-mở-nguồn-trích-dẫn-new-chat--export-markdown)
- [IV. Kiến trúc mã nguồn & Dependency Injection](#iv-kiến-trúc-mã-nguồn--dependency-injection)
  - [1. Cấu trúc 5 project trong `DocuMind.slnx`](#1-cấu-trúc-5-project-trong-documindslnx)
  - [2. Luồng xử lý chính (RAG & MVVM)](#2-luồng-xử-lý-chính-rag--mvvm)
- [V. Lỗi thường gặp & Giới hạn cần biết](#v-lỗi-thường-gặp--giới-hạn-cần-biết)

---

## I. Yêu cầu hệ thống & Chạy nhanh

### 1. Yêu cầu môi trường
- **Hệ điều hành:** **Windows** (bắt buộc vì `DocuMind.WPF` target `net10.0-windows` và mã hóa API key bằng **Windows DPAPI**).
- **.NET 10 SDK:** Kiểm tra bằng PowerShell:
  ```powershell
  dotnet --version
  ```
- **IDE (chọn 1):** Visual Studio 2022/2026 (có workload *.NET desktop development*), hoặc VS Code + C# Dev Kit dùng kèm CLI.
- **Mạng / AI Runtime:** Cần kết nối Internet nếu dùng OpenRouter/Gemini/OpenAI/Groq, hoặc cài **Ollama** nếu chạy model trên máy cá nhân.

---

### 2. Restore, Build, Test & Run

Mở PowerShell tại thư mục gốc `DocuMind_WPF` (nơi chứa `DocuMind.slnx`):

```powershell
# Bước 1: Di chuyển vào thư mục DocuMind_WPF
cd d:\CN5.1\PRN212\26.FA.PRN212\Project-Prn212\DocuMind_WPF

# Bước 2: Khôi phục NuGet packages & build toàn solution
dotnet restore DocuMind.slnx
dotnet build DocuMind.slnx --no-restore

# Bước 3: Chạy toàn bộ 68 unit/regression/STA smoke tests
dotnet test DocuMind.slnx --no-build

# Bước 4: Khởi chạy ứng dụng desktop WPF
dotnet run --project DocuMind.WPF/DocuMind.WPF.csproj
```

> **Lưu ý khi mở bằng Visual Studio:** Mở `DocuMind.slnx`, đặt `DocuMind.WPF` làm **Startup Project**, rồi nhấn `F5` hoặc `Ctrl + F5`. App có cơ chế single-instance bằng `Mutex` theo user Windows, nên mỗi lúc chỉ mở một cửa sổ DocuMind.

---

### 3. Vị trí lưu Database SQLite & Bảo mật API Key

- **Database SQLite:** Được tạo và tự nâng cấp bằng **EF Core Migrations** tại:
  ```text
  %LOCALAPPDATA%\DocuMind\documind_local.db
  ```
  Trước mỗi lần nâng cấp schema trên DB cũ đã có dữ liệu, `DatabaseMaintenance` tự tạo bản sao lưu cùng thư mục (`documind_local.db.backup-...`).
- **Workspace mặc định:** Sau khi migrate, ứng dụng tạo sẵn workspace tên **`My Workspace`**.
- **Bảo mật API Key:** `WindowsDpapiSecretManager` mã hóa API key bằng **Windows DPAPI (`DataProtectionScope.CurrentUser`)** trước khi lưu vào bảng `ProviderSettings`. Key chỉ giải mã được trên đúng máy và tài khoản Windows đã lưu nó — không chia sẻ file `.db` chứa key sang máy khác.

---

## II. Cấu hình AI Provider (Bắt buộc trước khi Chat)

Khi mở app lần đầu, góc dưới thanh bên trái hiển thị **`Not configured`**. Nhấn nút **`Manage providers`** để mở cửa sổ **DocuMind — AI Providers**.

> **Nguyên tắc an toàn trong Settings:** Nút **`Test connection`** và **`Test embedding`** chỉ kiểm tra bản nháp (**draft**) trên form, **chưa lưu** vào database. Chỉ khi nhấn **`Save provider`**, cấu hình mới được lưu và kích hoạt.

### 1. Cách 1 (Khuyên dùng miễn phí offline): Ollama Local

1. Cài Ollama trên Windows:
   ```powershell
   winget install --id Ollama.Ollama --exact --accept-package-agreements --accept-source-agreements
   ```
2. Tải cặp model chat và embedding gợi ý:
   ```powershell
   ollama pull qwen3:4b-instruct
   ollama pull bge-m3
   ollama list
   ```
3. Trong cửa sổ **Manage providers**, điền:
   - **Provider:** `OllamaLocal`
   - **API key:** Để trống
   - **Chat model:** `qwen3:4b-instruct`
   - **Embedding model:** `bge-m3`
   - **Base URL:** `http://localhost:11434/v1`
   - **Temperature:** `0.3` (hoặc tùy chỉnh `0.0 – 1.0`)
   - Tick chọn **`Use this provider for document embeddings and RAG`**
4. Bấm **`Test connection`** → **`Test embedding`** → **`Save provider`**.

---

### 2. Cách 2 (Không cần cài model nặng): OpenRouter

1. Tạo API key miễn phí tại OpenRouter.
2. Trong cửa sổ **Manage providers**, nhấn nút **`Use OpenRouter`** ở đầu hộp thoại để app tự điền sẵn:
   - **Chat model:** `openrouter/free`
   - **Embedding model:** `nvidia/nemotron-3-embed-1b:free`
   - **Base URL:** `https://openrouter.ai/api/v1`
   - **Temperature:** `0.3`
   - Tự bật **`Use this provider for document embeddings and RAG`**
3. Dán **API key** vào ô mật khẩu (có nút **`Hold to reveal`** nếu cần kiểm tra lại ký tự vừa dán).
4. Bấm **`Test connection`** → **`Test embedding`** → **`Save provider`**.

---

### 3. Các Provider khác (Gemini, OpenAI, Groq) & Quy tắc lưu cấu hình

| Provider | Chat model mặc định | Embedding model mặc định | Base URL mặc định | Ghi chú |
| :--- | :--- | :--- | :--- | :--- |
| **Gemini** | `gemini-2.5-flash` | `text-embedding-004` | `https://generativelanguage.googleapis.com/v1beta` | Hỗ trợ chat & embedding qua Google Gemini API |
| **OpenAI** | `gpt-4o-mini` | `text-embedding-3-small` | `https://api.openai.com/v1` | Cần tài khoản OpenAI có quyền dùng cả chat & embeddings |
| **Groq** | `llama-3.3-70b-versatile` | *(không có mặc định)* | `https://api.groq.com/openai/v1` | Dùng tốt cho **Direct Chat**; nếu dùng RAG hãy chọn provider embedding riêng (ví dụ OpenRouter hoặc OllamaLocal) |

> **Tách riêng Chat và Embedding:** Bạn có thể bật **Save provider** cho một provider **chỉ làm Chat** (bỏ tick *Use this provider for document embeddings and RAG*) mà vẫn giữ provider embedding đang hoạt động trước đó.

---

## III. Hướng dẫn sử dụng giao diện DocuMind

### 1. Quản lý Workspace
- Chọn workspace tại dropdown **WORKSPACE** ở thanh bên trái.
- Mở mục **`Workspace settings`** để sửa **NAME**, **SYSTEM INSTRUCTION**, hoặc bấm **`New`** / **`Save`** / **`Delete`**.
- Xóa workspace sẽ xóa toàn bộ tài liệu, version index và lịch sử hội thoại thuộc workspace đó (có hộp thoại xác nhận).

---

### 2. Nạp tài liệu (Ingest & Re-index)
1. Bấm nút **`+ Upload documents`**, chọn một hoặc nhiều file định dạng **`.pdf`**, **`.txt`**, hoặc **`.md`** (lưu ý: file PDF scan dạng ảnh cần OCR trước vì `PdfPigDocumentParser` chỉ trích xuất văn bản có sẵn).
2. Hệ thống thực hiện tuần tự:
   - Tính hash SHA-256 trên snapshot tệp tạm.
   - Đọc nội dung từng trang (PDF giữ đúng số trang; TXT/MD tính trang 1).
   - Chia đoạn theo cửa sổ trượt **`180 words`**, chồng lấn **`30 words`** (`words180-overlap30-v1`).
   - Gọi embedding model tạo vector và lưu ở trạng thái `Ready` (nếu lỗi hoặc bấm **`Cancel`** giữa chừng, index `Ready` cũ vẫn được giữ nguyên).
3. **Khi nào cần Re-index?** Mỗi index gắn với một `EmbeddingProfile` (Provider + Model + Endpoint + Dimension + PipelineVersion). Nếu bạn đổi **embedding provider/model**, cần nạp lại đúng file nguồn để tạo index tương thích.

---

### 3. Hỏi đáp: Direct Chat vs Search documents (RAG)

Ở thanh tiêu đề phía trên khung chat có công tắc **`Search documents`** (mặc định **Bật**):

- **Khi `Search documents` BẬT (Chế độ RAG):**
  1. Câu hỏi được chuyển thành vector embedding.
  2. Tìm các đoạn văn bản có độ tương đồng Cosine **`>= 0.3`** (`MinimumScore = 0.3f`) trong các tài liệu `Ready` khớp profile embedding hiện tại, lấy tối đa **top 4** đoạn (`topK = 4`).
  3. Gửi các đoạn tìm được kèm hướng dẫn chống prompt injection và yêu cầu trích dẫn nguồn `[Source: file name - page number]` cho AI trả lời.
  4. Nếu không có đoạn nào đạt ngưỡng hoặc chưa nạp tài liệu tương thích, hệ thống trả lời rõ là chưa đủ bằng chứng trong tài liệu.
- **Khi `Search documents` TẮT (Chế độ Direct Chat):**
  - Gửi thẳng System Instruction, lịch sử hội thoại gần nhất (cùng chế độ, giới hạn ngân sách byte/token) và câu hỏi đến Chat model, không truy xuất tài liệu.

> **Phím tắt nhập liệu:** Nhấn **`Enter`** để gửi câu hỏi; nhấn **`Shift + Enter`** để xuống dòng.

---

### 4. Quick Start, Mở nguồn trích dẫn, New chat & Export Markdown

- **Quick Start (`Summarize`, `Study questions`, `Explain concepts`):** Bấm chip để điền nhanh câu lệnh gợi ý vào ô nhập, sau đó chỉnh sửa nếu muốn và bấm **`Send ->`**.
- **Sources & `Open source`:** Dưới câu trả lời RAG có danh sách trích dẫn kèm tên file và số trang. Bấm **`Open source`** để mở file gốc (file `.pdf` được mở qua Microsoft Edge tại đúng `#page=...` nếu tệp gốc vẫn tồn tại ở đường dẫn ban đầu).
- **`New chat`:** Tạo `ConversationId` mới cho workspace để bắt đầu phiên hỏi đáp sạch sẽ.
- **`Export`:** Xuất toàn bộ tin nhắn đang hiển thị cùng metadata (`Mode`, `Provider`, `Model`, `Temperature`) và danh sách `Sources` ra file `.md`.

---

## IV. Kiến trúc mã nguồn & Dependency Injection

### 1. Cấu trúc 5 project trong `DocuMind.slnx`

| Project | Target | Trách nhiệm chính |
| :--- | :--- | :--- |
| `DocuMind.Domain` | `net10.0` | Entities (`Workspace`, `Document`, `DocumentVersion`, `DocumentChunk`, `EmbeddingProfile`, `ProviderSetting`, `ChatMessage`) và Enums (`AiProviderType`, `ChatMode`, `MessageRole`, `DocumentVersionStatus`) |
| `DocuMind.Application` | `net10.0` | Interfaces (`IChatProviderFactory`, `IChatCompletionService`, `IEmbeddingService`, `IDocumentParser`, `IDocumentStore`, `IChatHistoryStore`, `IRepository<T>`, `ISecretProtector`), DTOs, `RagOrchestratorService`, `VectorMathHelper`, `UserErrors` |
| `DocuMind.Infrastructure` | `net10.0` | EF Core SQLite (`AppDbContext`, `Migrations`, `DatabaseMaintenance`), `EfRepository<T>`, `EfDocumentStore`, `EfChatHistoryStore`, `PdfPigDocumentParser`, HTTP providers (`GeminiAiService`, `OpenAiCompatibleService`, `ChatProviderFactory`), `WindowsDpapiSecretProtector` |
| `DocuMind.WPF` | `net10.0-windows` | Composition Root (`App.xaml.cs`), `MainWindow.xaml`, `SettingsDialog.xaml`, `MarkdownView`, `MainViewModel`, `SettingsViewModel` (dùng `CommunityToolkit.Mvvm` 8.4.2) |
| `DocuMind.Tests` | `net10.0-windows` | Bộ 68 kiểm thử xUnit (`RepositoryTests`, `MigrationRegressionTests`, `ProviderRegressionTests`, `RagRegressionTests`, `WorkspaceRegressionTests`, `WpfSmokeTests`) |

---

### 2. Luồng xử lý chính (RAG & MVVM)

- **Composition Root ([App.xaml.cs](file:///d:/CN5.1/PRN212/26.FA.PRN212/Project-Prn212/DocuMind_WPF/DocuMind.WPF/App.xaml.cs)):** Đăng ký `IDbContextFactory<AppDbContext>`, `HttpClient` (`Singleton`), `ISecretProtector`/`IDocumentStore`/`IChatHistoryStore` (`Singleton`), `IRepository<T>`/`IDocumentParser`/`IChatProviderFactory`/`RagOrchestratorService` (`Scoped`), `ViewModel` và `Window` (`Transient`).
- **Luồng MVVM:** `MainWindow` (XAML) bind dữ liệu tới `[ObservableProperty]` và `[RelayCommand]` của `MainViewModel` → gọi `RagOrchestratorService` → gọi `IChatProviderFactory` / `IDocumentStore` → thao tác HTTP AI API hoặc SQLite qua `IDbContextFactory<AppDbContext>`.

---

## V. Lỗi thường gặp & Giới hạn cần biết

| Triệu chứng / Lỗi | Nguyên nhân | Cách xử lý |
| :--- | :--- | :--- |
| **"Please open Settings..." / `Not configured`** | Chưa lưu provider chat active | Mở **`Manage providers`**, điền cấu hình và bấm **`Save provider`** |
| **RAG báo không đủ bằng chứng dù đã upload file** | Chưa chọn provider embedding, đổi embedding model mà chưa re-index, hoặc file PDF là ảnh scan | Kiểm tra **`Use this provider for document embeddings and RAG`**, test embedding rồi bấm **`+ Upload documents`** nạp lại file nguồn |
| **Không giải mã được key (`KeyNeedsReplacement`)** | DB được copy từ máy/tài khoản Windows khác | Nhập lại API key trên máy hiện tại và bấm **`Save provider`** |
| **Ollama báo lỗi kết nối** | Dịch vụ Ollama chưa chạy hoặc chưa `ollama pull` đúng tên model | Mở Ollama ở khay hệ thống, kiểm tra `ollama list` và URL `http://localhost:11434/v1` |
| **Không mở được file nguồn (`Open source`)** | File gốc trên máy đã bị đổi tên, di chuyển hoặc xóa | Kiểm tra lại vị trí file gốc trên đĩa (lịch sử chat vẫn giữ snapshot nội dung trích dẫn) |

> **Giới hạn hiện tại của phiên bản:** Timeout mặc định là **10 phút/file** khi ingest, **2 phút/lượt chat** và **30 giây/test kết nối**; chưa hỗ trợ OCR cho PDF scan, chưa có parser cho `.docx`, chưa có màn hình duyệt lại các conversation cũ sau khi bấm **`New chat`**, và chưa streaming từng token khi trả lời.
