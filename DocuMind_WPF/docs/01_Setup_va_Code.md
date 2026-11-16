> Tài liệu lịch sử của bản gốc 355df3d. Luồng/schema và trạng thái sửa lỗi hiện hành: [05_Fixes_va_Nghiem_thu.md](05_Fixes_va_Nghiem_thu.md). Các mô tả lỗi dưới đây là bằng chứng trước khi sửa.

# Hướng dẫn setup và giải thích code DocuMind

Tài liệu dành cho thành viên nhóm phát triển ứng dụng WPF trợ lý học tập và nghiên cứu tài liệu. Sau khi đọc, người mới có thể chạy project, cấu hình AI, theo dõi đường đi của dữ liệu và xác định nơi cần sửa khi mở rộng chức năng.

Phiên bản mã nguồn được mô tả là `355df3d`. Ngày đối chiếu: 05 tháng 11 năm 2026. Các nhãn **Hiện có**, **Hạn chế** và **Đề xuất** phân biệt hành vi thực tế với phần chưa triển khai. Bản DOCX được xuất trực tiếp từ nội dung Markdown này.

## 1 Mục đích và phạm vi ứng dụng

DocuMind hỗ trợ sinh viên hỏi đáp tài liệu học tập, tóm tắt, tạo câu hỏi ôn tập và giải thích khái niệm. Direct Chat gửi câu hỏi ra mô hình AI. RAG tìm những đoạn gần câu hỏi trong tài liệu đã nạp rồi gửi các đoạn đó cùng câu hỏi sang AI. Cả hai chế độ đều dùng provider đang active.

| Yêu cầu | Hiện trạng | Giới hạn cần hiểu |
| --- | --- | --- |
| Chat trực tiếp với AI | Có Direct Chat | Mỗi request chưa kèm lịch sử hội thoại |
| Chat RAG trên tài liệu | Có ingest và retrieval top K | Cần embedding khả dụng và tương thích dữ liệu đã nạp |
| Hoán chuyển nền tảng | Gemini, OpenAI, Groq, OllamaLocal | Một cấu hình active; khả năng embedding khác nhau |
| Tính năng phù hợp chủ đề | Bốn prompt ôn tập, nguồn tài liệu, export Markdown | Prompt tóm tắt vẫn chỉ dùng top 4 chunks khi bật RAG |
| Lưu dữ liệu cục bộ | SQLite lưu chunks, workspace, provider | Chat UI chưa được lưu vào bảng ChatMessages |
| SQL Server | Có script và hướng dẫn trong bộ tài liệu | Code gốc chỉ đăng ký UseSqlite |

**Hiện có:** app desktop Windows chạy một người dùng. Chưa có đăng nhập, đồng bộ tài khoản hoặc dịch vụ backend riêng. Nội dung chunk và prompt được gửi tới provider khi tạo embedding hoặc sinh câu trả lời; SQLite cục bộ không đồng nghĩa toàn bộ xử lý diễn ra offline. Ollama local có thể xử lý tại máy nếu dùng model local phù hợp.

## 2 Chuẩn bị môi trường

Cần Windows, .NET 10 SDK, quyền ghi thư mục build và mạng để restore NuGet. WPF target là `net10.0-windows`; Domain, Application, Infrastructure và Tests target `net10.0`. Chỉ có runtime .NET thì chưa đủ để build. Dùng CLI hoặc Visual Studio có khả năng mở .NET 10 và workload phát triển desktop .NET.

Mở PowerShell tại thư mục chứa `DocuMind.slnx`, không chạy lệnh từ thư mục project con nếu lệnh dùng đường dẫn tương đối bên dưới. Kiểm tra SDK trước:

```powershell
dotnet --info
dotnet --list-sdks
```

Baseline đã kiểm tra bằng SDK `10.0.402`: solution build thành công với 0 warning, 0 error; 14 test pass. Đây là kiểm tra code và test tự động, không phải chứng nhận kết nối API hoặc chất lượng câu trả lời RAG.

### 2.1 Restore build test và chạy

```powershell
dotnet restore DocuMind.slnx
dotnet build DocuMind.slnx --no-restore
dotnet test DocuMind.Tests/DocuMind.Tests.csproj --no-build
dotnet run --project DocuMind.WPF/DocuMind.WPF.csproj
```

Lần đầu phải restore trước khi dùng `--no-restore`. Lỗi `NETSDK1004` thường có nghĩa thiếu `obj/project.assets.json`. Nếu đổi source/package, build lại trước khi test với `--no-build` để tránh chạy DLL cũ.

Trong Visual Studio, mở solution, chọn `DocuMind.WPF` làm startup project, chọn Debug rồi chạy. Nếu IDE chưa nhận `.slnx` hoặc target .NET 10, cập nhật IDE tương thích hoặc dùng CLI; không tự hạ target framework chỉ để mở solution.

### 2.2 Dữ liệu được tạo ở đâu

