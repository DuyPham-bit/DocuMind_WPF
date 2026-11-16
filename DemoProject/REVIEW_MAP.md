# Bản đồ nội dung đã rà soát để xây dựng corpus

Mục tiêu đợt rà soát là chọn nội dung PRN212 đủ dùng để thử DocuMind, không phải chứng nhận mọi bài tập đều không có bug. Đã lập bản đồ thư mục trong toàn workspace, đọc các source và tài liệu đại diện cho từng cụm học phần cùng các đường gọi chính của DocuMind; không đọc lại từng bản sao giống nhau hoặc coi các project PRN222 đi kèm là giáo trình PRN212.

| Cụm thư mục | Nội dung đối chiếu | Quyết định biên soạn |
| --- | --- | --- |
| `Session01_Hello`, `Session02_Language` | HelloCS; tham số; `HealthV2/.../BmiCalculator.cs`; các biến thể Health | PDF 01: nền tảng C#, tham số và bài tính có đáp án |
| `Session03_OOP` | `Goldie.ShapeManager.V4`: Shape, IMeasurable, Line, Rectangle, Disk; các phiên bản Geometry và collection | PDF 01: hợp đồng, kế thừa, đa hình; không buộc Line tính diện tích |
| `Session04-Delegate`, `20-10` | Các nhóm DelegateBasic, Returned, Action, Func, lambda; GiaoLang.DieuDao | PDF 02: chữ ký delegate, lambda, truy vấn và thông báo |
| `Session05-GUI` | ShopDieuDao, MainWindow và cấu trúc WPF | PDF 03: XAML, DataContext, binding; bổ sung MVVM theo source DocuMind |
| `Session06-Database` | Shop/ShopSetting, FruitShopDbContext, Fruit, Category, SQL Server | PDF 04: EF Core, quan hệ dữ liệu, so sánh với SQLite của DocuMind |
| `ASM`, `FinalProject/DocuMind_WPF/ASM1` | Tài liệu hướng dẫn/review và ProductManagerMvc | Bối cảnh CRUD, repository; không biến corpus thành tài liệu web thay cho WPF |
| `FinalProject/DocuMind_WPF/ASM2` | USER_GUIDE, Program.cs, đăng ký DI và ba marker lifetime | PDF 04: Transient/Scoped/Singleton, composition root, test bằng fake |
| `FinalProject/DocuMind_WPF/DocuMind_WPF` | Domain, Application, Infrastructure, WPF, Tests; tài liệu 06-10; README/USER_GUIDE | PDF 03-04: workspace, quiz, RAG, profile, transaction, lỗi và context |
| `PE`, `PE-EXAM` | Bản đồ solution Q1/Q2 và bài thực hành | Giữ mức giải thích và câu hỏi ngắn phù hợp thực hành; không sao chép đề thi nguyên văn |
| `TaiLieu`, `CODE`, `Slot1` | Bản đồ tài liệu báo cáo/quy tắc và tài liệu khởi đầu | Không đưa thông tin tổ chức chưa xác nhận, lịch thi hay điểm số vào PDF |

Đường DocuMind rút gọn trong footer PDF được hiểu tương đối với `FinalProject/DocuMind_WPF/DocuMind_WPF/`. Các đường `Session...` và `FinalProject/...` tính từ `D:/FPT/PRN212/`.

## Phân biệt source thật và ví dụ mới

- Fruit của bài database gốc dùng `double Price`, không có Stock trong file đã đối chiếu. ShopLab trong PDF chủ động dùng `decimal` và thêm Stock để có phép tổng hợp và validation rõ ràng. Không khẳng định source gốc đã có các thay đổi đó.
- SHOP-DEMO-212 D1, các giá/tồn kho và mã neo OOP-212/DEL-212/WPF-212/DI-212 đều do bộ demo tự tạo. Không truy vấn hoặc xuất dữ liệu thật của FruitShopDB.
- Chọn bốn chủ đề có liên hệ để thử truy xuất nhiều file; giữ dữ kiện chuẩn trong PDF 02, nhắc lại có kiểm soát ở PDF 04.
- Không thêm PDF scan hoặc tài liệu trống vào bộ chính, vì parser hiện hỗ trợ text PDF và yêu cầu này tập trung kiểm tra tính năng thành công. Các lỗi provider/cancel/thiếu nguồn được thử bằng kịch bản riêng.
