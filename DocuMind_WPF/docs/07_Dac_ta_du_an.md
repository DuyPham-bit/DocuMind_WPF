# Đặc tả đầy đủ dự án DocuMind WPF

Tài liệu đặc tả chức năng và thiết kế kỹ thuật cho ứng dụng trợ lý học tập và nghiên cứu tài liệu trên Windows. Người sử dụng có thể tổ chức tài liệu theo workspace, hỏi đáp trực tiếp hoặc tìm kiếm tài liệu bằng RAG, xem nguồn và xuất hội thoại. Nhóm phát triển dùng tài liệu để vận hành, bảo trì và nghiệm thu đúng hành vi của phiên bản hiện hành.

Ngày đối chiếu 14 tháng 11 năm 2026. Mã nguồn nền 7404dc9 cùng các sửa lỗi trong đợt rà soát cuối. Tên tác giả theo Git là BossTom0912. Tài liệu mô tả sản phẩm đang triển khai; các phần giới hạn và hướng phát triển được ghi riêng để tránh nhầm với tính năng đã hoàn thành.

## 1 Mục tiêu và phạm vi sản phẩm

DocuMind là ứng dụng desktop một người dùng, lưu dữ liệu trong SQLite cục bộ và gọi dịch vụ AI theo cấu hình của từng máy. Mục tiêu là hỗ trợ tra cứu kiến thức, giải thích khái niệm, tạo câu hỏi ôn tập và tóm tắt nội dung. Người dùng cần đọc nguồn để kiểm chứng câu trả lời trước khi sử dụng trong học tập hoặc nghiên cứu.

Hai chế độ xử lý là Direct Chat và RAG. Direct Chat gửi system prompt, lịch sử phù hợp và câu hỏi đến mô hình chat. RAG tạo embedding câu hỏi, tìm các đoạn tương thích trong workspace rồi gửi bằng chứng đã chọn đến mô hình chat. Provider chat và provider embedding có thể khác nhau; cấu hình được áp dụng chung cho toàn ứng dụng, không cấu hình riêng từng workspace.

Phạm vi hiện có gồm quản lý workspace, lưu system prompt, nạp PDF có lớp văn bản và tệp văn bản, index có phiên bản, quản lý provider và key, lịch sử theo conversation, hủy thao tác, Markdown dạng văn bản, mở tệp nguồn và export Markdown. Ứng dụng không có đăng nhập, phân quyền nhiều người, đồng bộ đám mây, backend riêng, OCR, chấm điểm quiz, streaming token hoặc trình đọc PDF tích hợp. DOCX là định dạng của bộ tài liệu dự án, chưa phải định dạng tài liệu đầu vào được parser hỗ trợ.

### 1.1 Vai trò và điều kiện sử dụng

| Vai trò | Công việc | Trách nhiệm |
| --- | --- | --- |
| Người dùng | Cấu hình AI, nạp tài liệu, hỏi đáp, quản lý workspace và xuất chat | Chọn dữ liệu được phép xử lý và kiểm chứng nguồn |
| Thành viên phát triển | Build, kiểm thử, sửa lỗi và mở rộng service | Giữ các ràng buộc dữ liệu và cập nhật tài liệu |
| Người nghiệm thu | Chạy kịch bản chức năng và lỗi | Phân biệt test giả lập với API thật và đánh giá chất lượng |

Ứng dụng cần Windows, quyền ghi LocalApplicationData và thư mục tạm. Build cần .NET 10 SDK; chạy bản framework dependent cần .NET 10 Desktop Runtime. Provider cloud cần mạng và key hợp lệ. Ollama cần dịch vụ local cùng các model đã tải; khả năng hoạt động offline phụ thuộc cấu hình thực tế.

## 2 Kiến trúc và phân chia trách nhiệm

Ứng dụng dùng WPF với MVVM. View binding đến ObservableProperty và RelayCommand do CommunityToolkit.Mvvm sinh. ViewModel giữ trạng thái giao diện, chụp thông tin thao tác và gọi Application service. Domain chứa entity và enum. Infrastructure triển khai SQLite, HTTP, PDF và DPAPI. Composition root trong App.xaml.cs ghép implementation bằng dependency injection.

| Project | Trách nhiệm | Phụ thuộc project |
| --- | --- | --- |
| DocuMind.Domain | Workspace, Document, DocumentVersion, Chunk, Profile, ProviderSetting, ChatMessage và enum | Không phụ thuộc lớp khác |
| DocuMind.Application | Interface, DTO, session, orchestrator, chunking, cosine và thông báo lỗi | Domain |
| DocuMind.Infrastructure | EF Core, migrations, document store, history store, parser, provider HTTP và bảo vệ key | Application và Domain |
| DocuMind.WPF | Window, dialog, ViewModel, MarkdownView và DI | Application, Infrastructure và Domain |
| DocuMind.Tests | Unit, regression và STA smoke test | Bốn project sản phẩm |

Chiều gọi khi chat là MainWindow → MainViewModel → RagOrchestratorService → IChatProviderFactory và IDocumentStore → HTTP hoặc SQLite. Khi lưu cấu hình, SettingsViewModel gọi factory trực tiếp. DatabaseMaintenance xử lý nâng cấp trước khi mở MainWindow.

HttpClient là singleton; IDbContextFactory tạo context mới cho mỗi thao tác. Document/history store không giữ context giữa các lần await HTTP. Repository mutation tự SaveChanges trong từng phương thức, không yêu cầu caller gọi commit bổ sung. Orchestrator hoàn tất parse, embedding và serialize trước transaction kích hoạt index để không giữ khóa SQLite trong khi chờ AI.

Các package được pin trong project gồm EF Core SQLite và Design 10.0.14, ProtectedData 10.0.14, PdfPig 0.1.16, CommunityToolkit.Mvvm 8.4.2 và DependencyInjection 10.0.14. Tests dùng xUnit 2.9.3, FluentAssertions 8.11.0 và Microsoft.NET.Test.Sdk 17.14.1. Domain/Application/Infrastructure target net10.0; WPF và Tests target net10.0-windows.