`DocuMind.WPF/App.xaml.cs`, constructor `App`, dùng `AppDomain.CurrentDomain.BaseDirectory` và tên `documind_local.db`. Với Debug mặc định, file thường ở `DocuMind.WPF/bin/Debug/net10.0-windows/documind_local.db`; Release/publish có thư mục khác. Đường dẫn phụ thuộc thư mục chứa binary đang chạy, không phụ thuộc nơi đặt solution.

`OnStartup` gọi `Database.EnsureCreated()` trước khi hiển thị MainWindow. Database mới có bốn bảng và một workspace mặc định; không seed API key. Nếu chạy binary khác thư mục, người dùng có thể thấy database khác và tưởng dữ liệu đã mất.

**Hạn chế:** thư mục cài đặt như Program Files có thể không cho ghi. `EnsureCreated` không nâng cấp database khi entity thay đổi. Trước khi sao lưu file DB, đóng app và công cụ DB; nếu SQLite dùng WAL thì dữ liệu còn có thể nằm trong file phụ. Không xóa DB để chữa lỗi trước khi có bản sao.

## 3 Cấu hình nhà cung cấp AI

Mở nút Cài đặt API Key và Nền tảng ở thanh bên. Chọn provider, nhập key của chính provider đó, kiểm tra tên model, Base URL và lưu. Bảng dưới mô tả giá trị trong code, không cam kết model còn được tài khoản của bạn cấp quyền sử dụng.

| Provider | Chat model mặc định trong code | Base URL | Embedding trong code |
| --- | --- | --- | --- |
| Gemini | gemini-2.5-flash | Google cố định trong service | text-embedding-004 cố định |
| OpenAI | gpt-4o-mini | https://api.openai.com/v1 | text-embedding-3-small nếu cấu hình rỗng |
| Groq | llama-3.3-70b-versatile | https://api.groq.com/openai/v1 | Cũng fallback text-embedding-3-small |
| OllamaLocal | llama3 | http://localhost:11434/v1 | Cũng fallback text-embedding-3-small |

### 3.1 Cách kiểm tra an toàn trong bản hiện tại

1. Chọn provider rồi tự kiểm tra lại ô API Key. Khi provider mới chưa có key đã lưu, form hiện chưa xóa key cũ tự động.
2. Nhập Chat Model mà tài khoản hoặc máy local có sẵn. Base URL OpenAI-compatible kết thúc ở `/v1`, không nhập thêm `/chat/completions`.
3. Với Ollama local, đảm bảo dịch vụ chạy và đã tải chat model. UI cho phép để key rỗng với OllamaLocal.
4. Bấm kiểm tra kết nối nếu chấp nhận lưu và kích hoạt cấu hình đang nhập. Trong code hiện tại nút này gọi `SwitchProviderAsync` trước khi test.
5. Nếu test thất bại, cấu hình cũ không được tự khôi phục. Chọn lại provider/cấu hình đúng rồi lưu.
6. Tắt RAG để thử câu hỏi ngắn. Sau đó kiểm tra embedding riêng bằng ingest một file nhỏ trước khi nạp tài liệu lớn.

`TestConnectionAsync` chỉ gửi một câu Ping đến chat model với temperature 0.1. Test pass chưa chứng minh `/embeddings` dùng được, và chưa kiểm tra khả năng tìm đúng tài liệu. Các lỗi HTTP bị service gom thành `false` khi test nên thông báo có thể không cho biết nguyên nhân chi tiết.

### 3.2 Những giới hạn theo provider

**Gemini:** Base URL trong form không được GeminiAiService sử dụng. Embedding gọi cố định `text-embedding-004`, bất kể giá trị `ProviderSetting.EmbeddingModel`. Google đã công bố lịch deprecation ngày 14 tháng 01 năm 2026 cho model này [S4]. Vì vậy cần sửa lựa chọn embedding và re-index trước khi coi Gemini RAG là một cấu hình dùng ổn định. Chỉ đổi Chat Model trên form không sửa được endpoint embedding.

**OpenAI:** chat và embedding dùng chung key/Base URL. Cần quyền truy cập cả hai API. Nếu dùng proxy, kiểm tra proxy hỗ trợ đúng response shape cho cả chat và embedding. Đây là mô tả tích hợp trong code; bộ tài liệu không gọi API tính phí để xác nhận key.

**Groq:** service giả định có endpoint embeddings tương thích OpenAI và gửi model fallback của OpenAI. Tài liệu API Groq đã kiểm tra không liệt kê endpoint embeddings [S5]; không coi Direct Chat pass là bằng chứng RAG pass. Hướng sửa là tách chat provider khỏi embedding provider hoặc khóa RAG khi provider thiếu capability.

**OllamaLocal:** OpenAI-compatible API có endpoint embeddings [S6], nhưng model embedding phải được cài và đúng tên. `text-embedding-3-small` không tự có trên Ollama. UI hiện chưa có ô chọn embedding model; cần thay code/cấu hình DB có chủ đích. Không dùng vector giả trong script mẫu để thay thế ingest thật.

