# DocuMind_WPF — Nâng cấp Công nghệ Học tập Hiện đại (OpenXML + Hybrid FTS5/SIMD RRF + SM-2 + Quiz Review + LiveCharts2)

**Ngày:** 2026-11-17  
**Phạm vi:** Dự án `DocuMind_WPF` (.NET 10 / WPF / EF Core SQLite)

---

## 1. Mục tiêu & Lý do (Intent & Why)

Nâng cấp chiều sâu kỹ thuật cho **khía cạnh học tập** trong `DocuMind_WPF` bằng cách tích hợp đồng bộ các công nghệ .NET 10 và thuật toán hiện đại phục vụ trực tiếp việc học của sinh viên từ slide/tài liệu:
1. Giải quyết lỗi thực tế hiện tại: `PdfPigDocumentParser` chỉ đọc được `.pdf` và text thuần, khiến file bài giảng `.pptx` và `.docx` bị đọc thành dữ liệu nhị phân lỗi.
2. Nâng cấp lõi tìm kiếm RAG từ pure cosine loop sang **Hybrid Search** kết hợp **SQLite FTS5 (BM25)** + **SIMD Vector (`System.Numerics.Tensors.TensorPrimitives`)** hợp nhất bằng **Reciprocal Rank Fusion (RRF)**.
3. Nâng cấp tính năng học tập từ quiz đơn thuần sang hệ thống ghi nhớ dài hạn **SM-2 Spaced Repetition Flashcards**, **Quiz History Review / Retry Wrong Only / Bloom Difficulty**, và biểu đồ phân tích tiến độ học tập bằng **LiveCharts2 (`LiveChartsCore.SkiaSharpView.WPF`)**.

### Goals
- Parse chính xác `.pptx` (theo từng slide + speaker notes) và `.docx` (đoạn văn + bảng biểu) bằng `DocumentFormat.OpenXml`.
- Tăng tốc tính cosine similarity bằng `TensorPrimitives.CosineSimilarity` (`System.Numerics.Tensors`).
- Triển khai **Hybrid Retrieval (SQLite FTS5 BM25 + Cosine Vector + RRF `k = 60`)** với ràng buộc tuyệt đối: chỉ tìm trên các `DocumentChunk` thuộc `DocumentVersion` có `Status == Ready`, khớp `EmbeddingProfile` hiện tại và nằm trong `documentIds` được chọn (nếu có).
- Thêm hệ thống **Flashcard + SM-2 Spaced Repetition** có lưu trữ SQLite (`Flashcards` table qua EF Core Migration), sinh thẻ từ tài liệu qua AI và ôn tập theo hàng đợi đến hạn (`Due`).
- Nâng cấp **Study Quiz**: mở lại bài cũ từ `QUIZ HISTORY` (`QuestionsJson`), nút **Làm lại câu sai (Retry Wrong Only)**, chọn mức độ tư duy (**Standard / Conceptual / AppliedCode**), và đồng bộ ngôn ngữ đề thi theo `AppText.Vietnamese`.
- Tích hợp **LiveCharts2** hiển thị biểu đồ xu hướng điểm Quiz và phân bổ mức thành thạo Flashcard theo theme Warm Beige AI Studio.

### Non-Goals
- Không thêm đồng bộ đám mây / đăng nhập nhiều người dùng.
- Không nhúng mô hình LLM/ONNX hàng trăm MB vào gói `.exe` (giữ build/test nhẹ, chạy được offline CI).
- Không triển khai SSE token streaming cho chat trong đợt nâng cấp này.

### Anti-Goals
- Không để FTS5 trả về chunk của `DocumentVersion` cũ (`Failed`/`Staging`), sai `EmbeddingProfile`, hoặc ngoài danh sách tài liệu đã chọn.
- Không để truy vấn không liên quan (`"unrelated"`) lọt qua Hybrid Search khi cả vector score < `MinimumScore` lẫn FTS5 không có match hợp lệ.
- Không làm gãy hành vi của 126 unit/WPF smoke test hiện có (giữ nguyên tên control `x:Name` như `TopicBox`, `CountBox`, `DocumentPicker`, `DocumentList`, `QuestionScroll`, `QuestionList`, `HistoryList` và constructor tương thích của `RagOrchestratorService` / `QuizWindow`; riêng `QuizTests.GenerateUsesDelimitedDocumentContextAndSavedTemperature` ở dòng 91 cần đồng bộ assertion sang `AppText.T("untrusted data", "không tin cậy")` giống các dòng 106, 169 khi `BuildPrompt` chuyển sang song ngữ hoàn chỉnh).

