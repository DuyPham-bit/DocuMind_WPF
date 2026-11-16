# DemoProject - bộ PDF PRN212 để thử DocuMind

Ngày biên soạn: 16/11/2026. Bốn tài liệu tiếng Việt tự biên soạn, mỗi tài liệu 4 trang, khoảng 89-91 KB/file; văn bản thật, chọn/copy và lập chỉ mục được, không cần OCR. Đây là tài liệu demo dựa trên source trong workspace, không phải đề thi hay giáo trình chính thức.

| PDF | Chủ đề | Chức năng nên thử |
| --- | --- | --- |
| `01_CSharp_OOP_PRN212.pdf` | Kiểu dữ liệu, BMI, đóng gói, abstract class, interface, đa hình | Định nghĩa, tính toán nhỏ, so sánh, quiz OOP |
| `02_Delegate_LINQ_Async_PRN212.pdf` | Delegate, lambda, Func/Action/Predicate, LINQ, async, dữ liệu ShopLab | Lọc nhiều điều kiện, tổng hợp số liệu, truy xuất bảng |
| `03_WPF_MVVM_PRN212.pdf` | DataContext, binding, notification, command, trạng thái UI | Giải thích tình huống lỗi, quiz WPF/MVVM, workspace |
| `04_EFCore_DI_DocuMind_PRN212.pdf` | EF Core, CRUD, transaction, DI lifetime, pipeline RAG | Hỏi đáp kiến trúc, nhiều nguồn, phân biệt các kiểu lỗi |

## Bắt đầu

1. Đóng phiên DocuMind đang chạy để lưu trạng thái rồi build/mở lại **solution bên trong**: `DocuMind_WPF/DocuMind.slnx`, tính từ thư mục gốc repository (cùng cấp `ASM1`, `ASM2` và `DemoProject`).
2. Đảm bảo Ollama server đang chạy. Manage providers: `OllamaLocal`, chat `qwen3:4b-instruct`, embedding `bge-m3`, Base URL `http://localhost:11434/v1`, bật dùng cho embedding. Test rồi Save nếu thay cấu hình.
3. Tạo workspace riêng tên `PRN212 Demo`; nạp **một PDF trước**, chờ index xong rồi mới nạp thêm ba PDF còn lại. Chỉ nạp các file `.pdf`, không nạp README hoặc mã builder.
4. Bật **Search documents**. Dùng câu hỏi bên dưới, ưu tiên yêu cầu trả lời tối đa 2-3 câu kèm nguồn. Không cần nạp lại PDF cũ chỉ vì thay giới hạn context.
5. Study quiz: trong **Tài liệu làm quiz**, tích một hoặc nhiều PDF muốn dùng; file cần re-index bị khóa, nút tạo bị khóa khi không chọn file nào. Bắt đầu 3 câu với chủ đề cụ thể `Đóng gói`, `LINQ`, `MVVM` hoặc `Dependency Injection lifetime`, sau đó thử 5 câu. Khi để chủ đề trống, app chỉ lấy mẫu từ các file đã chọn; không bảo đảm phủ mọi file/trang trong một lượt quiz.

## Câu hỏi chuẩn và đáp án để đối chiếu

Các đáp án dưới đây dành cho người kiểm thử; không nạp file này vào workspace vì sẽ làm lẫn đáp án vào corpus. Cho phép cách diễn đạt khác nếu đúng nội dung và nguồn.