**Temperature:** slider được lưu vào ProviderSettings nhưng `SendMessageAsync` không truyền giá trị đã lưu; chat đi qua tham số mặc định 0.7. Đây là bug cần sửa, không phải cách slider hoạt động như mong muốn.

### 3.3 Bảo vệ key

`WindowsDpapiSecretManager` ưu tiên Windows DPAPI CurrentUser. Ciphertext không được xem như secret chia sẻ được giữa máy hoặc tài khoản Windows. Mỗi thành viên tự nhập key; không commit DB chứa key, không gửi screenshot key và không đưa key vào script SQL.

**Hạn chế:** khi Protect thất bại, code fallback Base64; Base64 có thể giải ngược và không phải mã hóa. Form đang dùng TextBox hiển thị rõ key. UI có dòng bảo mật tuyệt đối nhưng implementation không đảm bảo điều đó. Tránh dùng key có quyền rộng cho bản thử nghiệm này; hướng sửa nằm trong backlog BUG-008 và BUG-009.

## 4 Kịch bản sử dụng để học và demo

Chuẩn bị một file `OnTap_MVVM.txt` UTF-8 với nội dung ngắn bên dưới. Đoạn văn này chỉ để thử luồng ingest và câu trả lời có nguồn, không thay cho tài liệu môn học.

```text
MVVM gồm Model, View và ViewModel.
View hiển thị giao diện và binding tới ViewModel.
ViewModel chứa trạng thái trình bày và command.
Model mô tả dữ liệu và quy tắc nghiệp vụ.
ICommand giúp View gọi hành động mà không gọi trực tiếp service.
```

Chọn workspace mặc định. Cấu hình provider có chat và embedding khả dụng, bấm Nạp tài liệu rồi chọn file. Khi thành công, danh sách UploadedFiles có tên file. Hệ thống chỉ lưu text/chunks/vectors và tên file; không copy toàn bộ file gốc vào một thư mục quản lý tài liệu.

Bật RAG, hỏi “Trong tài liệu, ViewModel có vai trò gì?”. Kiểm tra câu trả lời có nhắc trạng thái trình bày và command, cùng tên file và trang 1. Nguồn bên dưới là các chunk được retrieval lấy ra; vẫn cần đối chiếu nội dung vì chưa có bộ kiểm chứng từng trích dẫn.

Thử hỏi “Tài liệu quy định deadline môn học ngày nào?”. Câu trả lời mong muốn là tài liệu không đề cập. Đây là tiêu chí đánh giá thủ công; prompt có yêu cầu nhưng không bảo đảm model luôn tuân thủ.

Chọn prompt Tạo câu hỏi ôn tập hoặc Giải thích khái niệm. Chip chỉ đưa prompt vào ô nhập; người dùng vẫn phải bấm Gửi. Với tài liệu dài, nút Tóm tắt hiện dùng cùng cơ chế top K nên có thể chỉ tóm tắt vài đoạn gần prompt, chưa bao quát toàn tài liệu.

Tắt RAG để chat trực tiếp. Hỏi “Hãy gợi ý cách ôn tập MVVM”. Nội dung file không được retrieval/gắn vào request Direct Chat. Hỏi tiếp “giải thích ý thứ hai vừa rồi” có thể không đúng ngữ cảnh vì request không gửi các lượt chat trước.

Bấm Xuất Markdown và chọn đường dẫn. File có nội dung chat trong phiên, thời gian và danh sách nguồn; phần chế độ trong header lấy toggle tại lúc xuất, chưa ghi mode riêng cho từng tin nhắn. Nút Xóa chat chỉ xóa collection UI; file đã export không bị xóa. Nút xóa tài liệu loại chunks theo workspace và tên file khỏi DB, không xóa file gốc trên ổ đĩa.

## 5 Bản đồ mã nguồn và kiến trúc

| Project | Trách nhiệm | Phụ thuộc project |
| --- | --- | --- |
| DocuMind.Domain | Bốn entity và ba enum | Không |
| DocuMind.Application | Interface, DTO, RAG và vector helper | Domain |
| DocuMind.Infrastructure | EF, SQLite, parser PDF, HTTP, DPAPI | Application và Domain |
| DocuMind.WPF | Composition root, Views, ViewModels, display models | Application, Infrastructure, Domain |
| DocuMind.Tests | xUnit và các test nền tảng | Domain, Application, Infrastructure |

Domain không biết WPF, HTTP hay EF. Application khai báo điều nó cần thông qua interface. Infrastructure triển khai các interface. WPF ghép implementation vào DI tại `App.xaml.cs`, vì vậy WPF biết Infrastructure là bình thường ở composition root.

Các package đáng chú ý: EF Core SQLite 10.0.14, PdfPig 0.1.16, ProtectedData 10.0.14, CommunityToolkit.Mvvm 8.4.2, DependencyInjection 10.0.14. Tests dùng xUnit, FluentAssertions, EF InMemory và một test SQLite thật trong bộ nhớ. Version lấy từ `.csproj`; không tự nâng package trong lúc chỉ bổ sung tài liệu.

