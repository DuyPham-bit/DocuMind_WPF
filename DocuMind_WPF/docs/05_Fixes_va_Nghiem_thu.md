# Bản sửa DocuMind và tiêu chí nghiệm thu

## Phạm vi và kiểm thử

Áp dụng các bước bảo vệ cấu hình, contract RAG, ingest có version, lifetime DB/WPF và độ bền dữ liệu. Không tự đổi model embedding đã lưu. Mỗi nhóm có regression test trong `DocuMind.Tests`; fake AI/parser/HTTP không dùng API key hoặc quota thật. SQLite file thật được tạo riêng cho mỗi test và xóa sau test; không dùng EF InMemory.

Repo không có DB người dùng mẫu khi bắt đầu. Fixture dựng schema gốc từ migration `LegacyBaseline`, bỏ history để mô phỏng `EnsureCreated`, thêm chunk/chat/provider giả rồi chụp backup. Test xác nhận nâng cấp, re-index và restore giữ đúng nội dung cũ; không sao chép secret thật vào repo.

| Nhóm | Thay đổi và nghiệm thu tự động |
| --- | --- |
| BUG-006/007/008/009 | Draft chứa provider, key, chat/embedding model, URL, temperature. Kiểm tra chat/embedding tạo service tạm, không ghi DB. Save mã hóa trước khi mở transaction, lưu key và active flags cùng transaction. DPAPI lỗi không fallback; key cũ không giải mã được yêu cầu nhập lại, giữ các trường khác. Generation bỏ kết quả tải provider cũ, key bị clear ngay khi đổi. Test pass/fail/cancel không đổi cấu hình; key A/B được kiểm tra trên fake HTTP header. |
| BUG-002/003/004; IMP-005/006 | Tách capability chat/embedding. Groq chat-only, Ollama phải chọn model embedding đã cài. Provider embedding được chọn độc lập và giữ khi đổi provider chat. Retrieval chỉ dùng Ready với profile trùng provider/model/endpoint/dimension/normalization/pipeline. JSON lỗi, vector rỗng/zero/non-finite/sai chiều, score dưới 0.3 không tạo citation; không có fallback top-K. Không có nguồn trả lời cần re-index/không đủ căn cứ, sources rỗng, không gọi chat. |
| BUG-001; IMP-008 | Staging giữ Ready cũ qua parse/HTTP/vector/serialize/cancel lỗi. Parse và HTTP nằm ngoài transaction. Activate ghi chunks/profile và thay Ready trong transaction ngắn; lỗi constraint cũng rollback xóa version cũ. Nạp cùng đường dẫn không tạo bản sao; file trùng tên khác đường dẫn có ID riêng. Cancel, timeout 10 phút mỗi file, 2 phút chat, 30 giây kiểm tra. Embedding retry tối đa 3 lần tổng cộng cho 408/429/5xx, không retry 401/403. Batch hiển thị tên và số file thành công/lỗi/hủy thực tế. |
| BUG-005/010/011/012 | `IDbContextFactory` tạo/dispose context mỗi thao tác; repository mutation tự commit. UI capture workspace/conversation/generation, prompt, mode và ChatSession (provider/model/temperature) trước thao tác. Reload cũ bị hủy/bỏ; kết quả chat chỉ cập nhật đúng selection gốc, lịch sử vẫn lưu đúng workspace gốc. Direct/RAG đều dùng temperature đã lưu, được kiểm tra đến payload HTTP. Error UI/log không in exception thô, API response hoặc URL. |
| IMP-001/002/003/004/007/009 | LocalApplicationData + migration/backup/restore. Chat lưu workspace/conversation cùng mode/provider/model/temperature/snapshot nguồn. Workspace có tạo/đổi tên/system prompt/xóa với xác nhận cascade. Markdown văn bản hỗ trợ heading, bold, italic, list, quote, inline/fenced code, không thực thi HTML. Export metadata từng message. Nguồn lưu ID/path/page, PDF mở Edge với `#page`, tệp khác mở ứng dụng mặc định khi còn tồn tại. System instruction phân biệt tài liệu không tin cậy, context JSON trong delimiter ngẫu nhiên; test prompt injection kiểm tra ranh giới dữ liệu/chỉ thị. |

Không triển khai embedding song song/batch tối ưu khi chưa benchmark; pipeline hiện tại tuần tự, không có concurrency không giới hạn trên context. Prompt-injection test xác minh cấu trúc request/phân quyền prompt, không phải chứng minh mô hình thực luôn chống được mọi tấn công.

## Dùng cấu hình

1. Mở Cài đặt, chọn provider và nhập key trong PasswordBox. Nút giữ để hiện key chỉ hiển thị tạm trong lúc giữ chuột. DPAPI CurrentUser gắn với tài khoản Windows; không phải cam kết bảo mật tuyệt đối.
2. Điền chat model, URL và temperature. Test kết nối chỉ kiểm tra draft; thành công, lỗi, hủy hoặc đóng dialog đều không kích hoạt draft.
3. Để dùng RAG, điền embedding model, chọn “Dùng provider này cho embedding/RAG”, có thể test embedding riêng rồi Lưu. Lưu cũng kích hoạt provider này cho chat. Sau đó có thể chọn/lưu provider chat khác mà vẫn giữ embedding provider trước đó. Bỏ chọn embedding trên chính provider đang phục vụ embedding sẽ tắt vai trò đó.
4. Lưu là thao tác duy nhất cập nhật cấu hình. Nếu DPAPI không giải mã được key cũ (kể cả dữ liệu Base64 fallback từ bản cũ), nhập key lại; không tự dùng secret không xác thực.
5. Đổi embedding model/endpoint/provider cần nạp lại tài liệu. Đổi chat model/provider không thay profile index.