| ID | Câu hỏi gợi ý | Đáp án cần có | Nguồn kỳ vọng |
| --- | --- | --- | --- |
| Q01 | BMI của 72 kg và 1,8 m trong ví dụ là bao nhiêu? Chỉ tính số học. | Khoảng 22,22; không tự suy ra phân loại sức khỏe. | PDF 01, trang 1 |
| Q02 | Vì sao Line không nên bị ép triển khai GetArea? | Không có ý nghĩa diện tích của bài; IMeasurable dành cho đối tượng đo diện tích/chu vi. | PDF 01, trang 3 |
| Q03 | Trong ShopLab, giá 0 và tồn kho 0 có hợp lệ không? | Có theo quy ước demo; số âm không hợp lệ. Không suy rộng ra mọi cửa hàng. | PDF 01, trang 4; PDF 03, trang 3 |
| Q04 | area(3,5) và power(2,3) trả bao nhiêu? | 15 và 8. | PDF 02, trang 1 |
| Q05 | SHOP-DEMO-212 có bao nhiêu mặt hàng và tổng tồn kho? | 4 mặt hàng, 47 sản phẩm. | PDF 02, trang 2 |
| Q06 | Tổng giá trị tồn kho là bao nhiêu? Phân biệt tổng giá. | 1.540.000 VND; tổng giá bốn mặt hàng là 180.000 VND, không phải giá trị tồn. | PDF 02, trang 2 |
| Q07 | Hàng nhập khẩu giá trên 50.000 VND và còn hàng là gì? | Nho tím, Id 104, CategoryId 2, Price 80.000 VND, Stock 5. | PDF 02, trang 2 |
| Q08 | Where(Stock == 0) trả mặt hàng nào? | Cam, Id 103, Stock 0. | PDF 02, trang 2-3 |
| Q09 | ObservableCollection có tự báo khi sửa Price của một Fruit không? | Collection báo thêm/xóa/sắp phần tử; thay property item cần notification của item. | PDF 03, trang 2 |
| Q10 | Save trong ShopLab được bật khi nào? | Name không trống, Price >= 0, Stock >= 0, IsBusy false; phải đánh giá lại CanExecute. | PDF 03, trang 3 |
| Q11 | ShopSetting và DocuMind có cùng provider database không? | Không; ShopSetting dùng SQL Server, DocuMind dùng SQLite. | PDF 04, trang 1 |
| Q12 | Bán 3 Táo xanh từ tồn 12 thì còn bao nhiêu, tổng tiền bao nhiêu? | Còn 9; tổng tiền 135.000 VND. | PDF 04, trang 2; PDF 02, trang 2 |
| Q13 | Phân biệt Transient, Scoped, Singleton trong tài liệu. | Mỗi resolve; cùng scope; cùng service provider. WPF phải quản lý scope phù hợp. | PDF 04, trang 3 |
| Q14 | DocuMind chia đoạn và lấy nguồn mặc định thế nào? | 180 từ, overlap 30 từ, topK 4, ngưỡng cosine 0,3; phải đúng profile embedding. | PDF 04, trang 4 |
| Q15 | Giá không âm được bảo vệ ở đâu trong ShopLab? Hãy tổng hợp nhiều nguồn. | Bất biến của đối tượng; validation/CanExecute UI; service và ranh giới ghi. | PDF 01 trang 2, PDF 03 trang 3, PDF 04 trang 2 |
| Q16 | Ngày thi chính thức PRN212 của lớp là ngày nào? | Bộ PDF không cung cấp; không bịa ngày. | Không có dữ kiện; PDF 01 trang 4 nêu giới hạn |
| Q17 | Mật khẩu database của tôi là gì? | Không có trong tài liệu; không suy đoán. | Không có dữ kiện |

Số trang là số in ở chân trang và cũng là chỉ số trang vật lý của PDF. Có thể chấp nhận nguồn bổ sung từ trang khác nếu nguồn đó chứa đúng dữ kiện; không chấp nhận chỉ trả lời đúng mà trích file/trang không liên quan.

## Kịch bản kiểm thử chức năng

| Kịch bản | Thao tác | Tiêu chí đánh giá |
| --- | --- | --- |
| Nạp và parser | Nạp mỗi file riêng, rồi cả bốn | Ready, tên file đúng, không báo scan/OCR, số tài liệu đúng |
| Trích dẫn | Hỏi Q02, Q06, mở nguồn | Đúng file và trang có căn cứ |
| Hỏi nối tiếp | Hỏi Q07 rồi "CategoryId của mặt hàng đó là gì?" | Giữ ngữ cảnh hội thoại, trả 2 nếu retrieval có căn cứ |
| Nhiều tài liệu | Hỏi Q15, yêu cầu nêu các lớp bảo vệ | Đối chiếu OOP, WPF và lưu dữ liệu; xem cả đúng/sai của nguồn |
| Không đủ dữ kiện | Hỏi Q16, Q17 | Nói thiếu thông tin, không bịa dữ kiện |
| Workspace riêng | Tạo workspace trống rồi hỏi Q06 | Không lấy nguồn từ workspace PRN212 Demo |
| New chat | Bấm New chat | Hội thoại mới; các PDF trong workspace vẫn còn |
| Quiz | Tạo 3 câu, chọn đáp án, nộp, làm lại | Câu có 4 lựa chọn, chấm điểm/giải thích/nguồn đúng; mỗi lần nộp có lịch sử |
| Chọn nguồn quiz | Bỏ chọn tất cả, chỉ chọn PDF 01, tạo quiz; sau đó chọn thêm PDF 03 | Khi rỗng không tạo; câu hỏi và nguồn chỉ thuộc các PDF đã chọn, cả khi để trống chủ đề |
| Re-index | Nạp lại đúng một PDF cùng đường dẫn | Không tăng số tài liệu do duplicate; nguồn mới vẫn dùng được |
| Export | Xuất hội thoại sau vài câu hỏi | File xuất có nội dung đúng theo phạm vi app hỗ trợ |
| Hủy | Hủy khi tạo quiz hoặc chat đang xử lý | UI hết busy, thông báo rõ, thao tác tiếp theo dùng được |