## 3 Yêu cầu chức năng và tiêu chí chấp nhận

| Mã | Chức năng | Kết quả cần đạt |
| --- | --- | --- |
| FR01 | Khởi động và nâng cấp | Migrate đúng schema, backup trước nâng cấp dữ liệu cũ, dừng khi schema không nhận diện |
| FR02 | Quản lý workspace | Tạo, chọn, đổi tên, lưu prompt và xóa cascade sau xác nhận |
| FR03 | Cấu hình provider | Test draft không lưu; Save lưu key và active flags nguyên tử |
| FR04 | Index tài liệu | Nạp nhiều file tuần tự; không mất Ready cũ khi nạp lại thất bại |
| FR05 | Direct Chat | Gửi đúng prompt, lịch sử theo mode và temperature đã lưu |
| FR06 | RAG | Chỉ truy xuất Ready cùng profile; vector lỗi không tạo nguồn |
| FR07 | Lịch sử | Lưu cặp user và assistant vào đúng workspace và conversation |
| FR08 | Nguồn | Giữ snapshot nguồn; mở đúng path khi tệp còn tồn tại |
| FR09 | Export | Xuất các message đang hiển thị cùng metadata từng message |
| FR10 | Hủy và lỗi | Phục hồi trạng thái giao diện; thông báo vẫn thấy sau khi kết thúc |
| FR11 | Prompt mẫu | Điền câu hỏi mẫu vào ô nhập; người dùng bấm Send để chạy |

Các tiêu chí trên mô tả hành vi có thể kiểm tra. Chúng không cam kết mọi câu trả lời AI đúng, mọi model vẫn được nhà cung cấp phục vụ, hoặc mọi PDF đều trích xuất được văn bản.

## 4 Màn hình và trạng thái tương tác

MainWindow có thanh bên workspace, phần Workspace settings chứa tên và system instruction, nút Upload documents, danh sách tài liệu và Manage providers. Vùng chính có tiêu đề workspace, Search documents, Export, New chat, hội thoại, nguồn và ô nhập. Search documents bật tương ứng RAG; tắt tương ứng Direct Chat. Trạng thái mặc định là RAG bật.

Enter gửi câu hỏi. Shift và Enter xuống dòng; tổ hợp có Control không bị handler gửi tự động. Quick start gồm Summarize, Study questions và Explain concepts. Các chip chỉ đặt UserInput; chúng dùng cùng pipeline hỏi đáp khi gửi và không mở quy trình tổng hợp toàn tài liệu riêng.

CanWork bằng không IsBusy và không IsLoading. Upload, xóa tài liệu, gửi chat, New chat và quản lý workspace dùng điều kiện này. ViewModel kiểm tra thêm workspace và nội dung câu hỏi. Người dùng vẫn có thể đổi selection hoặc mode khi một thao tác đang chờ; kết quả được chụp và kiểm tra để tránh ghi nhầm giao diện.

Trong đợt rà soát cuối, vùng BusyStatus được đưa vào hàng bố cục riêng, có xuống dòng, xuất hiện khi có nội dung dù IsBusy đã false. Progress và Cancel chỉ hiện trong lúc bận. Nhờ đó lỗi HTTP, tệp nguồn mất hoặc tổng kết nạp tài liệu không biến mất ngay khi thao tác kết thúc. Vùng trạng thái rỗng được thu gọn.

SettingsDialog có provider, PasswordBox, giữ chuột để hiện key, chat model, temperature, embedding model, vai trò embedding, Base URL, test chat, test embedding, cancel và save. Khi đang tải hoặc kiểm tra, các trường chỉnh sửa bị khóa. Đóng Settings giữ MainWindow mở và refresh tên provider active.

## 5 Mô hình dữ liệu và quan hệ

Runtime dùng bảy bảng nghiệp vụ cùng bảng lịch sử migrations của EF. Guid và thời gian thường được C# khởi tạo; không giả định có DEFAULT tương ứng khi viết SQL thủ công. Enum được lưu dưới dạng số, embedding và nguồn dưới dạng JSON.

### 5.1 Workspace

Workspace có Id dạng Guid, ConversationId dạng Guid, Name bắt buộc, Description, SystemPrompt và CreatedAt UTC. Name có metadata MaxLength 200, SystemPrompt 2000; SQLite không tự cưỡng chế độ dài như một số hệ quản trị khác. Một workspace có nhiều Document, DocumentChunk và ChatMessage. Xóa workspace cascade tới tài liệu, các phiên bản, chunks và mọi conversation của workspace.

ConversationId trên Workspace là conversation hiện hành. Không có bảng Conversation riêng. New chat tạo Guid mới và lưu trên workspace; lịch sử conversation trước vẫn còn trong ChatMessages nhưng giao diện hiện chưa có màn hình duyệt lại chúng.

### 5.2 Document và DocumentVersion

Document gồm Id, WorkspaceId, DisplayName, SourcePath, SourceHash. SourcePath là đường dẫn tuyệt đối người dùng chọn; index unique theo WorkspaceId và SourcePath dùng NOCASE để phù hợp đường dẫn Windows. Hai file khác đường dẫn nhưng cùng tên được coi là hai tài liệu. SourceHash là SHA256 của snapshot đã parse, không phải cơ chế tự theo dõi thay đổi file.

DocumentVersion gồm Id, DocumentId, Status, EmbeddingProfileId nullable và IndexedAt. Status có Staging bằng 0, Ready bằng 1, Failed bằng 2. Unique filtered index giới hạn một Ready cho mỗi Document. Document có thể có Failed hoặc Staging bên cạnh Ready. Kích hoạt thành công xóa các version cũ không phải Staging của tài liệu trong cùng transaction; đây không phải kho lưu mọi phiên bản để rollback từ UI.

Quan hệ Document → DocumentVersion và DocumentVersion → DocumentChunk đều cascade. EmbeddingProfile → DocumentVersion dùng Restrict. Startup chuyển các Staging còn sót thành Failed. Dữ liệu legacy không biết profile được giữ nhưng không cho retrieval cho đến khi re-index.

### 5.3 DocumentChunk và EmbeddingProfile

