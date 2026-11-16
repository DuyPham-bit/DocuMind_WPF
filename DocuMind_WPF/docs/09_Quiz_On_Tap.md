# Quiz ôn tập từ tài liệu

Tính năng chủ đề học tập của DocuMind: AI soạn câu trắc nghiệm từ tài liệu đã nạp trong workspace, **app tự chấm điểm** (không hỏi lại AI đáp án), hiện giải thích và nguồn (file, trang) cho từng câu, rồi lưu lịch sử ôn tập theo workspace.

## Cách dùng

1. Cấu hình provider có chat **và** embedding (ví dụ OpenRouter hoặc Ollama local, xem [06_Ollama_Local.md](06_Ollama_Local.md)), rồi nạp tài liệu vào workspace.
2. Bấm **Quiz ôn tập** (giao diện tiếng Anh: **Study quiz**) trên thanh trên cùng của cửa sổ chính. Cửa sổ Quiz dùng cùng ngôn ngữ EN / VI với cửa sổ chính; các tên nút bên dưới là bản tiếng Việt (bản tiếng Anh: **Quiz documents**, **Select all**, **Clear**, **Topic**, **Questions**, **✨ Generate quiz**, **Submit**, **↺ Try again**, **QUIZ HISTORY**).
3. Trong **Tài liệu làm quiz**, tích một hoặc nhiều file. Mặc định chọn mọi file sẵn sàng trong workspace; có **Chọn tất cả** và **Bỏ chọn**. File **Cần nạp lại tài liệu** không được chọn. Phải chọn ít nhất một file mới bật nút tạo quiz.
4. Nhập **chủ đề** (ví dụ "Dependency Injection") để lấy các đoạn liên quan nhất **trong các file đã chọn**, hoặc để trống để lấy mẫu trong phạm vi đó. Chọn số câu (3/5/7/10) rồi bấm **✨ Tạo quiz**. Trong lúc tạo, phạm vi tài liệu, chủ đề và số câu được khóa; vẫn có nút Hủy.
5. Chọn đáp án, bấm **Nộp bài**. Đáp án đúng tô xanh, lựa chọn sai tô đỏ; mỗi câu hiện giải thích và nguồn (rê chuột vào nguồn để xem đoạn trích).
6. **↺ Làm lại** xóa lựa chọn để làm lại cùng bộ câu; mỗi lần nộp là một dòng mới trong **Lịch sử ôn tập**.

Bộ lọc dùng DocumentId, không dùng tên file. Tài liệu bị bỏ chọn không được đưa vào prompt. Lọc trước khi xếp hạng/lấy mẫu nên các file khác không chiếm mất số đoạn nguồn. Đây là lấy mẫu có giới hạn, không bảo đảm mỗi file được chọn sẽ có một câu hỏi trong mọi lượt quiz.

Nếu workspace chưa có tài liệu tương thích với profile embedding hiện tại, app báo cần nạp/re-index và **không gọi AI**. Nếu AI trả sai định dạng, app báo tạo lại; câu nào sai cấu trúc bị bỏ, không hiển thị câu lỗi.

## Luồng code

```
QuizWindow (View) ── QuizViewModel ── QuizService ──┬─ RagOrchestratorService.RetrieveSourcesAsync / SampleSourcesAsync
                                                     ├─ IChatProviderFactory.GetChatSessionAsync → IChatCompletionService
                                                     └─ IQuizStore (EfQuizStore → bảng QuizAttempts)
```

| Tầng | File | Vai trò |
| --- | --- | --- |
| Domain | `DocuMind.Domain/Entities/QuizAttempt.cs` | Một lượt nộp bài: workspace, chủ đề, số câu đúng/tổng, JSON câu hỏi + lựa chọn, provider/model. |
| Application | `DTOs/QuizDtos.cs` | `QuizQuestion`, `QuizAnswer` (có `IsCorrect`), `QuizDraft`, `QuizAttemptSummary`. |
| Application | `Services/QuizService.cs` | Lấy căn cứ, dựng prompt, parse JSON phòng thủ, chấm điểm, lưu lượt làm. |
| Application | `Services/RagOrchestratorService.cs` | `RetrieveSourcesAsync` (top-K theo ngưỡng 0.3, dùng chung với chat RAG) và `SampleSourcesAsync` (rải đều, khi không có chủ đề). |
| Infrastructure | `Persistence/EfQuizStore.cs`, migration `AddQuizAttempts` | Lưu/đọc lịch sử; xóa workspace thì cascade xóa lịch sử quiz. |
| WPF | `ViewModels/QuizViewModel.cs`, `Views/QuizWindow.xaml` | MVVM (CommunityToolkit): `GenerateCommand`, `SubmitCommand`, `RetryCommand`, `CancelCommand`; RadioButton nhóm theo từng câu. |
| WPF | `App.xaml.cs` | Đăng ký DI: `IQuizStore`, `QuizService`, `QuizViewModel`. `MainWindow` mở `QuizWindow` qua `OpenQuizAction`. |

### Prompt và an toàn

- Đoạn tài liệu được đóng gói JSON trong khối `DOCUMENT_DATA_<guid ngẫu nhiên>` ở user prompt; system prompt nói rõ đây là dữ liệu không tin cậy (cùng nguyên tắc với chat RAG).
- JSON gửi AI giữ nguyên chữ tiếng Việt (`JavaScriptEncoder.Create(UnicodeRanges.All)`), vẫn escape `<`, `>`, `&`.
- AI được yêu cầu trả `{"questions":[{"question","options"[4],"answer","explanation","source"}]}`. Parser chấp nhận code fence, mảng gốc, đáp án dạng số (0-based) hoặc chữ cái `"B"`, bỏ nhãn `"A. "` trong lựa chọn; loại câu thiếu nội dung, lựa chọn trùng, đáp án ngoài phạm vi; `source` không hợp lệ thì không gắn nguồn giả.
- Temperature lấy từ cấu hình đã lưu; timeout 10 phút, có nút Hủy. Ollama chat dùng context 8192 và tối đa 3072 token đầu ra; bắt đầu bằng 3 câu, giảm số câu nếu model không tạo đủ JSON hợp lệ trong ngân sách đầu ra. Xem `06_Ollama_Local.md`.

## Kiểm thử

`DocuMind.Tests/QuizTests.cs` (16 test, không dùng API thật): parse/loại JSON lỗi, chấm điểm, ranh giới prompt-injection và temperature, không gọi AI khi chưa có tài liệu, lưu lịch sử mới nhất trước và cascade khi xóa workspace, luồng ViewModel tạo → nộp → làm lại, và STA test render `QuizWindow` với RadioButton tách nhóm.

`DocuMind.Tests/QuizDocumentSelectionTests.cs` thêm 8 ca: lọc trước top-K/lấy mẫu, nhiều file trùng tên, selection rỗng không gọi AI, chặn nguồn workspace khác, trạng thái file cần re-index, chọn/bỏ chọn trong ViewModel, truyền đúng file khi tạo và STA test binding checkbox/bố cục cửa sổ nhỏ. Sau khi tích hợp main mới nhất, toàn solution có 111 test pass ở cấu hình Release.

```powershell
dotnet test DocuMind.slnx --filter "FullyQualifiedName~QuizTests|FullyQualifiedName~QuizDocumentSelectionTests"
```

Chất lượng câu hỏi thực tế phụ thuộc model; cần thử với provider thật của nhóm trước khi demo.
