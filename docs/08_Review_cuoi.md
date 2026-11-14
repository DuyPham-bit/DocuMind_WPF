# Review cuối dự án DocuMind WPF

Ngày 14/11/2026. Baseline `7404dc9`, đối chiếu diff `HEAD^..7404dc9` và các đường gọi hiện hành từ WPF đến Application, SQLite, parser và provider HTTP. Đã đọc hai DOCX gốc, các tài liệu Markdown vận hành, entity, migrations, service, ViewModel, XAML và regression tests liên quan. Review theo quy tắc defect-first của skill review-agent; yêu cầu tạo tài liệu, sửa các lỗi xác nhận, commit và push thực hiện theo phạm vi người dùng giao trực tiếp. Không gọi AI cloud hoặc đọc DB/key thật.

## Các finding đã xác nhận và sửa

### [P1] Giữ thông báo sau khi thao tác kết thúc — DocuMind.WPF/MainWindow.xaml:133 tại baseline 7404dc9

Sau lỗi HTTP, cancel, lỗi mở nguồn hoặc nạp xong tài liệu, ViewModel đặt `BusyStatus` rồi tắt `IsBusy` trong finally. UI mới chỉ hiển thị vùng duy nhất binding `BusyStatus` khi `IsBusy=true`, khiến thông báo biến mất ngay lúc cần đọc; lỗi source khi đang idle cũng không hiện. Đã tái hiện bằng STA test thất bại với thông báo HTTP 401 khi idle. Sửa bằng vùng trạng thái riêng bên dưới vùng chat, tự xuống dòng và chỉ collapse khi text rỗng; progress/Cancel vẫn theo `IsBusy`. Test sau sửa xác nhận trạng thái hiện và không đè lên vùng hội thoại.

### [P2] Đồng bộ script SQLite với migration dịch workspace — DocuMind.Infrastructure/Persistence/Migrations/20261114120000_TranslateDefaultWorkspace.cs:10

Migration mới đã được app chạy nhưng `docs/Database_SQLite.txt` chỉ chứa ba migration cũ. Chạy đúng lệnh `dotnet run --project docs/tools/DbCheck -- sqlite` trên baseline thất bại ở `Verification failed: all migrations applied`; database dựng từ script không có seed/history mới. Đã sinh lại script bằng EF CLI 10.0.14, cập nhật DbCheck kiểm tra seed `My Workspace` và chạy lại thành công. Schema, index, FK, migration history, dữ liệu Unicode, rollback và cascade được kiểm tra trên DB riêng, không mở DB người dùng.

## Đánh giá sau sửa

No findings.

Không còn finding xác nhận chưa xử lý trong diff cuối của đợt này. Không suy rộng thành chứng nhận toàn dự án không thể có lỗi. Hai lỗi trên là regression của đợt UI/migration trước; các giới hạn sản phẩm được ghi ở phần dưới thay vì gắn nhãn regression mới.

## Kết quả xác minh

| Kiểm tra | Kết quả |
| --- | --- |
| `dotnet test DocuMind.slnx --no-restore` trước sửa | 68 pass, 0 fail, 0 skip |
| STA test thêm assertion idle status trước sửa | Fail đúng do không có TextBlock status đang visible |
| STA smoke sau sửa | Pass; status còn thấy khi idle, không đè vùng chat, Test embedding dùng được trong viewport ở min width |
| `dotnet test DocuMind.slnx --artifacts-path docs/.qa/final-build --no-restore` | 68 pass, 0 fail, 0 skip |
| EF `has-pending-model-changes` | Không có model change thiếu migration |
| DbCheck SQLite trước sửa | Fail do thiếu migration mới trong script |
| DbCheck SQLite sau sửa | PASS schema, indexes, FK, seed, Unicode, rollback, cascade và migrations |
| Word specification | 19 mục, render qua Word PDF rồi Poppler và xem ảnh từng trang |

App đang mở khóa output mặc định nên build sau thay đổi dùng artifacts path riêng trong vùng QA. Không dừng phiên làm việc hoặc sửa database đang chạy. Renderer `render_docx.py` được thử nhưng không có `soffice.exe`; dùng Microsoft Word cục bộ để xuất PDF kiểm tra và bundled Poppler để render PNG. PDF/PNG/build artifacts chỉ dùng nội bộ, không phải deliverable và không commit.

## Các giới hạn cần đưa vào nghiệm thu

- Chưa chạy lại live API của Gemini, OpenAI, Groq, OpenRouter hoặc Ollama trong đợt này; fake HTTP pass không chứng minh model/quota thực đang khả dụng.
- Chưa smoke mở PDF thật đúng trang bằng Edge, benchmark hiệu năng hoặc đánh giá model thật trên câu hỏi chuẩn/prompt injection.
- History hiển thị dùng chung budget 4000 byte UTF8 cộng overhead với history gửi model; export chỉ có collection đang hiển thị. Conversation cũ còn DB nhưng chưa có màn hình chọn lại.
- Câu hỏi thất bại có thể còn bubble chưa lưu; response HTTP thành công nhưng text rỗng chưa có xử lý trạng thái riêng.
- Nhãn `re-index required` dựa vào việc có Ready, chưa phản ánh ngay profile active đã đổi. Retrieval vẫn kiểm tra toàn bộ profile trước khi dùng nguồn.
- PDF scan cần OCR bên ngoài; DOCX chưa có parser; nguồn có thể mở file hiện tại đã thay đổi so với snapshot index. Tóm tắt dùng topK nên chưa bảo đảm bao phủ toàn bộ file.
- Timeout ngoài là 10 phút/file, 2 phút/chat, 30 giây/test; HttpClient mặc định còn giới hạn khoảng 100 giây/request. Embedding retry 3 lần tổng cộng cho 408/429/5xx, chưa tôn trọng Retry-After.

Các giới hạn trên được mô tả trong đặc tả 07 để nhóm vận hành và bảo vệ demo đúng phạm vi. Không tự mở rộng chức năng hoặc thay model/provider/key đã lưu trong đợt tài liệu này.

## Các file giao cho nhóm

`07_Dac_ta_du_an.docx` là tài liệu Word hiện hành. `07_Dac_ta_du_an.md` là nguồn nội dung và `tools/build_spec.py` là builder tái tạo. Hai DOCX 01/02 được giữ nguyên làm tài liệu lịch sử của baseline `355df3d`. README dẫn người đọc đến tài liệu hiện hành; script SQL và công cụ DbCheck được đồng bộ với migration mới.