DocumentChunk gồm Id, WorkspaceId, DocumentVersionId nullable cho tương thích legacy, ChunkIndex, FileName, PageNumber, Content, EmbeddingJson và CreatedAt. ChunkIndex bắt đầu từ 0 theo thứ tự tạo trong một lần ingest. Index unique theo DocumentVersionId và ChunkIndex. PageNumber là số trang PDF thật được parser trả về; mọi file văn bản không phải PDF dùng trang 1.

EmbeddingProfile gồm Id, Provider, Model, Endpoint, Dimension, Normalization và PipelineVersion. Khóa unique là tổ hợp sáu thuộc tính sau Id. Pipeline hiện hành là words180-overlap30-v1; normalization contract là cosine. Code không biến mọi vector thành unit vector lúc lưu; cosine chuẩn hóa theo độ lớn khi so sánh. Endpoint lưu giá trị hiệu lực sau trim và bỏ dấu slash cuối.

Chỉ version Ready với toàn bộ profile khớp embedding session hiện tại mới được đọc. Cùng chiều vector chưa đủ để coi hai model tương thích. Đổi chat provider không làm mất tính tương thích của index; đổi embedding provider, model, endpoint hoặc pipeline cần re-index.

### 5.4 ProviderSetting và ChatMessage

ProviderSetting có Id int, ProviderType, EncryptedApiKey, BaseUrl, ChatModel, EmbeddingModel, Temperature, IsActive và IsEmbeddingActive. ProviderType unique. Hai filtered unique index cưỡng chế tối đa một provider chat active và một provider embedding active. Có thể chưa có provider nào active. Hai vai trò có thể cùng một dòng hoặc khác dòng.

ChatMessage có Id, WorkspaceId, ConversationId, Mode, Provider, Model, Temperature, Role, Content, SourcesJson và Timestamp UTC. Role phân biệt user, assistant và system; luồng UI hiện tạo user/assistant. Metadata provider/model/temperature được chụp theo chat session của lượt đó, không lấy lại cấu hình hiện tại khi export.

SourcesJson là snapshot danh sách RagSourceDto gồm FileName, PageNumber, Content, Score, DocumentId và SourcePath. Không có foreign key từ snapshot JSON đến Document. Vì vậy câu trả lời cũ còn nguồn sau khi re-index hoặc xóa tài liệu, nhưng mở file chỉ thành công nếu path nguồn vẫn tồn tại.

## 6 Hợp đồng service và dữ liệu trao đổi

| Thành phần | Hợp đồng chính | Trách nhiệm |
| --- | --- | --- |
| IChatProviderFactory | LoadDraftAsync, SaveAsync, CreateTemporary, GetChatSessionAsync, GetEmbeddingSessionAsync | Validate, chọn role provider và tạo service |
| IChatCompletionService | GenerateResponseAsync và TestConnectionAsync | Nhận prompt, history, temperature, token; trả text |
| IEmbeddingService | GenerateEmbeddingAsync và SupportsEmbeddings | Tạo float array; khai báo capability |
| IDocumentParser | ParseFileAsync | Trả danh sách PageNumber và Text |
| IDocumentStore | BeginAsync, ActivateAsync, FailAsync, ListAsync, GetCompatibleChunksAsync, DeleteAsync | Quản lý version và truy xuất index |
| IChatHistoryStore | LoadAsync và AppendAsync | Đọc cửa sổ history và lưu cặp message |
| IRepository | Get, Find, Add, Update, Delete | Mỗi mutation là một unit of work tự lưu |
| ISecretProtector | Protect và Unprotect | DPAPI CurrentUser trên Windows |

ProviderConfigDraft chứa plaintext key trong bộ nhớ để thử cấu hình; ToString được thiết kế không in key. ChatSession chụp service, provider, model và temperature. EmbeddingSession chụp service, provider, model, endpoint cùng hằng normalization/pipeline. ChatResultDto chứa ResponseText và Sources. DocumentListItem mang Id, tên hiển thị, path và cờ NeedsReindex; Label thêm re-index required khi không có Ready.

## 7 Luồng khởi động và nâng cấp database

Điểm vào là App.OnStartup. App tạo mutex Local theo tên người dùng để chặn nhiều instance trong cùng Windows session. Nếu đã có instance, hiện thông báo rồi đóng instance mới. Mutex này không phải cơ chế đồng bộ nhiều tài khoản hoặc nhiều Windows session.

1. DI đăng ký DbContextFactory dùng SQLite tại LocalApplicationData, HttpClient và các service.
2. DatabaseMaintenance tạo thư mục dữ liệu. Nếu DB đích chưa tồn tại nhưng có DB cũ cạnh binary, dùng SQLite Backup API sao chép, giữ nguồn nguyên vẹn.
3. Đọc migrations đang chờ. Khi DB có bảng người dùng và cần nâng cấp, chụp backup tên documind_local.db.backup kèm UTC timestamp.
4. Nếu DB chưa có history nhưng có schema gốc, đối chiếu tên cột, số cột và kiểu của các bảng baseline rồi ghi baseline vào history. Schema không phù hợp dừng sau backup.
5. Chạy EF MigrateAsync. Các migration thêm version/profile/history và bảo toàn index legacy; migration cuối dịch workspace seed chưa đổi tên hoặc prompt.
6. Chuyển Staging còn lại thành Failed kể cả khi không có migration mới.
7. Resolve và Show MainWindow. Loaded gọi InitializeAsync, tải workspace, chọn workspace đầu và đọc provider active.

Reload workspace tải tài liệu và history song song bằng context riêng. Collection cũ được clear ngay. Generation và CancellationToken loại bỏ kết quả của selection trước. Lỗi startup hiện MessageBox cùng thư mục DB và shutdown với mã 1; lỗi tải dữ liệu trên màn hình đi qua UserErrors và BusyStatus.

DB runtime nằm tại %LOCALAPPDATA%\DocuMind\documind_local.db. Tệp trong bin không còn là DB hoạt động mặc định. Không xóa DB thật để sửa lỗi khởi động. Script Database_SQLite.txt chỉ dành cho DB trống; app tự xử lý nhận baseline, backup và migration của DB cũ.