### 5.1 Composition root và vòng đời service

`App` đăng ký HttpClient singleton; AppDbContext, repository, parser, factory và orchestrator scoped; các ViewModel và Window transient. `OnStartup` tạo scope ngắn để EnsureCreated rồi dispose scope.

Sau đó MainWindow được resolve từ root provider. Các dependency scoped của cửa sổ do đó có thể bị giữ ở root scope suốt vòng đời app, thay vì mỗi thao tác có một DbContext riêng. Cấu hình này cần xem lại khi chạy các command đồng thời hoặc mở Settings trong lúc ingest. Chưa có bằng chứng runtime trong baseline rằng race đã xảy ra; backlog đánh dấu đây là rủi ro cần tái hiện.

`MainWindow` gán DataContext, nối OpenSettingsAction và auto-scroll. Event Loaded gọi `InitializeAsync`. Cửa sổ Settings được tạo sau khi resolve SettingsViewModel; đóng dialog sẽ gọi refresh tên provider theo kiểu fire-and-forget.

### 5.2 MVVM và source generator

`MainViewModel` kế thừa `ObservableObject`. Field có `[ObservableProperty]` tạo property có notification; method có `[RelayCommand]` tạo command tương ứng. Ví dụ `UserInput`, `IsBusy`, `SendMessageCommand` được UI binding sử dụng. Class phải `partial` để generator bổ sung code ở thời điểm compile.

```csharp
// MainViewModel.cs: trích gọn mẫu đang có
[ObservableProperty]
private string _userInput = string.Empty;

[RelayCommand]
private async Task SendMessageAsync()
{
    // Guard, thêm tin nhắn, gọi orchestrator, xử lý lỗi.
}
```

Không sửa file generated trong `obj`. Sửa field/method gốc rồi build lại. `ObservableCollection` phát notification khi thêm/xóa phần tử; nó không tự lưu database. `ChatMessageDisplayModel` có thuộc tính đọc như IsUser, HasSources và FormattedTime để chọn bubble và nguồn.

`MainWindow.xaml` bind ô nhập tới UserInput với UpdateSourceTrigger=PropertyChanged. Enter gửi; Shift+Enter hoặc Ctrl+Enter không bị handler gửi chặn. Chat hiển thị bằng TextBlock nên Markdown như `**in đậm**` hiện nguyên ký hiệu, chưa có markdown renderer.

## 6 Entity DTO và hợp đồng service

### 6.1 Entity và dữ liệu lưu

| Entity | Khóa và trường chính | Cách dùng |
| --- | --- | --- |
| Workspace | Guid Id, Name, Description, SystemPrompt, CreatedAt | Cô lập chunks và prompt theo không gian |
| DocumentChunk | Guid Id, WorkspaceId, FileName, PageNumber, Content, EmbeddingJson, CreatedAt | Đơn vị retrieval và nguồn |
| ProviderSetting | int Id, ProviderType, EncryptedApiKey, BaseUrl, ChatModel, EmbeddingModel, Temperature, IsActive | Một cấu hình cho mỗi provider |
| ChatMessage | Guid Id, WorkspaceId, Role, Content, SourcesJson, Timestamp | Có schema, chưa nối lưu/đọc từ UI |

Một Workspace có nhiều DocumentChunks và ChatMessages. Xóa workspace trong DB cascade xóa hai nhóm con. ProviderSetting độc lập, không thuộc workspace. Unique index chỉ áp dụng ProviderType; database chưa cưỡng chế chỉ có đúng một IsActive.

Guid và timestamp mặc định được entity C# khởi tạo, không phải DEFAULT trong DB. Các lệnh INSERT ngoài app phải cung cấp giá trị đầy đủ. `CreatedAt`/Timestamp entity dùng UTC; display model gán DateTime.Now. SQL SQLite lưu Guid/thời gian bằng TEXT; SQL Server dùng kiểu có cấu trúc.

### 6.2 Interface và DTO

| Thành phần | Đầu vào | Đầu ra và lưu ý |
| --- | --- | --- |
| IChatCompletionService.GenerateResponseAsync | systemPrompt, userPrompt, temperature, ct | Task string; chưa có stream/history |
| IChatCompletionService.GenerateEmbeddingAsync | text, ct | Task float array |
| IChatCompletionService.TestConnectionAsync | ct | bool; chỉ kiểm tra chat |
| IChatProviderFactory | cấu hình provider hoặc yêu cầu lấy active | Service mới hoặc ProviderSetting |
| IDocumentParser.ParseFileAsync | filePath, ct | Danh sách cặp PageNumber và Text |
| IRepository T | entity hoặc predicate | CRUD tracking; phải SaveChanges |
| IDocumentChunkRepository | workspaceId, fileName | Chunks, tên file, xóa theo file |

`RagSourceDto` gồm FileName, PageNumber, Content và Score. `ChatResultDto` gồm ResponseText và Sources. `ProviderConfigDto` tồn tại nhưng Settings hiện gọi factory trực tiếp, chưa dùng DTO này. `ChatMode` cũng tồn tại nhưng ViewModel dùng bool UseRagMode, chưa lưu mode vào ChatMessage.

