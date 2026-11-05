# DocuMind — tài liệu vận hành

Hướng dẫn hiện hành: [Sửa lỗi, dữ liệu và nghiệm thu](05_Fixes_va_Nghiem_thu.md). Bản sửa được kiểm tra ngày 05/11/2026 trên Windows, .NET 10 và SQLite thật.

- Chạy app: `dotnet run --project DocuMind.WPF`.
- Kiểm thử: `dotnet test DocuMind.slnx`.
- DB hiện hành: `%LOCALAPPDATA%\DocuMind\documind_local.db`, nâng cấp bằng EF migrations và backup tự động trước thay đổi schema.
- [SQL SQLite](Database_SQLite.txt) được sinh từ migrations, chỉ dùng để tạo DB **trống**, chạy một lần; app tự xử lý DB cũ.
- [Danh sách bug ban đầu](04_Bug_va_Cai_tien.md) lưu bằng chứng của phiên bản trước khi sửa. Trạng thái nghiệm thu mới nằm trong tài liệu 05.

Hai tài liệu `01_Setup_va_Code` và `02_Luong_hoat_dong` (MD/DOCX), ảnh trong `assets`, cùng `Database_SQLServer.txt` mô tả phiên bản gốc `355df3d`. Sơ đồ/schema, vị trí DB và một số luồng trong đó đã thay đổi; giữ chúng làm tư liệu lịch sử. App hiện hành sử dụng SQLite, chưa chuyển sang SQL Server. Không chạy script SQL Server cũ để nâng cấp app.

`docs/tools/DbCheck` hỗ trợ kiểm tra script SQLite cùng backup/restore ngoại tuyến; xem tài liệu 05. Công cụ build/verify Word chỉ dành cho bộ tài liệu lịch sử, chưa tái phát hành DOCX cho kiến trúc mới.
