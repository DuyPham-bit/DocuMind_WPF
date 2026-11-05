# Bộ tài liệu DocuMind

Bộ tài liệu tiếng Việt cho ứng dụng WPF trợ lý học tập và nghiên cứu tài liệu, đối chiếu mã nguồn `355df3d` ngày 05 tháng 11 năm 2026. Nội dung mô tả code hiện tại; phần đề xuất SQL Server và backlog chưa được áp dụng vào app.

## Thứ tự đọc

1. [Hướng dẫn setup và code bản Word](01_Setup_va_Code.docx) hoặc [bản Markdown](01_Setup_va_Code.md): setup, cấu hình provider, demo, kiến trúc, giải thích service/entity và chuyển SQL Server.
2. [Luồng hoạt động bản Word](02_Luong_hoat_dong.docx): 9 sơ đồ kiến trúc, ERD và tuần tự; có [nguồn Markdown](02_Luong_hoat_dong.md) để cập nhật.
3. Chọn [script SQLite](Database_SQLite.txt) cho DB của app hiện tại hoặc [script SQL Server](Database_SQLServer.txt) cho database ngoài. Hai dialect ở hai file riêng, không trộn cú pháp.
4. [Bug và cải tiến ban đầu](04_Bug_va_Cai_tien.md): 12 bug và 9 cải tiến, gồm bằng chứng, cách tái hiện, hướng sửa và nghiệm thu.

## Dùng database scripts

SQLite: mở đúng `documind_local.db` cạnh binary bằng công cụ SQLite. Có thể chạy script trên DB trống để học query. Script bật foreign_keys, tạo bảng/index nếu chưa có và seed workspace mặc định nếu thiếu.

SQL Server: tạo một database riêng, chọn database trong SSMS rồi chạy TXT hoặc dùng sqlcmd với UTF-8. File từ chối tạo schema trong DB hệ thống. App gốc vẫn dùng SQLite; mục 9 trong hướng dẫn code giải thích thay package, DI và cơ chế khởi tạo schema để kết nối SQL Server.

Mỗi script có query đọc dữ liệu và ví dụ CRUD trong transaction kết thúc bằng ROLLBACK. Vector trong ví dụ là dữ liệu giả cho schema, không dùng RAG. Script không sửa schema cũ và không chứa API key; cấu hình key qua app trên từng máy.

## Kết quả kiểm tra

| Hạng mục | Kết quả và giới hạn |
| --- | --- |
| Solution gốc | Build thành công, 0 warning, 0 error; 14 test pass |
| SQLite script | Pass schema/type/nullability/index/FK theo EF, seed, Unicode, chạy lại, rollback và cascade |
| SQL Server script | Pass các kiểm tra tương tự trên LocalDB 17, DB tạm riêng được xóa sau kiểm tra |
| Markdown và Word | Build từ một nguồn, kiểm tra nội dung source có trong DOCX |
| Bố cục Word | Render và kiểm tra từng trang; sơ đồ/bảng/tiếng Việt được rà soát |
| API và UI live | Chưa kiểm thử bằng API key thật; các race/bug UI được phân loại là tĩnh hoặc cần tái hiện |

Render dùng LibreOffice đã cài trên Windows theo lựa chọn cho phép của người yêu cầu. Ảnh trang/PDF phục vụ QA nằm trong `.qa` và được gitignore; các file giao cho nhóm là DOCX, MD, TXT và sơ đồ trong assets.

## Cập nhật và tái kiểm tra

Sửa hai Markdown nguồn trước, rồi chạy từ thư mục solution với Python có python-docx và Pillow:

```powershell
python docs/tools/build_docs.py
```

Utility tạo lại hai DOCX, 9 PNG sơ đồ và kiểm tra nội dung source. Mặc định dùng font Segoe UI ở Windows cho sơ đồ; nếu dùng hệ điều hành khác cần đặt font hỗ trợ tiếng Việt trong script. Mục lục Word liệt kê các chương theo source; số trang nằm ở footer. Không sửa DOCX riêng rồi bỏ qua nguồn Markdown.

Để lấy schema EF và kiểm thử hai script bằng utility độc lập với solution app:

```powershell
dotnet run --project docs/tools/DbCheck -- schema
dotnet run --project docs/tools/DbCheck -- schema sqlserver
dotnet run --project docs/tools/DbCheck -- sqlite
sqllocaldb start MSSQLLocalDB
dotnet run --project docs/tools/DbCheck -- sqlserver
```

Schema EF được ghi trong `.qa`. SQLite test dùng DB trong bộ nhớ. SQL Server test chỉ kết nối LocalDB, tạo DB tên ngẫu nhiên có prefix `DocuMind_DocsCheck_`, chạy script hai lần, kiểm tra dữ liệu/metadata rồi xóa đúng DB đó. Utility không mở database của app. Nếu không có LocalDB, chỉ chạy SQLite test và nhờ thành viên có SQL Server xác minh script còn lại.

DbCheck tham chiếu Infrastructure và thêm EF SQL Server/Pkcs 10.0.14 chỉ trong project công cụ, không thay package hoặc DI của app. Sau khi sửa schema, cập nhật script và tài liệu cùng lúc; chạy lại test, render Word và rà toàn bộ trang trước khi phát hành bản tài liệu mới.