`IRepository<T>.GetByIdAsync` nhận Guid nên phù hợp entity Guid; không dùng nó để tìm ProviderSetting có khóa int. Factory hiện truy vấn ProviderSettings qua AppDbContext trực tiếp. `RagOrchestratorService` được inject workspace repository nhưng chưa sử dụng field này để kiểm tra workspace tồn tại.

### 6.3 Quy tắc commit repository

`AddAsync`, `AddRangeAsync`, `UpdateAsync`, `DeleteAsync` và `DeleteRangeAsync` chỉ thay đổi tracking. `SaveChangesAsync` mới commit. Riêng `DeleteByFileNameAsync` tự gọi SaveChanges ngay sau khi đánh dấu xóa. Sự khác biệt này là nguyên nhân ingest không có tính nguyên tử: dữ liệu cũ bị commit xóa trước lúc embedding mới hoàn tất.

Các query chunk dùng tracking và không có CancellationToken trong interface repository. `GetByWorkspaceAsync` sắp theo FileName rồi PageNumber, chưa có ChunkIndex để bảo đảm thứ tự các chunk cùng trang.

## 7 Luồng code chi tiết

### 7.1 Khởi động và workspace

Điểm vào là `App.OnStartup`, tiếp theo `MainWindow.Loaded` và `MainViewModel.InitializeAsync`. EnsureCreated tạo schema/seed nếu database chưa có bảng; ViewModel đọc tất cả workspace, chọn phần tử đầu, đọc provider active và thêm lời chào trong ChatMessages.

Thay SelectedWorkspace gọi `OnSelectedWorkspaceChanged`, nạp SystemPrompt và tải tên file. Callback là async void; InitializeAsync không await toàn bộ callback. Các query tải file lỗi bị catch rỗng nên giao diện có thể không báo nguyên nhân. Hiện không có command tạo/sửa/xóa workspace trong UI.

### 7.2 Nạp tài liệu

Điểm vào UI là `UploadDocumentCommand`; ViewModel mở OpenFileDialog, hỗ trợ chọn nhiều file và gọi ingest tuần tự. Input service gồm workspaceId, đường dẫn, IProgress string và CancellationToken. Output là các bản ghi DocumentChunk; method không trả một Document entity.

`PdfPigDocumentParser.ParseFileAsync` đọc PDF trong Task.Run, lấy page.Text và bỏ trang rỗng. File khác PDF được đọc nguyên bằng File.ReadAllTextAsync rồi coi là trang 1. Filter có PDF/TXT/MD/JSON/CS và All files; không có whitelist chặn mọi định dạng nhị phân. PDF scan cần OCR nhưng parser hiện không thực hiện OCR.

`IngestDocumentAsync` kiểm tra file, parse, lấy provider, xóa chunks cũ cùng tên trong workspace, chia text từng trang, gọi embedding cho mỗi chunk rồi tạo entity. Embedding float array được serialize thành JSON string. Chỉ sau khi toàn bộ vòng lặp thành công mới AddRange và SaveChanges.

**Nhánh lỗi:** file không tồn tại hoặc không trích text sẽ dừng trước xóa; provider không active cũng dừng trước xóa. Nếu API embedding lỗi sau bước xóa, chunks cũ đã mất. Với nhiều file, các file trước có thể đã commit trong khi file sau lỗi; thông báo một lỗi không có nghĩa cả lô rollback. Hai đường dẫn khác nhau nhưng cùng tên file được coi là một tài liệu trong cùng workspace.

### 7.3 Chunking và vector

`VectorMathHelper.ChunkText` chia theo whitespace, không theo token hoặc câu. Tham số mặc định maxWords=180, overlapWords=30, step=150. Ví dụ với 25 từ, maxWords=10 và overlap=3, các chunk là từ 0–9, 7–16, 14–23 và 21–24. Đây là ví dụ cũng được test tự động.

Không gộp qua ranh giới trang PDF. Số trang nguồn vẫn đúng với trang parser đọc được; file text luôn có PageNumber=1. Đếm từ kiểu này có thể cắt giữa câu và chưa tối ưu cho tiếng Việt hoặc mã nguồn.

CosineSimilarity tính tổng tích vô hướng chia cho tích độ dài hai vector. Với vector null/rỗng, khác dimension hoặc độ lớn gần 0, hàm trả 0. Điểm giống nhau thường gần 1; điểm 0 không có nghĩa xác suất câu trả lời đúng bằng 0. UI format Score thành phần trăm nhưng đây không phải confidence được hiệu chuẩn.

### 7.4 Direct Chat

`SendMessageAsync` guard input rỗng, IsBusy hoặc chưa chọn workspace. ViewModel lấy câu hỏi đã Trim, xóa ô nhập và thêm user bubble trước khi gọi API. Khi UseRagMode=false, nó gọi `ChatExternalAsync(SystemPrompt, question)`.