Các tên model mặc định cũ được giữ nguyên để tránh đổi không gian index. Tính sẵn có của model/quyền tài khoản phải xác minh bằng smoke test riêng; không coi fake HTTP test là xác nhận endpoint thật đang phục vụ model đó.

## Dữ liệu và nâng cấp

- DB: `%LOCALAPPDATA%\DocuMind\documind_local.db`. App chỉ chạy một instance cho mỗi tài khoản/session Windows.
- Nếu DB mới chưa tồn tại, app copy DB cạnh binary qua SQLite Backup API, giữ file nguồn nguyên vẹn (gồm dữ liệu đã commit trong WAL).
- Trước migration của DB có dữ liệu, chụp `documind_local.db.backup-<UTC timestamp>` cùng thư mục. Backup chứa dữ liệu người dùng và ciphertext; không đưa vào Git.
- DB `EnsureCreated` gốc chỉ được nhận baseline khi bảng/cột/type phù hợp; schema lạ dừng nâng cấp sau backup. Migrations tiếp theo thêm schema và backfill ở bước riêng, tránh chạy SQL data khi SQLite đang chờ rebuild bảng.
- Chunk cũ được giữ trong Document/DocumentVersion có status Failed, profile null vì không biết không gian embedding; danh sách hiện “cần re-index”. Không suy đoán model cũ từ chiều vector. Re-index cùng file thay thế dữ liệu cũ khi thành công.
- `Document`: workspace, display name, đường dẫn tuyệt đối của file người dùng chọn, hash SHA-256 của snapshot đã parse. `DocumentVersion`: Staging/Ready/Failed, profile, thời điểm. `DocumentChunk`: version, thứ tự đoạn, trang, nội dung/vector. `EmbeddingProfile`: provider/model/endpoint/dimension/cosine/pipeline `words180-overlap30-v1`.
- Startup chuyển Staging còn lại sau lần tắt bất thường sang Failed, kể cả khi không có migration mới. Không đụng Ready.
- Lịch sử tải tối đa 100 message gần nhất rồi cắt theo budget 4.000 token bảo thủ (UTF-8 byte + overhead), bỏ assistant lẻ ở đầu. Nút “Cuộc chat mới” đổi conversation và giữ lịch sử cũ trong DB, không phải xóa vĩnh viễn. Xóa workspace sẽ xóa cascade mọi conversation, tài liệu, version và chunk; hồ sơ embedding có thể được giữ lại.
- Snapshot nguồn trong chat không mất khi tài liệu bị xóa/re-index. Đường dẫn nguồn là đường dẫn cục bộ; PDF mở đúng trang cần Microsoft Edge có sẵn. Tệp bị di chuyển/xóa có thông báo thay vì mở một file cùng tên khác.

## Backup/restore và kiểm tra script

Đóng DocuMind trước backup/restore. Không restore trực tiếp khi app hoặc công cụ khác còn mở DB.

```powershell
dotnet test DocuMind.slnx
dotnet run --project docs/tools/DbCheck -- sqlite
dotnet run --project docs/tools/DbCheck -- backup "C:\path\documind_local.db" "C:\path\sample.db"
dotnet run --project docs/tools/DbCheck -- restore "C:\path\sample.db" "C:\path\documind_local.db"
```

Lệnh restore chụp thêm backup của DB đích hiện tại trước khi ghi lại từ bản đã chọn. Nếu quay về DB trước nâng cấp để chạy binary cũ, giữ app mới đóng; mở app mới sẽ thực hiện migration lại. Dùng bản backup đã chụp thay vì `Down` migration để khôi phục cả schema lẫn dữ liệu index trước swap.

Sinh lại SQL cho DB trống bằng EF CLI 10.0.14:

```powershell
dotnet ef migrations script --project DocuMind.Infrastructure --output docs/Database_SQLite.txt
dotnet ef migrations has-pending-model-changes --project DocuMind.Infrastructure
```

`Database_SQLite.txt` hiện là script migration từ đầu, chạy một lần trên DB trống; không còn là script idempotent để chạy lặp trên DB hiện hữu. Không chạy lên DB cũ bằng tay vì app còn bước nhận baseline và backup.

## Kết quả xác minh

- `dotnet test DocuMind.slnx`: 64 test pass (bao gồm STA smoke test mở/đóng cửa sổ WPF, PasswordBox, Markdown, SQLite transaction/rollback, migration/restore và interleaving).
- WPF build: 0 warning, 0 error. EF kiểm tra không có model change chưa migration.
- UI thực: mở main và Settings, xác minh các trường/key che và cấu hình embedding; công cụ UI gặp lỗi khi đóng Settings (`foreground window did not report a process id`), test STA xác nhận đóng Settings giữ main mở.
- Smoke API thật Gemini/OpenAI/Groq/Ollama: **chưa chạy**, không dùng key/quota thật. Test handler đã xác minh route/model/header/payload, temperature và retry. PDF mở Edge/trang thật chưa smoke trên máy này.
- DOCX/sơ đồ/SQL Server cũ chưa tái phát hành; dùng tài liệu này cùng migrations làm nguồn cho bản hiện hành.

`BUG_FIX_FLOW.local.md` là ghi chú local được exclude khỏi Git; không nằm trong danh sách file phát hành/commit.