---

## 2. Ràng buộc Kiến trúc (Constraints)

- Giữ nguyên cấu trúc Clean Architecture 4 lớp:
  - `DocuMind.Domain`: Entities & Enums thuần, không phụ thuộc NuGet ngoài.
  - `DocuMind.Application`: Interfaces, DTOs, Services, thuật toán thuần (`VectorMathHelper`, `RrfFusion`, `Sm2Scheduler`, `QuizService`, `FlashcardService`, `RagOrchestratorService`) — không tham chiếu EF Core, OpenXML hay WPF.
  - `DocuMind.Infrastructure`: `DocumentFormat.OpenXml`, `PdfPig`, EF Core SQLite Migrations, SQLite FTS5 query/triggers, `EfQuizStore`, `EfFlashcardStore`, `EfDocumentStore`.
  - `DocuMind.WPF`: `LiveChartsCore.SkiaSharpView.WPF`, ViewModels, XAML Views theo bảng màu Warm Beige (`FuturisticTheme.xaml`), song ngữ EN/VI (`UiText`).
- Tự động nâng cấp DB hiện có qua `DatabaseMaintenance.InitializeAsync` + EF Core Migration mới (`Backup` trước khi migrate vẫn giữ nguyên).
- Đóng gói thành công bản `Release` `win-x64` self-contained single-file `.exe`.

---

## 3. Các phương án đã cân nhắc (Approaches Considered)

1. **Phương án 1 — Nâng cấp lõi AI & Search (`Microsoft.Extensions.AI` + `Polly` + `SQLite FTS5` + `SIMD`):** Mạnh về kiến trúc AI pipeline và resilience nhưng chưa xử lý lỗi đọc `.pptx`/`.docx` và chưa mở rộng trải nghiệm học tập trực quan (Flashcards SM-2, Analytics chart).
2. **Phương án 2 (Được chọn) — Học tập Đa định dạng & Trực quan hóa (`OpenXML` + `SQLite FTS5 / TensorPrimitives / RRF` + `SM-2 Flashcards` + `Quiz Review` + `LiveCharts2`):** Cân bằng giữa công nghệ lõi hiện đại (OpenXML, FTS5, SIMD Vector, RRF) và tính năng học tập có chiều sâu (đọc slide môn học, lặp lại ngắt quãng SM-2, ôn câu sai, biểu đồ tiến độ).
3. **Phương án 3 — Realtime SSE Streaming + WebView2 Mermaid Mindmaps:** Trực quan khi chat nhưng phụ thuộc WebView2 Runtime, khó kiểm thử tự động và yếu hơn về hệ thống ghi nhớ/kiểm tra kiến thức.

---

## 4. Thiết kế Chi tiết Từng Module

### 4.1. OpenXML Multi-Format Parser (`DocuMind.Infrastructure`)
- Thêm NuGet `DocumentFormat.OpenXml` vào `DocuMind.Infrastructure.csproj`.
- Mở rộng `PdfPigDocumentParser` (giữ tên lớp hoặc triển khai chung trong `PdfPigDocumentParser : IDocumentParser` để không phá caller trực tiếp nào nếu có; `App.xaml.cs` tiếp tục đăng ký `IDocumentParser`):
  - **`.pdf`**: giữ nguyên dùng `UglyToad.PdfPig`, mỗi trang là `(page.Number, text)`.
  - **`.pptx`**: dùng `DocumentFormat.OpenXml.Packaging.PresentationDocument.Open(filePath, false)`. Duyệt `SlideIdList` theo đúng thứ tự trình chiếu; mỗi slide trích text theo từng đoạn (`A.Paragraph` / `A.Text` trong `SlidePart`) và ghép phần `NotesSlidePart` (nếu có, gắn tiền tố `[Notes]`) thành `(slideIndex, text)`. Slide rỗng thì bỏ qua.
  - **`.docx`**: dùng `DocumentFormat.OpenXml.Packaging.WordprocessingDocument.Open(filePath, false)`. Duyệt `Body` gồm các `Paragraph` và `Table` (trích từng ô nối bằng ` | ` theo từng dòng) theo thứ tự tài liệu; gom thành các trang logic khoảng ~400 từ/trang (`(logicalPage, text)`) để trích dẫn trang có ý nghĩa.
  - **`.doc` / `.ppt` (định dạng nhị phân Office cũ):** từ chối rõ ràng bằng `InvalidOperationException` với thông báo thân thiện (chuyển sang `.docx`/`.pptx` hoặc `.pdf`) thay vì rơi vào nhánh text đọc ký tự rác.
  - **Text-based (`.txt`, `.md`, `.csv`, `.cs`, `.json`, `.xml`, `.py`, `.js`, `.ts`, `.sql`, `.log`, ...):** giữ nguyên đọc UTF-8 thành `(1, text)`.