Orchestrator lấy active service, rồi service gửi HTTP. OpenAI-compatible gửi đúng hai messages system và user tới `/chat/completions`; Gemini dùng system_instruction và một contents user. Không có danh sách ChatMessages trong payload. Service trả string; ViewModel thêm assistant bubble. Lỗi được hiện thành bubble, và finally bỏ IsBusy. Chưa có token streaming hoặc nút Cancel.

### 7.5 Chat RAG

Điểm vào service là `ChatWithRagAsync(workspaceId, systemPrompt, userQuestion, topK=4, temperature=0.7, ct)`. Nó tải toàn bộ chunks trong workspace. Nếu rỗng, trả thông báo yêu cầu nạp tài liệu cùng Sources rỗng và không gọi AI.

Nếu có chunks, service embedding câu hỏi bằng provider đang active. Sau đó deserialize EmbeddingJson từng chunk, tính cosine, lọc Score > 0.05, sắp giảm dần và lấy tối đa 4. Nếu không có chunk qua ngưỡng, fallback lại lấy top 4 trong toàn bộ kết quả, kể cả điểm 0 hoặc âm.

Context gồm tên file, trang, điểm và nội dung chunk. Augmented prompt yêu cầu trích dẫn và nói rõ khi tài liệu không có thông tin. AI nhận systemPrompt cùng augmentedPrompt, sinh answer, rồi service trả ChatResultDto. ViewModel thêm answer và Sources lên UI.

**Hạn chế:** Sources là tập retrieval trước khi answer được sinh; code không kiểm tra model có thực sự dùng mỗi nguồn hoặc trích dẫn đúng không. Không có metadata model/dimension trong chunk, không có token budget, lexical search, reranking hay vector index. Mỗi câu hỏi đều đọc vector JSON và tính similarity trong bộ nhớ.

### 7.6 Settings xóa và export

`SettingsViewModel.InitializeAsync` tải cấu hình active. Đổi SelectedProvider đặt lại ChatModel/BaseUrl theo mặc định và gọi tải key fire-and-forget; không phục hồi đầy đủ model/BaseUrl tùy chỉnh của provider không active. SaveAsync và TestConnectionAsync đều đi qua SwitchProviderAsync, nhưng TestConnection còn gọi Ping.

`SwitchProviderAsync` đọc tất cả config, tắt IsActive, tìm hoặc tạo provider đích, mã hóa key nếu không rỗng, lưu model/BaseUrl/temperature và active=true. Một SaveChanges commit những thay đổi này. Factory lấy config active đầu tiên và tạo service theo enum.

`DeleteDocumentAsync` xác nhận bằng MessageBox, xóa tất cả chunks cùng fileName/workspaceId và cập nhật collection. Chưa có try/catch riêng tại command. `ClearChat` không đụng database. `ExportMarkdown` ghi đồng bộ bằng File.WriteAllText, chưa có xử lý lỗi quyền ghi hoặc file đang bị khóa.

## 8 Database và truy vấn ngoài ứng dụng

Đọc `Database_SQLite.txt` để làm việc với file app hiện tại. Khi dùng DB Browser for SQLite hoặc DBeaver, mở đúng file đang được binary sử dụng. Bật foreign_keys trên connection trước khi thực hành DELETE cascade. SQLite không dùng cú pháp GO hoặc NVARCHAR kiểu SQL Server.

`Database_SQLServer.txt` dùng schema dbo và kiểu SQL Server đúng với model. Chạy file trên database riêng đã chọn trong SSMS. Hai script có tạo bảng/index khi chưa tồn tại, seed khi chưa có ID mặc định, query chỉ đọc và ví dụ CRUD tự rollback. Chạy lại trên schema tương thích không tạo workspace seed thứ hai. Đây không phải công cụ sửa schema cũ.

Các query thống kê file dựa trên DocumentChunks. Không có bảng Documents, nên file chưa ingest xong hoặc không có chunk sẽ không hiện trong thống kê. Có thể nhiều chunk của cùng file/trang; không dùng FileName và PageNumber như khóa duy nhất.

## 9 Hướng dẫn chuyển app sang SQL Server

**Đề xuất cần sửa code:** script SQL Server không tự khiến app dùng SQL Server. Giữ một bản sao database hiện tại, thực hiện trên nhánh riêng và chọn một chiến lược khởi tạo schema. Ví dụ dưới dùng schema đã được script tạo sẵn; không trộn EnsureCreated với migrations trên cùng DB [S2].

### 9.1 Tạo database và chạy script

Ví dụ local dành cho máy có SQL Server LocalDB và sqlcmd. Lệnh đầu tạo một database mới; nếu database đã tồn tại thì chỉ chạy script trên database đó sau khi kiểm tra schema.

```powershell
sqllocaldb start MSSQLLocalDB
sqlcmd -S "(localdb)\MSSQLLocalDB" -E -Q "CREATE DATABASE DocuMind"
sqlcmd -S "(localdb)\MSSQLLocalDB" -E -d DocuMind -f 65001 -b -i docs/Database_SQLServer.txt
```