## 8 Luồng cấu hình và bảo vệ provider

### 8.1 Mở dialog và chọn provider

MainWindow tạo SettingsViewModel riêng, mở dialog modal có Owner. InitializeAsync lấy chat provider active nếu có, rồi LoadDraftAsync. Khi đổi provider, key và các trường bị clear trước khi tải; generation ngăn kết quả cũ ghi đè lựa chọn mới. Dòng đã lưu được giải mã bằng DPAPI; provider chưa lưu dùng Defaults trong factory.

Nếu giải mã thất bại, các trường model, URL, temperature và vai trò embedding vẫn được tải, key để trống và KeyNeedsReplacement bật. Người dùng phải nhập key thay thế rồi Save. Key không được fallback thành Base64/plaintext.

### 8.2 Kiểm tra bản nháp

Test connection chụp draft, tạo temporary service và gửi Ping với temperature 0.1. Test embedding tạo service tạm, gửi chuỗi test và xác minh vector không rỗng, có thành phần khác 0 và mọi phần tử hữu hạn. Hai thao tác có timeout 30 giây, Cancel hủy request; không mở transaction ghi cấu hình và không đổi provider active dù pass, fail hoặc cancel.

Kết nối chat thành công không chứng minh embedding hoạt động. Vector test hợp lệ không chứng minh retrieval tốt hoặc index cũ tương thích. Đóng dialog không lưu draft. Giữ chuột trên Hold to reveal chỉ hiện tạm key; nhả, rời chuột, đổi trạng thái hoặc deactivated sẽ ẩn. Dispose xóa key khỏi ViewModel và clear PasswordBox, nhưng không phải cam kết xóa mọi bản sao string khỏi bộ nhớ .NET.

### 8.3 Lưu và kích hoạt

1. Validate provider được hỗ trợ, chat model không rỗng và key không rỗng trừ OllamaLocal.
2. Kiểm tra temperature hữu hạn trong khoảng 0 đến 2. Slider giao diện hiện cho chọn 0 đến 1.
3. Base URL phải là URL tuyệt đối HTTPS hoặc HTTP loopback, không query, fragment hay user info. Không đặt key trong URL.
4. Nếu chọn vai trò embedding, capability và tên embedding model phải hợp lệ.
5. Mã hóa key trước transaction. Lỗi DPAPI giữ nguyên cấu hình đã lưu.
6. Trong transaction, tắt chat active cũ; nếu chọn embedding thì tắt embedding active cũ. Upsert dòng provider đích với key, model, endpoint, temperature và role flags rồi commit.
7. Save thành công đóng dialog; main refresh tên provider chat.

Lưu provider chat B với vai trò embedding tắt vẫn giữ provider embedding A nếu A khác B. Bỏ chọn vai trò embedding trên chính provider A rồi lưu sẽ tắt vai trò đó. Save không tự gọi API thật và có thể lưu cấu hình chưa kết nối được.

### 8.4 Các adapter và giá trị mặc định trong source

| Provider | Chat model mặc định | Embedding mặc định | Adapter |
| --- | --- | --- | --- |
| Gemini | gemini-2.5-flash | text-embedding-004 | GeminiAiService |
| OpenAI | gpt-4o-mini | text-embedding-3-small | OpenAiCompatibleService |
| Groq | llama-3.3-70b-versatile | Không có | OpenAiCompatibleService, chat only |
| OllamaLocal | llama3 | Chưa chọn | OpenAiCompatibleService |
| OpenRouter | openrouter/free | nvidia/nemotron-3-embed-1b:free | OpenAiCompatibleService |

Đây là giá trị code, không phải xác nhận quyền truy cập, giá hoặc vòng đời model hiện tại. Ollama muốn dùng RAG phải nhập một embedding model thực sự đã cài. Ví dụ cặp model trong hướng dẫn local của repo là qwen3:4b-instruct và bge-m3; không tự thay những cấu hình người dùng đã lưu.

Endpoint mặc định là generativelanguage.googleapis.com/v1beta cho Gemini, api.openai.com/v1 cho OpenAI, api.groq.com/openai/v1 cho Groq, localhost:11434/v1 cho OllamaLocal và openrouter.ai/api/v1 cho OpenRouter. Các endpoint cloud dùng HTTPS. URL hiệu lực có thể được người dùng thay bằng proxy đáp ứng đúng contract.

## 9 Luồng nạp tài liệu và kích hoạt index

Điểm vào Upload documents mở picker nhiều file PDF, TXT, MD hoặc All files. Các file được xử lý tuần tự; cần workspace và cấu hình embedding. Mỗi file có timeout 10 phút ở orchestrator. Không có transaction chung cho cả lô: file thành công trước vẫn được lưu khi file sau lỗi.

1. MainViewModel chụp workspaceId, conversationId và generation rồi đặt IsBusy.
2. Orchestrator lấy EmbeddingSession trước khi tạo Staging. Nếu chưa có embedding provider thì dừng trước khi tạo version.
3. BeginAsync chuẩn hóa full path và tìm Document cùng workspace/path. Nếu đang nâng cấp legacy, thử ghép legacy theo tên file. Chưa có document thì tạo mới, sau đó lưu version Staging.
4. Sao chép file vào snapshot có tên ngẫu nhiên trong thư mục tạm, giữ extension. File nguồn được mở FileShare.Read; snapshot dùng cho cả hashing và parsing.
5. Tính SHA256 của snapshot, parse theo trang; finally xóa snapshot kể cả lỗi. File gốc không bị sửa hoặc xóa.
6. Với từng trang, chia theo cửa sổ 180 từ, overlap 30, bước dịch 150 từ. Trang trống không tạo chunk. File văn bản dùng ReadAllTextAsync và trang 1; PDF dùng PdfPig page.Text.
7. Gửi từng chunk đến embedding service tuần tự. Xác minh vector hợp lệ và cùng dimension trong file. Serialize float array thành JSON, giữ chunks trong bộ nhớ với ChunkIndex tăng dần.
8. Nếu không có chunk hợp lệ thì báo lỗi. Khi hoàn tất, ActivateAsync mở transaction ngắn, lấy hoặc tạo profile trùng contract, xóa version cũ không Staging, thêm chunks, chuyển version thành Ready và cập nhật hash.
9. Commit thành công mới thay index cũ. UI tải lại danh sách tài liệu và báo tên/số file hoàn tất hoặc lỗi/hủy nếu vẫn đúng selection ban đầu.