- Cập nhật filter hộp thoại chọn file trong `MainViewModel.UploadDocumentAsync`:
  `Documents (*.pdf;*.pptx;*.docx;*.txt;*.md)|*.pdf;*.pptx;*.docx;*.txt;*.md|All files (*.*)|*.*`

### 4.2. SIMD Vector & SQLite FTS5 Hybrid Retrieval (RRF)

#### A. SIMD Cosine Similarity (`DocuMind.Application`)
- Sử dụng `System.Numerics.Tensors.TensorPrimitives.CosineSimilarity(ReadOnlySpan<float>, ReadOnlySpan<float>)` trong `VectorMathHelper.CosineSimilarity`.
- Giữ nguyên contract bảo vệ biên: nếu `!IsValidVector(v1) || !IsValidVector(v2) || v1.Length != v2.Length` trả `0f`; kết quả từ `TensorPrimitives.CosineSimilarity` kiểm tra `float.IsFinite` và `Math.Clamp(score, -1f, 1f)`.

#### B. SQLite FTS5 Index & Triggers (`DocuMind.Infrastructure`)
- Tạo bảng ảo FTS5 `DocumentChunksFts` trong EF Core Migration mới (`AddLearningTechAndFts5`):
  - Cột `ChunkId UNINDEXED`, `WorkspaceId UNINDEXED`, `DocumentVersionId UNINDEXED`, `FileName`, `Content` với `tokenize = 'unicode61 remove_diacritics 2'` (tìm chính xác cả tiếng Việt có dấu/không dấu và định danh code).
  - Thêm triggers SQLite `AFTER INSERT ON DocumentChunks` (`trg_DocumentChunks_ai`) và `AFTER DELETE ON DocumentChunks` (`trg_DocumentChunks_ad`) để `DocumentChunksFts` luôn tự đồng bộ kể cả khi `EfDocumentStore.ActivateAsync` hoặc `DeleteAsync` dùng `ExecuteDeleteAsync` với cascade delete.
  - Trong cùng Migration, backfill các `DocumentChunks` hiện có vào `DocumentChunksFts`.
- Thêm interface `ILexicalChunkSearch` trong `DocuMind.Application.Interfaces`:
  - `Task<List<(Guid ChunkId, double Bm25Rank)>> SearchAsync(Guid workspaceId, string query, IReadOnlyCollection<Guid> compatibleVersionIds, int topK, CancellationToken ct = default);`
  - `RagOrchestratorService` nhận thêm tham số tùy chọn `ILexicalChunkSearch? lexicalSearch = null` ở cuối constructor để toàn bộ unit test cũ khởi tạo `new RagOrchestratorService(providers, parser, store)` không bị lỗi biên dịch.
  - `EfLexicalChunkSearch` trong `DocuMind.Infrastructure`:
    - Sanitize chuỗi truy vấn thành các token chữ/số (`[\p{L}\p{N}_]+`), ghép thành biểu thức `FTS5` an toàn dạng `"tok1" OR "tok2"` (tránh hoàn toàn lỗi cú pháp FTS5 khi người dùng hỏi về `C#`, `DbContext.SaveChanges()`, `ASP.NET`, ngoặc đơn, dấu hai chấm...). Nếu không có token hợp lệ -> trả `[]`.
    - Truy vấn SQLite parameterized: `WHERE DocumentChunksFts MATCH @matchQuery AND WorkspaceId = @workspaceId` và lọc `DocumentVersionId` nằm trong `compatibleVersionIds`, sắp theo `bm25(DocumentChunksFts)` tăng dần (BM25 trong FTS5 càng âm càng khớp tốt), giới hạn `topK * 3`.