Với SQL Server ngoài máy, thay server/instance bằng thông tin nhóm cấp, tạo database theo quyền được cấp rồi chọn DB trong SSMS. LocalDB là ví dụ cho từng máy, không phải server chia sẻ của nhóm. Không đặt mật khẩu thật trong source hoặc Markdown.

### 9.2 Thêm provider và đổi DI

```powershell
dotnet add DocuMind.Infrastructure/DocuMind.Infrastructure.csproj package Microsoft.EntityFrameworkCore.SqlServer --version 10.0.14
```

Tại `App.xaml.cs`, thay đăng ký `UseSqlite` bằng ví dụ bên dưới. Đây là snippet đề xuất, chưa được áp dụng vào code gốc:

```csharp
var connectionString = Environment.GetEnvironmentVariable("DOCUMIND_SQLSERVER")
    ?? throw new InvalidOperationException("Thiếu DOCUMIND_SQLSERVER.");
services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString));
```

```powershell
$env:DOCUMIND_SQLSERVER = 'Server=(localdb)\MSSQLLocalDB;Database=DocuMind;Integrated Security=true;TrustServerCertificate=true'
dotnet run --project DocuMind.WPF/DocuMind.WPF.csproj
```

`TrustServerCertificate=true` ở đây dành cho môi trường local; server nhóm nên dùng certificate tin cậy và cấu hình theo quản trị viên. Biến môi trường ví dụ không chứa mật khẩu. Khi khởi động bằng Visual Studio, đặt biến cho tiến trình IDE hoặc cấu hình môi trường chạy tương ứng.

### 9.3 Chọn một cơ chế quản lý schema

Với database đã chạy script, thay `db.Database.EnsureCreated()` trong OnStartup bằng kiểm tra kết nối và kiểm tra workspace/schema cần thiết; báo lỗi setup khi thiếu. Không dựa vào EnsureCreated để bù các bảng thiếu vì nó có thể bỏ qua khởi tạo khi database đã có bảng [S2].

Nếu nhóm muốn dùng migrations lâu dài, tạo bộ migration theo provider và một quy trình baseline/migrate riêng. Repo gốc chưa có migrations, Design package hoặc design-time factory; không hướng dẫn chạy `dotnet ef database update` như thể đã sẵn sàng. SQLite và SQL Server cần migration riêng theo provider [S3]. Không drop DB có dữ liệu để chuyển cơ chế một cách tự động.

Sau khi đổi provider DB, kiểm tra một workspace, ingest tài liệu nhỏ, xóa tài liệu, tắt/mở app và mở DB ngoài để xác nhận đang dùng cùng database. DB query test trong bộ tài liệu không thay thế việc kiểm thử ứng dụng đã chuyển provider.

### 9.4 Chuyển dữ liệu và key

Hai script tạo schema sạch và seed, không tự import dữ liệu SQLite. Muốn chuyển, export theo thứ tự Workspaces trước, DocumentChunks/ChatMessages sau; giữ Guid, UTC, JSON và quan hệ. ProviderSettings nên cấu hình lại qua app trên từng máy thay vì sao chép ciphertext DPAPI. Nếu đổi embedding model trong quá trình chuyển, phải tạo lại vector thay vì dùng tiếp vector cũ.

Khi thêm SQL Server provider, chạy `dotnet list package --vulnerable --include-transitive` và xử lý cảnh báo theo dependency tree. Utility kiểm thử tài liệu pin thêm Pkcs 10.0.14 để tránh bản transitive 9.0.11 bị NuGet cảnh báo; đây không phải thay đổi package của app gốc.

## 10 Mở rộng chức năng và cách đặt code

### 10.1 Thêm nhà cung cấp AI

Tạo implementation IChatCompletionService trong Infrastructure; thêm enum, nhánh factory, provider selector và mặc định form. Với provider thực sự OpenAI-compatible có thể tái sử dụng service hiện tại sau khi kiểm tra URL, auth và JSON. Tạo HTTP handler giả để test payload và lỗi mà không cần key thật.

Nếu provider chỉ chat, không giả định nó có embeddings. Hướng kiến trúc nên tách IEmbeddingService và cấu hình embedding theo workspace. Đưa capability vào UI để chặn ingest/RAG không được hỗ trợ, thay vì đợi HTTP lỗi giữa chừng. Đây là thay đổi interface đề xuất, chưa có trong baseline.

### 10.2 Thêm parser và OCR

Thêm implementation/dispatcher IDocumentParser, route theo extension và cập nhật DI/filter/validation. Giữ contract PageNumber và Text để citation tiếp tục hoạt động. Với DOCX cần quy ước nguồn theo section/paragraph hoặc trang render; không tự gán số trang thật nếu parser không biết pagination. Với PDF scan, OCR là bước bổ sung riêng và cần đo lỗi nhận dạng.

### 10.3 Hoàn thiện chủ đề học tập

