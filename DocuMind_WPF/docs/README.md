# DocuMind — tài liệu vận hành

**User guide chính (setup, sử dụng, giải thích code): [USER_GUIDE.md](../USER_GUIDE.md).**

Tài liệu hiện hành ngày 14/11/2026: [Đặc tả đầy đủ dự án Word](07_Dac_ta_du_an.docx), [nguồn Markdown](07_Dac_ta_du_an.md) và [review cuối cùng](08_Review_cuoi.md). Đặc tả bao gồm kiến trúc, dữ liệu, toàn bộ luồng, vận hành và giới hạn. Đợt review sửa lỗi thông báo bị ẩn sau thao tác và đồng bộ SQLite script với migration mới nhất; 68 test pass.

Lịch sử đợt sửa trước: [Sửa lỗi, dữ liệu và nghiệm thu](05_Fixes_va_Nghiem_thu.md), kiểm tra ngày 05/11/2026 trên Windows, .NET 10 và SQLite thật.

- Hướng đi của nhóm: **mỗi thành viên dùng Ollama local**, xem [cài đặt và cấu hình miễn phí](06_Ollama_Local.md).
- Tính năng học tập: [Quiz ôn tập từ tài liệu](09_Quiz_On_Tap.md) — AI ra đề trắc nghiệm, app chấm điểm và lưu lịch sử.
- Chạy app: `dotnet run --project DocuMind.WPF`.
- Kiểm thử: `dotnet test DocuMind.slnx`.
- DB hiện hành: `%LOCALAPPDATA%\DocuMind\documind_local.db`, nâng cấp bằng EF migrations và backup tự động trước thay đổi schema.
- OpenRouter cho nhóm: mở **Cài đặt → Dùng OpenRouter**, dán API key, kiểm tra kết nối và embedding rồi **Lưu cấu hình**. App tự điền chat `openrouter/free`, embedding `nvidia/nemotron-3-embed-1b:free`, URL `https://openrouter.ai/api/v1` và bật embedding/RAG. Mỗi thành viên nhập key trên máy mình; không cần cài Ollama. Các model miễn phí có quota dùng chung theo tài khoản; không đưa key vào source/Git. Danh mục/giá model kiểm tra ngày 07/11/2026: [chat](https://openrouter.ai/openrouter/free), [embedding](https://openrouter.ai/nvidia/nemotron-3-embed-1b:free).
- [SQL SQLite](Database_SQLite.txt) được sinh từ migrations, chỉ dùng để tạo DB **trống**, chạy một lần; app tự xử lý DB cũ.
- [Danh sách bug ban đầu](04_Bug_va_Cai_tien.md) lưu bằng chứng của phiên bản trước khi sửa. Trạng thái nghiệm thu mới nằm trong tài liệu 05.

Hai tài liệu `01_Setup_va_Code` và `02_Luong_hoat_dong` (MD/DOCX), ảnh trong `assets`, cùng `Database_SQLServer.txt` mô tả phiên bản gốc `355df3d`. Sơ đồ/schema, vị trí DB và một số luồng trong đó đã thay đổi; giữ chúng làm tư liệu lịch sử. App hiện hành sử dụng SQLite, chưa chuyển sang SQL Server. Không chạy script SQL Server cũ để nâng cấp app.

`docs/tools/DbCheck` hỗ trợ kiểm tra script SQLite cùng backup/restore ngoại tuyến; xem tài liệu 07. `docs/tools/build_spec.py` sinh Word hiện hành từ Markdown 07 bằng python-docx; cần render và xem lại toàn bộ trang sau mỗi thay đổi. `build_docs.py` và `verify_docs.py` chỉ dành cho bộ tài liệu lịch sử 01 và 02.