#### C. Reciprocal Rank Fusion (`RrfFusion` trong `DocuMind.Application`)
- Trong `RagOrchestratorService.RetrieveSourcesAsync`:
  1. Lấy toàn bộ `chunks` tương thích (`GetCompatibleChunksAsync` — đã kiểm tra `Status == Ready`, khớp `EmbeddingProfile` & `dimension`), lọc theo `selectedIds` nếu có.
  2. Chỉ giữ các chunk có vector hợp lệ (`IsValidVector` và `vector.Length == vectorQuery.Length`). Ghi nhận tập `compatibleVersionIds` của chính các chunk hợp lệ đó.
  3. **Vector ranking:** chấm `CosineSimilarity`, giữ các chunk đạt `score >= MinimumScore (0.3f)`, xếp giảm dần.
  4. **Lexical ranking:** nếu `lexicalSearch != null` và `compatibleVersionIds.Count > 0`, gọi `lexicalSearch.SearchAsync(...)` để lấy top ứng viên BM25 thuộc đúng tập chunk hợp lệ (có vector hợp lệ & thuộc `selectedIds`).
  5. **Hợp nhất RRF (`k = 60`):**
     - Với mỗi chunk xuất hiện trong vector ranking (hạng `rv`, 1-based) hoặc lexical ranking (hạng `rl`, 1-based):
       `rrf = (inVector ? 1f / (60f + rv) : 0f) + (inLexical ? 1f / (60f + rl) : 0f)`.
     - Nếu `lexicalSearch == null` (hoặc không có match từ FTS5): thứ tự và tập kết quả tương thích 100% với hành vi cosine trước đây (`score >= MinimumScore`).
     - Chuẩn hóa điểm hiển thị `Score` về `[0, 1]` (hoặc giữ độ khớp cosine cao nhất / RRF normalized `rrf / (2f / 61f)` khi có hybrid match) để hiển thị rõ trên citation.
     - **Chính sách chống nhiễu:** Nếu query không khớp bất kỳ vector nào (`>= 0.3f`) lẫn không khớp FTS5 (`[]`), trả `[]` (bảo toàn 100% test `IncompatibleOrInvalidIndexNeverProducesCitationOrCallsChat`).

### 4.3. SM-2 Spaced Repetition Flashcards (`Domain` + `Application` + `Infrastructure`)
- **Entity `Flashcard` (`DocuMind.Domain.Entities`):**
  - `Guid Id`, `Guid WorkspaceId`, `Workspace? Workspace`, `Guid? DocumentId` (nullable, `OnDelete(SetNull)` khi xóa tài liệu hoặc giữ nguồn text), `string Front` (max 500), `string Back` (max 2000), `string Hint` (max 400), `string SourceFileName` (max 300), `int SourcePageNumber`, `string SourceExcerpt` (max 1000).
  - Trạng thái SM-2: `int Repetitions = 0`, `int IntervalDays = 0`, `double EaseFactor = 2.5`, `DateTime NextReviewAt` (UTC), `DateTime? LastReviewedAt` (UTC), `DateTime CreatedAt` (UTC).
  - Index `(WorkspaceId, NextReviewAt)`.
- **Thuật toán thuần `Sm2Scheduler` (`DocuMind.Application.Services`):**
  - Enum `FlashcardGrade { Again = 0, Hard = 3, Good = 4, Easy = 5 }`.
  - Khi ôn tập tại thời điểm `utcNow`:
    - Nếu `(int)grade < 3`: `Repetitions = 0`, `IntervalDays = 1`.
    - Ngược lại: `Repetitions += 1`; `IntervalDays = Repetitions == 1 ? 1 : Repetitions == 2 ? 6 : (int)Math.Round(IntervalDays * EaseFactor, MidpointRounding.AwayFromZero)`.
    - Cập nhật `EaseFactor = Math.Max(1.3, EaseFactor + (0.1 - (5 - q) * (0.08 + (5 - q) * 0.02)))` với `q = (int)grade`.
    - `NextReviewAt = utcNow.AddDays(IntervalDays)`, `LastReviewedAt = utcNow`.
  - Có hàm `PreviewIntervals(...)` trả số ngày kế tiếp cho cả 4 nút (`Again / Hard / Good / Easy`) để hiển thị trực tiếp trên nút bấm UI (vd: `1d`, `6d`).