Ưu tiên quản lý workspace theo môn học, lịch sử chat theo buổi, mở đúng trang nguồn và lưu prompt ôn tập. Quiz hiện chỉ là text AI sinh; muốn có bài kiểm tra chấm điểm cần model dữ liệu câu hỏi/đáp án, kiểm tra đáp án và màn hình làm bài. Tóm tắt toàn tài liệu cần chiến lược bao phủ nhiều chunks, không dùng nguyên retrieval top 4 của hỏi đáp.

## 11 Kiểm thử và xử lý sự cố

### 11.1 Những gì bộ test hiện tại kiểm tra

| Nhóm test | Số test | Chứng minh |
| --- | --- | --- |
| VectorMathHelperTests | 6 | Cosine ở các trường hợp nền tảng và đầu vào không hợp lệ |
| ChunkingTests | 3 | Text rỗng, text ngắn, overlap |
| RepositoryTests | 2 | Query theo workspace và xóa theo tên file bằng InMemory |
| DatabaseInitTests | 1 | SQLite thật trong bộ nhớ tạo schema và seed |
| DpapiTests | 2 | Round trip và chuỗi rỗng trên Windows |

EF InMemory không kiểm tra đầy đủ ràng buộc quan hệ như SQL thật. DPAPI round trip không chứng minh fallback Base64 an toàn. Chưa có test orchestrator, provider HTTP, ViewModel, schema upgrade hoặc end-to-end RAG trong baseline.

### 11.2 Bảng xử lý sự cố

| Triệu chứng | Nguyên nhân cần kiểm tra | Hành động |
| --- | --- | --- |
| NETSDK1004 | Chưa restore | Restore solution rồi build |
| Không nhận net10.0 hoặc slnx | SDK/IDE không tương thích | Kiểm tra dotnet --info, cập nhật môi trường |
| Lỗi mở SQLite hoặc readonly | Sai đường dẫn/quyền thư mục | Kiểm tra thư mục binary và quyền ghi |
| Mở app thấy mất file | Đang chạy binary dùng DB khác | Đối chiếu documind_local.db thực tế |
| PDF không trích được text | Scan/rỗng/mã hóa | Dùng PDF text hoặc bổ sung OCR |
| Chat test pass nhưng ingest lỗi | Embedding khác capability/model | Kiểm tra endpoint và model embedding |
| HTTP 401 hoặc 403 | Key, quyền model, key sai provider | Nhập lại key và xác minh quyền |
| HTTP 404 | Model bị ngừng hoặc URL sai | Đối chiếu provider docs, không thêm đúp path |
| HTTP 429 | Quota/rate limit | Giảm tải, xem quota, thiết kế retry |
| RAG nguồn không liên quan | Đổi model, JSON lỗi hoặc fallback yếu | Kiểm tra metadata, re-index đúng model |
| Slider không đổi phản hồi | Temperature chưa nối vào request | Theo BUG-005 |
| Query SQL Server không thấy dữ liệu app | App vẫn dùng SQLite | Áp dụng và test hướng dẫn chuyển provider |

### 11.3 Checklist trước demo

1. Build/test từ code hiện tại, không dùng DLL cũ.
2. Xác định DB và provider active; che key khi trình chiếu.
3. Dùng một file text ngắn; xác nhận số chunk tăng và tên file xuất hiện.
4. Hỏi một câu có đáp án và một câu ngoài tài liệu; tự đọc nguồn.
5. Thử Direct Chat, export và mở file Markdown; nói rõ các hạn chế chưa sửa.

## 12 Tài liệu tham khảo

Các nguồn dưới được đối chiếu ngày 05 tháng 11 năm 2026. Giá trị mặc định của project lấy từ source baseline; vòng đời model và API có thể thay đổi theo nhà cung cấp.

- [S1 Microsoft EF Core SQL Server provider](https://learn.microsoft.com/en-us/ef/core/providers/sql-server/) — cài package và cấu hình UseSqlServer.
- [S2 Microsoft Create and Drop APIs](https://learn.microsoft.com/en-us/ef/core/managing-schemas/ensure-created) — EnsureCreated, GenerateCreateScript và hạn chế với migrations.
- [S3 Microsoft Migrations with Multiple Providers](https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/providers) — migration theo provider.
- [S4 Google Gemini Embedding announcement](https://developers.googleblog.com/en/gemini-embedding-available-gemini-api/) — công bố model và lịch deprecation text-embedding-004.
- [S5 Groq API Reference](https://console.groq.com/docs/api-reference) — endpoint công bố; việc thiếu embeddings trong danh mục là căn cứ hạn chế tích hợp, chưa phải test HTTP thực tế.
- [S6 Ollama OpenAI compatibility](https://docs.ollama.com/api/openai-compatibility) — subset API và endpoint tương thích.
- [S7 Microsoft ObservableProperty](https://learn.microsoft.com/en-us/dotnet/communitytoolkit/mvvm/generators/observableproperty) — source generator property notification.
- [S8 Microsoft RelayCommand](https://learn.microsoft.com/en-us/dotnet/communitytoolkit/mvvm/generators/relaycommand) — source generator command.