Trạng thái mỗi lần index đi từ Staging đến Ready khi commit thành công hoặc đến Failed khi lỗi/hủy. Ready trước vẫn tồn tại trong thời gian parse và gọi HTTP. Nếu activate lỗi constraint, transaction rollback cả việc xóa index cũ. Cleanup FailAsync dùng token độc lập để vẫn đánh dấu được khi token thao tác đã hủy. Startup xử lý các Staging còn sót sau khi tắt bất thường.

Nạp lại đúng path thay index, không sinh document trùng. File cùng tên ở path khác có DocumentId khác. Tệp đổi chỗ được coi là tài liệu khác, ngoại trừ bước ghép legacy theo tên. Không có tự re-index khi tệp bên ngoài đổi nội dung; người dùng cần chọn nạp lại. Failed/Staging không được retrieval.

Cancel dừng file đang xử lý, không bắt đầu file tiếp theo. Tổng kết hiện đếm file đã thành công và file đang lỗi/hủy; các file chưa bắt đầu sau khi cancel không nằm trong hai số này. PDF scan cần OCR bên ngoài; tệp DOCX/binary qua All files không được hiểu theo cấu trúc Office.

## 10 Luồng Direct Chat và lưu hội thoại

Điều kiện là có workspace, câu hỏi không rỗng và CanWork. Khi tắt Search documents, SendMessageAsync chạy Direct Chat. UI chụp workspace, conversation, generation, question, system prompt và mode trước khi await, xóa ô nhập và đặt trạng thái Thinking.

1. Tạo CancellationTokenSource với timeout 2 phút và lấy ChatSession hiện tại.
2. Load history của workspace/conversation hiện hành; lọc theo mode Direct, chuyển role thành user hoặc assistant.
3. Tạo user message với metadata đã chụp và hiển thị nếu selection còn khớp.
4. ChatExternalAsync gọi service bằng system prompt, question, history và temperature của session. Direct không lấy chunks hoặc gọi embedding.
5. Tạo assistant message và đảm bảo timestamp sau user. AppendAsync lưu cả cặp trong cùng SaveChanges.
6. Nếu selection vẫn khớp thì thêm assistant bubble, xóa trạng thái Thinking; finally tắt IsBusy.

Nếu đổi workspace khi AI đang trả lời, kết quả vẫn được lưu cho workspace/conversation gốc nhưng không thêm vào màn hình mới. Nếu thay provider trong Settings khi một lượt đang chạy, service và metadata của lượt đã chụp không tự đổi theo provider mới.

Lỗi HTTP, DB, timeout hoặc cancel hiện thông báo đã làm sạch. Cặp message chỉ được lưu sau khi có câu trả lời; user bubble đã hiển thị trước đó có thể còn trên UI khi thất bại nhưng không được ghi history. Một số response hợp lệ HTTP nhưng rỗng có thể trả chuỗi rỗng theo adapter; hiện chưa có phân loại blocked/empty response riêng.

## 11 Luồng RAG và quy tắc nguồn

RAG dùng cùng cơ chế chụp session, lưu message và bảo vệ selection như Direct, nhưng history chỉ gồm các message mode Rag. RagOrchestratorService chịu trách nhiệm retrieval và dựng prompt.

1. Lấy EmbeddingSession active và tạo embedding cho câu hỏi. Bước này diễn ra trước khi truy vấn chunks, kể cả workspace chưa có tài liệu.
2. Nếu query vector không hợp lệ, trả thông báo không đủ bằng chứng với Sources rỗng.
3. Lấy toàn bộ chunks Ready trong workspace có profile khớp provider, model, endpoint, dimension, normalization và pipeline.
4. Deserialize EmbeddingJson. Bỏ qua JSON hỏng, vector rỗng, zero, không hữu hạn hoặc sai chiều.
5. Tính cosine bằng dot product chia tích độ lớn. Giữ score lớn hơn hoặc bằng 0.3; xếp giảm dần, tie break bằng ChunkId.
6. Lấy topK mặc định 4, giới hạn tham số trong 1 đến 20. Không có fallback lấy chunk dưới ngưỡng hoặc không tương thích.
7. Không còn nguồn thì trả thông báo upload hoặc re-index; không gọi chat model ở nhánh này. Lượt hội thoại vẫn có thể được lưu với thông báo đó.
8. Với các nguồn đã chọn, tạo DTO chứa tên, trang, nội dung, score, DocumentId và path. Serialize dữ liệu tài liệu thành JSON, đặt trong delimiter DOCUMENT_DATA có Guid ngẫu nhiên.
9. Thêm system instruction coi tài liệu là dữ liệu không tin cậy, không làm theo chỉ thị trong tài liệu, dùng làm bằng chứng và ghi rõ khi không đủ căn cứ. Câu hỏi nằm sau khối dữ liệu.
10. Gọi chat service với temperature đã chụp và history; trả answer cùng danh sách nguồn cho ViewModel lưu và hiển thị.

Score là độ gần vector, không phải xác suất câu trả lời đúng. Nguồn hiển thị là các chunk được retrieval chọn trước khi sinh answer. Không có bước đối chiếu từng mệnh đề hoặc tự xác minh citation mà model viết. Delimiter và instruction giảm lẫn dữ liệu với chỉ thị nhưng không chứng minh mô hình chống mọi prompt injection.

Tóm tắt bằng Quick start vẫn chỉ dùng tối đa topK chunk, nên không bảo đảm bao phủ toàn bộ tài liệu dài. Truy xuất hiện load và chấm điểm trong bộ nhớ, chưa dùng vector database/ANN, chưa batch embedding và chưa song song hóa. Chi phí tăng theo tổng số chunk tương thích và số request embedding.