- **`IFlashcardStore` & `EfFlashcardStore` + `FlashcardService`:**
  - `GenerateAsync(workspaceId, topic, count, ct, documentIds)`: lấy nguồn qua `RagOrchestratorService`, sinh thẻ bằng prompt có delimiter chống prompt-injection và tuân theo `AppText.Vietnamese`, parse JSON phòng thủ tương tự `QuizService`, lưu vào DB.
  - `GetStudyQueueAsync(workspaceId)`: ưu tiên thẻ `NextReviewAt <= DateTime.UtcNow` trước, kèm danh sách toàn bộ deck và thống kê (`Total`, `DueNow`, `Learning`, `Mastered` với `Mastered` = `Repetitions >= 2 && IntervalDays >= 6`).
  - `ReviewAsync(cardId, grade)` và `DeleteAsync(workspaceId, cardId)`.

### 4.4. Nâng cấp Study Quiz (`QuizService`, `EfQuizStore`, `QuizViewModel`)
- Thêm `enum QuizDifficulty { Standard, Conceptual, AppliedCode }` vào `QuizDtos.cs`.
- Sửa `QuizService.BuildPrompt(topic, questionCount, sources, difficulty = QuizDifficulty.Standard)` *(quyết định kỹ thuật: chuyển toàn bộ system/user prompt sang song ngữ EN/VI theo `AppText.Vietnamese` và cập nhật assertion tại `QuizTests.cs:91` sang `AppText.T("untrusted data", "không tin cậy")` đồng bộ với các assertion `AppText.T` sẵn có trong file test)* để:
  - Ra đề **tiếng Anh** khi `!AppText.Vietnamese`, **tiếng Việt** khi `AppText.Vietnamese`.
  - Bổ sung chỉ dẫn theo `difficulty`:
    - `Standard`: cân bằng định nghĩa và ý chính.
    - `Conceptual`: tập trung cơ chế hoạt động, so sánh, nguyên nhân – hệ quả.
    - `AppliedCode`: tình huống thực tế, đọc/suy luận đoạn mã hoặc cấu hình trong tài liệu, tìm lỗi/trade-off.
- Mở rộng `IQuizStore` / `EfQuizStore`:
  - Thêm `Task<QuizAttemptDetail?> GetAttemptAsync(Guid attemptId, CancellationToken ct = default)` giải tuần tự `QuestionsJson` thành `List<QuizAnswer>` kèm `QuizAttemptSummary`.
- Trong `QuizViewModel`:
  - `SelectedDifficulty` (`Standard`, `Conceptual`, `AppliedCode`).
  - `SelectedHistoryItem`: khi người học bấm vào một dòng trong `QUIZ HISTORY`, tải `GetAttemptAsync` và hiển thị toàn bộ đề cũ ở chế độ đã chấm (`IsRevealed = true`, `IsSubmitted = true`, điểm số cũ, đáp án người dùng từng chọn, giải thích và nguồn).
  - `RetryWrongCommand`: lọc các câu mà người học trả lời sai hoặc bỏ trống (`SelectedIndex != Question.AnswerIndex`) từ bài hiện tại (hoặc bài lịch sử đang xem), tạo bộ câu hỏi mới chỉ gồm các câu đó để làm lại và nộp lưu lượt mới. (Lưu ý: `RetryCommand` giữ nguyên hành vi làm lại toàn bộ đề hiện tại để không phá test `ViewModelGeneratesSubmitsGradesAndRetries`).