Chất lượng AI không được bảo đảm chỉ bằng HTTP 200. Đánh giá riêng đáp án, phép tính và trích dẫn. Một câu trả lời có thể cần nhiều đoạn; topK và độ tương đồng ảnh hưởng khả năng lấy đủ nguồn.

## Bản sửa context trong source

- Ollama chat chuyển sang `/api/chat`, gửi `num_ctx=8192`, `num_predict=3072`, giữ history và temperature. Context này áp dụng theo request, không cần sửa model hoặc khởi động lại Ollama.
- Embedding vẫn dùng `/v1/embeddings` và `bge-m3`, vì vậy không đổi profile index đang có.
- HttpClient, chat và tạo quiz: tối đa 10 phút; test cấu hình: 2 phút; ingest: vẫn 10 phút/file. Nút Hủy vẫn hoạt động.
- JSON nguồn RAG giữ nguyên dấu tiếng Việt, vẫn escape ký tự HTML nhạy cảm; giảm việc phình prompt thành `\u...`.
- 8.192 là tổng context mỗi lượt, vẫn có giới hạn. Hội thoại quá dài hoặc quiz quá nhiều chữ vẫn có thể cần giảm đầu vào/số câu. App chưa hiển thị streaming.
- API native và cách cấu hình context được đối chiếu với [Ollama chat API](https://docs.ollama.com/api/chat) và [OpenAI compatibility](https://docs.ollama.com/api/openai-compatibility).

## Kiểm tra đã thực hiện

- `dotnet test` cấu hình Release sau khi tích hợp main mới nhất: **DocuMind 111 pass, ASM1 13 pass, ASM2 20 pass**, không có test fail hoặc skip. ASM1 còn cảnh báo NuGet NU1902/NU1903 cho dependency có sẵn; không thay dependency ASM1 trong nhánh này.
- Gọi thật qua adapter Ollama đã sửa với prompt **4.949 token**: **HTTP 200**, trả `OK`; `/api/ps` báo context **8.192**.
- Embedding thật `bge-m3`: **1.024 chiều**, vector hữu hạn.
- Cả bốn PDF: **4 trang/file**, text tiếng Việt trích được bằng chính `PdfPigDocumentParser` của ứng dụng, trên 6.700 ký tự/file.
- Render 16 trang bằng Poppler và kiểm tra trực quan: font tiếng Việt, bảng, code, footer và số trang.
- Chạy pipeline thật trên database tạm riêng: nạp **4 tài liệu, 33 chunk**; RAG trả đúng **47 sản phẩm, 1.540.000 VND**, dẫn đúng PDF 02 trang 2.
- Quiz thật chủ đề DI lifetime tạo được **3 câu hợp lệ** về Transient, Scoped và Singleton, không timeout.
- Những bước UI trong bảng trên là checklist để thao tác khi demo; không khẳng định đã tự động điều khiển giao diện.

## Tái tạo PDF

Builder gốc nằm tại `tools/build_demo_pdfs.py`, cần Python + ReportLab và font Arial của Windows:

```powershell
python .\DemoProject\tools\build_demo_pdfs.py
```

Xem `REVIEW_MAP.md` để biết phạm vi rà soát và nguồn của bốn chủ đề.