## 12 Luồng workspace và vòng đời chat

New trong Workspace settings tạo workspace tên New workspace, copy SystemPrompt hiện trên form và lưu qua repository. Sau thành công chỉ chọn workspace mới nếu generation chưa đổi. Save chụp entity mới với Id/ConversationId cũ, tên trim và prompt hiện tại; sau khi lưu thay item và giữ selection khi phù hợp. Tên trắng không được lưu. Prompt chỉnh trên form dùng cho lượt chat tiếp theo; cần Save nếu muốn giữ qua lần mở lại.

Đổi workspace clear collections, đặt tên/prompt từ entity, hủy reload cũ và tải danh sách tài liệu cùng history. Generation bảo vệ kết quả trả chậm. Các nút thao tác chính bị khóa khi IsLoading; selection vẫn có thể thay đổi.

Delete workspace yêu cầu xác nhận rõ việc xóa vĩnh viễn. Khi đồng ý, repository xóa workspace và SQLite cascade tài liệu, version, chunks và mọi chat. ProviderSetting không bị xóa; profile chưa dùng có thể còn trong DB. Xóa workspace không xóa file gốc hay file Markdown đã xuất.

New chat thay ConversationId, lưu workspace rồi reload rỗng. Nếu lưu lỗi, khôi phục ConversationId trên entity. Đây là chuyển conversation, không phải xóa lịch sử DB. Giao diện chưa có lệnh chọn conversation cũ hoặc xóa riêng một conversation.

LoadAsync đọc tối đa 100 message gần nhất theo timestamp và Id, rồi chọn từ mới đến cũ trong budget 4000 bằng số byte UTF8 cộng 16 cho mỗi message. Sau đảo thứ tự, bỏ assistant lẻ ở đầu. Đây là giới hạn bảo thủ, không phải tokenizer riêng của model; UI cũng dùng LoadAsync nên chỉ thấy cửa sổ history này. Nếu message gần nhất vượt budget, cửa sổ tải lại có thể rỗng dù dữ liệu vẫn còn trong DB.

## 13 Luồng xóa tài liệu mở nguồn và export

Xóa tài liệu nhận DocumentListItem có Id, hiển thị tên và đường dẫn trong xác nhận. DeleteAsync chỉ xóa document đúng workspaceId và documentId; cascade loại version và chunks. File gốc giữ nguyên, source snapshot trong ChatMessages giữ nguyên. Hủy xác nhận không gọi xóa. Lỗi DB được báo qua BusyStatus.

Open source kiểm tra SourcePath và File.Exists. Nếu mất tệp, hiện thông báo thay vì tìm một file khác cùng tên. PDF dùng msedge.exe với file URI và fragment page, có Math.Max để trang tối thiểu là 1. Tệp không phải PDF mở bằng ứng dụng mặc định qua Windows shell. Không upload tệp gốc cho bước mở nguồn và không có viewer nội bộ.

Nguồn có thể được mở ngay cả sau khi tài liệu đã xóa khỏi index nếu file còn tồn tại. Nếu file gốc đã sửa sau khi index, app hiện mở nội dung hiện tại của file chứ không có bản PDF snapshot lâu dài; không tự kiểm tra SourceHash ở bước mở. Edge chưa cài hoặc shell mở lỗi sẽ có thông báo.

Export mở SaveFileDialog, mặc định tên DocuMind kèm thời gian local và extension md. BuildMarkdownExport tạo header workspace hiện tại, rồi từng message với timestamp ISO, mode, provider, model, temperature, content và từng source kèm tên/trang/score/DocumentId/nội dung chunk. Ghi UTF8; hủy dialog không ghi, lỗi ghi có catch.

Export chỉ gồm collection đang hiển thị, không truy vấn toàn bộ DB hay mọi conversation. Nó có thể chứa user bubble chưa lưu của lượt lỗi. File export không phải backup, không có import hội thoại ngược lại ứng dụng. Người dùng cần quản lý file export như dữ liệu tài liệu có thể nhạy cảm.

## 14 Giao tiếp HTTP và chính sách lỗi

OpenAiCompatibleService gọi POST chat/completions với model, messages và temperature; messages gồm system, history rồi user mới. Embedding gọi POST embeddings với model và input. Key nằm trong Authorization Bearer nếu có. Parse chat lấy choices đầu và message.content; embeddings lấy data đầu và embedding array.

GeminiAiService gọi models kèm tên model đã escape và action generateContent hoặc embedContent. Key nằm trong header x-goog-api-key. Payload chat dùng system_instruction, contents và generationConfig.temperature; assistant history đổi role thành model. Adapter ghép các text part của candidate đầu. Base URL tùy chỉnh được dùng ở cả hai action.

ProviderHttp retry riêng embedding khi HTTP 408, 429 hoặc 5xx, tối đa 3 lần tổng cộng. Chờ lần lượt 200ms và 400ms. Không retry lỗi 401/403, không retry chat và không retry HttpRequestException kết nối. Hiện chưa đọc Retry-After. Response lỗi bị dispose; thông báo không đưa raw body hoặc URL vào UI.

HttpClient được tạo mặc định nên mỗi HTTP request còn chịu timeout mặc định khoảng 100 giây. Các timeout 10 phút mỗi file và 2 phút chat là giới hạn vòng đời thao tác bên ngoài, không kéo dài timeout của từng request. Cancel được truyền qua parser, copy/hash, EF và HTTP tại các điểm hỗ trợ token.

UserErrors log tên kiểu exception bằng Trace, không log message/inner exception, URL, key hoặc ciphertext. UI phân biệt cancel/timeout, HTTP có status, không kết nối, thao tác không hợp lệ và lỗi file ở mức hướng dẫn. Thông báo tổng quát giúp tránh lộ dữ liệu nhưng có thể cần debug bằng môi trường kiểm soát để xác định nguyên nhân chi tiết.

## 15 Lưu trữ bảo mật và dữ liệu ra ngoài