### 4.5. Giao diện Study Studio & LiveCharts2 Analytics (`DocuMind.WPF`)
- Thêm NuGet `LiveChartsCore.SkiaSharpView.WPF` vào `DocuMind.WPF.csproj`.
- Giữ nguyên cửa sổ `QuizWindow` (cùng constructor `QuizWindow(QuizViewModel, ...)` hoặc truyền `FlashcardViewModel` qua optional/DI property hoặc một `StudyStudioViewModel` tương thích — ưu tiên giữ `QuizWindow(QuizViewModel, Guid, string)` hoạt động độc lập cho unit test bằng cách cho `FlashcardService` / analytics lấy từ constructor overload hoặc optional dependency, đảm bảo mọi test `new QuizWindow(vm, ...)` cũ chạy không cần sửa!).
- Thiết kế `QuizWindow` theo 3 tab đồng bộ `FuturisticTheme.xaml` (Warm Beige `#FAF6EE` / `#FFFDF9` / `#B45309`):
  1. **Quiz** (tab mặc định khi mở — bảo đảm toàn bộ test layout/binding hiện có thấy ngay các control `TopicBox`, `CountBox`, `DocumentPicker`, `QuestionScroll`, `HistoryList`): thêm ComboBox chọn `Difficulty` và nút **Retry wrong** khi có câu sai; bấm item trong `HistoryList` để xem lại bài cũ.
  2. **Flashcards (SM-2)**: thanh tạo bộ thẻ từ tài liệu đã chọn (`Topic`, `Count`, `Generate`), hiển thị số thẻ đến hạn (`Due`), thẻ lật 2 mặt (`Front` + `Hint` -> `Show Answer` -> `Back` + nguồn trích dẫn), 4 nút chấm SM-2 (`Again`, `Hard`, `Good`, `Easy` kèm preview số ngày ôn lại), và danh sách/xóa thẻ trong deck.
  3. **Analytics (LiveCharts2)**:
     - 4 thẻ KPI: Tổng lượt Quiz, Điểm trung bình %, Số Flashcard đã thuộc (`Mastered / Total`), Số thẻ cần ôn hôm nay (`Due`).
     - Biểu đồ đường/cột **LiveCharts2 `CartesianChart`**: xu hướng điểm % các lượt làm Quiz gần nhất (màu cam đất `#B45309`).
     - Biểu đồ tròn/donut **LiveCharts2 `PieChart`**: tỉ lệ phân bổ Flashcard (`Mastered` `#047857`, `Learning` `#B45309`, `Due` `#9A3412`).
- Trên `MainWindow.xaml` (chat citations): hiển thị nhãn độ khớp `Score` (ví dụ `84%`) cạnh `Page {0}` trên từng thẻ nguồn nếu `Score > 0`.

---

## 5. Phạm vi Kiểm thử (Verification Strategy)

- **Unit & Integration Tests mới (`DocuMind.Tests`):**
  - `OpenXmlParserTests`: tạo file `.pptx` (có slide + speaker notes) và `.docx` (có đoạn văn + bảng) thật trong thư mục tạm bằng `DocumentFormat.OpenXml`, xác minh tách đúng từng slide/trang và từ chối `.doc`/`.ppt` nhị phân.
  - `HybridSearchTests`: kiểm tra `VectorMathHelper.CosineSimilarity` (`TensorPrimitives`), sanitization truy vấn FTS5 (ký tự đặc biệt `C#`, `DbContext.SaveChanges()`, tiếng Việt có dấu), RRF fusion ưu tiên chunk khớp cả vector + từ khóa, cascade trigger tự xóa khỏi FTS5 khi xóa document/version, và không bao giờ trả chunk từ profile không tương thích.
  - `Sm2SchedulerTests`: kiểm chứng thuật toán SM-2 qua chuỗi review `Again -> Good -> Good -> Easy`, cập nhật `EaseFactor` (>= 1.3), `IntervalDays`, `NextReviewAt` và `FlashcardService` JSON parsing/persistence.
  - `QuizEnhancementTests`: kiểm chứng `GetAttemptAsync` mở lại bài cũ, `RetryWrongCommand` chỉ giữ câu sai/bỏ trống, `BuildPrompt` theo ngôn ngữ EN/VI và `QuizDifficulty`.
  - `WpfSmokeTests`: xác minh `QuizWindow` render cả 3 tab (bao gồm LiveCharts2 controls) ở kích thước tối thiểu không ném ngoại lệ.
- **Regression:** Toàn bộ 126 test cũ phải tiếp tục pass 100%, và chạy `dotnet publish ... -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true` thành công.