DPAPI CurrentUser mã hóa API key; dữ liệu này phụ thuộc tài khoản Windows và không được xem là key có thể chia sẻ giữa máy. Các thành viên tự nhập key. Không có fallback Base64 khi mã hóa/giải mã thất bại. PasswordBox che key khi trình chiếu, chỉ hiển thị tạm theo thao tác giữ chuột.

DB không được mã hóa toàn bộ. Workspace, prompt, chunks, vectors, chat, source snapshot và đường dẫn nguồn được lưu trên ổ đĩa. Backup chứa cùng dữ liệu và ciphertext key. Quyền đọc dữ liệu phụ thuộc quyền filesystem/Windows; DPAPI không bảo vệ nội dung tài liệu trong SQLite.

Khi dùng cloud, từng chunk gửi tới embedding provider trong ingest. Khi RAG, câu hỏi gửi tới embedding provider; các chunk được chọn, câu hỏi, system instruction và history gửi tới chat provider. Direct gửi prompt, history và câu hỏi, không retrieval tài liệu. File PDF gốc không được upload nguyên khối bởi pipeline này nhưng nội dung đã trích xuất có thể rời máy.

OllamaLocal với endpoint loopback và model local cho phép xử lý AI trên máy sau khi cài/tải đủ. Nếu người dùng chọn provider cloud hoặc proxy bên ngoài, dữ liệu vẫn ra ngoài theo endpoint đó. Nhãn provider trên main thể hiện chat provider; không suy ra embedding provider cũng giống vậy.

Không commit DB thật, WAL, SHM, backup, key, export chat cá nhân hay snapshot tạm. Dùng fake provider và fixture riêng cho regression test. Mã Markdown chỉ được render dạng văn bản RichTextBox; không thực thi HTML/script, không tải remote image và không mở link từ output tự động.

## 16 Cài đặt vận hành và phục hồi

Chạy các lệnh từ thư mục có DocuMind.slnx. Restore cần truy cập NuGet. Không hạ target framework chỉ để né thiếu SDK. Khi đã mở app, build ra thư mục riêng hoặc đóng app trước khi build binary mặc định để tránh khóa file.

```powershell
dotnet restore DocuMind.slnx
dotnet build DocuMind.slnx --no-restore
dotnet test DocuMind.slnx --no-build
dotnet run --project DocuMind.WPF
```

Cấu hình đầu tiên: Manage providers, chọn provider, điền key nếu cần, chat model và Base URL. Nếu dùng RAG, chọn embedding model và vai trò embedding. Test chat và embedding độc lập, sau đó Save provider. Nạp TXT nhỏ trước PDF lớn và kiểm tra câu có/không có đáp án trong tài liệu.

Ví dụ setup local trong repo dùng endpoint http://localhost:11434/v1 với chat qwen3:4b-instruct và embedding bge-m3. Cần cài Ollama và tải model trước; đây là ví dụ cấu hình, không phải thao tác tự động trong app. Không có nút tải model Ollama trong phiên bản main đang đặc tả.

### 16.1 Backup và restore ngoại tuyến

Đóng DocuMind và các công cụ đang mở DB trước bảo trì. Dùng SQLite Backup API qua DbCheck để xử lý cả dữ liệu đã commit trong WAL, không chỉ copy một file db đang hoạt động. Backup và restore nhận đường dẫn tuyệt đối, nguồn và đích phải khác nhau. Lệnh backup từ chối ghi đè đích đã tồn tại; restore chụp bản an toàn của đích trước khi ghi lại.

```powershell
dotnet run --project docs/tools/DbCheck -- backup "C:\data\documind_local.db" "C:\backup\documind-20261114.db"
dotnet run --project docs/tools/DbCheck -- restore "C:\backup\documind-20261114.db" "C:\data\documind_local.db"
```

Thay đường dẫn bằng vị trí thực tế; DB runtime mặc định vẫn ở LocalApplicationData. Khi mở app mới trên DB đã restore từ trước nâng cấp, migrations có thể chạy lại. Muốn dùng binary cũ cần giữ app mới đóng và khôi phục backup phù hợp; không dựa vào Down migration để phục hồi index đã thay.

### 16.2 Sinh và kiểm tra script database

```powershell
dotnet ef migrations script --project DocuMind.Infrastructure --output docs/Database_SQLite.txt
dotnet ef migrations has-pending-model-changes --project DocuMind.Infrastructure
dotnet run --project docs/tools/DbCheck -- sqlite
```

EF CLI đã dùng trong đợt kiểm tra là 10.0.14. SQLite script hiện bao gồm migration dịch workspace seed. Script chỉ chạy một lần trên DB trống và không thay thế logic backup/adopt baseline của startup. Database_SQLServer.txt là tư liệu schema bản gốc; ứng dụng hiện không dùng SQL Server và migrations SQLite không được chuyển nguyên sang SQL Server.

### 16.3 Xử lý sự cố thường gặp

| Triệu chứng | Kiểm tra | Hướng xử lý |
| --- | --- | --- |
| Không build được hoặc thiếu assets | SDK, restore, app khóa DLL | Restore và dùng artifacts path riêng khi cần |
| Thấy dữ liệu khác bản cũ | Vị trí DB thực tế | Kiểm tra LocalApplicationData và backup/import |
| HTTP 401 hoặc 403 | Key và quyền model | Nhập đúng key/provider, test lại draft |
| HTTP 404 | Endpoint và tên model | Kiểm tra cấu hình theo provider đang dùng |
| HTTP 429 hoặc 5xx | Giới hạn và dịch vụ | Chờ phù hợp, giảm số request rồi thử lại |
| Chat được nhưng RAG lỗi | Embedding capability, model, role | Test embedding riêng và re-index |
| PDF không có text | Scan, PDF bảo vệ hoặc rỗng | Dùng bản có text hoặc OCR bên ngoài |
| Không có nguồn phù hợp | Profile và score | Nạp đúng tài liệu, giữ model/endpoint và hỏi rõ hơn |
| Key không giải mã | Tài khoản Windows hoặc ciphertext | Nhập lại key và Save, không sửa DB bằng plaintext |
| Không mở được nguồn | File đã chuyển/xóa, Edge hoặc shell | Khôi phục đúng path hoặc chọn/nạp lại nguồn |

## 17 Kiểm thử và nghiệm thu cuối

Đợt rà soát ngày 14 tháng 11 năm 2026 chạy 68 test và không có test bị bỏ qua. Test dùng SQLite thực trong fixture riêng, parser/provider/HTTP giả để tái hiện lỗi mà không tiêu key hoặc quota. WPF smoke chạy trên thread STA, mở/đóng main và Settings, kiểm tra PasswordBox và Markdown. Kiểm tra mới xác nhận BusyStatus vẫn thấy khi IsBusy false và nút Test embedding nằm trong viewport ở min width.

| Nhóm | Bằng chứng cần giữ |
| --- | --- |
| Ingest | Lỗi parse, HTTP, cancel, vector và activate không làm mất Ready cũ; path khác cùng tên không ghi đè |
| Retrieval | Profile, endpoint, dimension, pipeline, JSON và threshold lỗi không tạo nguồn hoặc gọi chat |
| Provider | Test không lưu, key đúng header, Save rollback khi lỗi, giữ embedding độc lập |
| History và workspace | Selection trả chậm không ghi nhầm UI, chat lưu workspace gốc, rename/prompt/New chat đúng |
| Migration | Legacy có/không history, backup/restore và abandoned Staging được xử lý |
| Database script | Cột, kiểu, nullability, index, FK, migration history, seed, rollback và cascade khớp |
| WPF | Main/Settings hoạt động, Markdown đọc được, key che và trạng thái lỗi còn thấy |

Kịch bản demo thủ công gồm: cấu hình một provider thực; test chat/embedding rồi Save; nạp TXT UTF8 có dữ kiện; hỏi một câu có đáp án; hỏi một câu ngoài tài liệu và đọc nguồn; tắt Search documents và thử follow-up; đổi workspace; tạo New chat; export; đóng/mở và đối chiếu history hiện hành; hủy một lần nạp lại; thử source đã di chuyển trên tài liệu thử riêng.

Build và fake HTTP pass không xác nhận model cloud đang khả dụng hoặc chất lượng RAG trên PDF thực tế. Đợt kiểm tra này không chạy lại live API, không dùng key thật, không đo GPU/CPU, không nghiệm thu Edge mở đúng trang và không đánh giá adversarial trên mô hình thật. Tài liệu local ngày 07 tháng 11 có ghi smoke Ollama trước đó; đó là bằng chứng lịch sử, không được tính thành live test mới của đợt này.

## 18 Kết quả review và giới hạn còn lại

Rà soát diff thay đổi giao diện gần nhất và các đường gọi hiện hành xác nhận hai lỗi cụ thể: BusyStatus chỉ hiện khi bận nên lỗi/tổng kết bị ẩn sau finally; SQLite script thiếu migration TranslateDefaultWorkspace nên DbCheck dừng ở kiểm tra all migrations applied. Hai lỗi đã được sửa trong đợt này và có kiểm tra tái hiện trước/sau. Chi tiết finding, lệnh xác minh và trạng thái nằm trong 08_Review_cuoi.md.

Giới hạn còn lại cần hiểu khi nghiệm thu: cửa sổ history/export chưa gồm toàn bộ conversation; câu hỏi lỗi có thể chỉ còn bubble chưa lưu; chưa có xử lý riêng AI trả rỗng hoặc bị chặn; danh sách tài liệu xác định re-index required từ việc có Ready, chưa so profile active ngay trên nhãn; source path có thể trỏ đến file đã sửa; tóm tắt topK không bảo đảm toàn tài liệu. Đây là giới hạn hiện hành, không trình bày thành regression mới của thay đổi tài liệu.

Hướng phát triển có thể gồm màn hình chọn conversation và export toàn bộ; phân tách history hiển thị khỏi budget request; xử lý response rỗng; OCR/parser DOCX đúng cấu trúc; context budget cho cả câu hỏi và nguồn; tổng hợp toàn tài liệu; tôn trọng Retry-After; benchmark vector store và batch embedding; kiểm thử model thực bằng tập câu hỏi chuẩn. Mỗi hướng cần tiêu chí nghiệm thu và test riêng trước khi được ghi là tính năng hoàn thành.

## 19 Bản đồ mã nguồn và tài liệu đối chiếu

| Nội dung | File nguồn để bảo trì |
| --- | --- |
| Composition root và startup | DocuMind.WPF/App.xaml.cs |
| Màn hình và command | DocuMind.WPF/MainWindow.xaml và ViewModels/MainViewModel.cs |
| Provider dialog | DocuMind.WPF/Views/SettingsDialog.xaml và ViewModels/SettingsViewModel.cs |
| RAG và ingest | DocuMind.Application/Services/RagOrchestratorService.cs |
| Chunking và cosine | DocuMind.Application/Services/VectorMathHelper.cs |
| Contract và DTO | DocuMind.Application/Interfaces và DTOs |
| Entity và enum | DocuMind.Domain/Entities và Enums |
| Schema và lifecycle DB | DocuMind.Infrastructure/Persistence/AppDbContext.cs, DatabaseMaintenance.cs và Migrations |
| Document và history store | DocuMind.Infrastructure/Persistence/EfDocumentStore.cs và EfChatHistoryStore.cs |
| API và retry | DocuMind.Infrastructure/AiProviders |
| Parser và key | DocuMind.Infrastructure/DocumentParsers và Security |
| Regression | DocuMind.Tests và docs/tools/DbCheck |

Hai Word 01_Setup_va_Code và 02_Luong_hoat_dong mô tả baseline 355df3d, còn hữu ích để hiểu lịch sử nhưng có schema và luồng cũ. Tài liệu 05 ghi đợt sửa trước, tài liệu 06 ghi setup Ollama local. Bản đặc tả 07 dùng làm điểm đọc chính cho phiên bản hiện hành; báo cáo 08 ghi đợt rà soát cuối. Khi thay đổi hành vi, cập nhật Markdown nguồn rồi tái sinh Word và kiểm tra toàn bộ trang, không sửa Word tách rời nguồn.
