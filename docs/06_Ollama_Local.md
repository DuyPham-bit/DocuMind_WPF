# Chạy DocuMind miễn phí trên máy từng thành viên

Mỗi máy cài Ollama và tải model một lần. Chat và embedding chạy trên máy, không dùng quota OpenRouter. Sau khi tải xong có thể dùng offline. Tốc độ tùy phần cứng; đóng game khi dùng để nhường VRAM.

## 1. Cài và tải model

Cài từ https://ollama.com/download/windows hoặc chạy PowerShell:

```powershell
winget install --id Ollama.Ollama --exact --accept-package-agreements --accept-source-agreements
```

Mở terminal mới sau khi cài. Ollama cần đang chạy ở khay hệ thống.

Nếu ổ C ít dung lượng, đặt thư mục model ở ổ khác **trước khi tải**, ví dụ (đổi đường dẫn phù hợp máy):

```powershell
[Environment]::SetEnvironmentVariable('OLLAMA_MODELS', 'E:\Ollama\models', 'User')
```

Thoát Ollama ở khay hệ thống rồi mở lại để nhận biến môi trường. Không cần mở cổng LAN: mỗi thành viên dùng máy riêng, API mặc định ở localhost.

```powershell
ollama pull qwen3:4b-instruct
ollama pull bge-m3
ollama list
```

Qwen3 4B Instruct khoảng 2.5 GB, BGE-M3 khoảng 1.2 GB, chưa tính bộ cài/runtime. Bản instruct trả lời trực tiếp, phù hợp demo. BGE-M3 hỗ trợ tìm kiếm đa ngôn ngữ. Ưu tiên RAM 16 GB trở lên; máy chỉ có CPU vẫn cần thử tốc độ thực tế trước demo.

## 2. Cấu hình DocuMind

Mở **Cài đặt API Key & Nền tảng**, chọn:

| Trường | Giá trị |
|---|---|
| Provider | `OllamaLocal` |
| API Key | Để trống |
| Chat Model | `qwen3:4b-instruct` |
| Embedding Model | `bge-m3` |
| Base URL | `http://localhost:11434/v1` |
| Temperature | `0.3` |
| Dùng provider này cho embedding/RAG | Bật |

Bấm **Kiểm tra kết nối**, **Kiểm tra embedding**, sau đó **Lưu cấu hình**. Lần đầu tải model vào RAM/VRAM có thể lâu hơn. Kiểm tra chưa lưu cấu hình; cần bấm Lưu.

## 3. Re-index và kiểm tra

Sau khi đổi embedding, dùng **Nạp tài liệu** chọn lại đúng file nguồn cũ trong từng workspace. Không cần xóa tài liệu: app tạo phiên bản mới và chỉ kích hoạt sau khi hoàn tất, giữ index cũ nếu thất bại. File nguồn đã di chuyển cần chọn lại vị trí mới.

1. Tắt RAG, hỏi một câu tiếng Việt để thử chat local.
2. Nạp TXT/MD nhỏ, bật RAG và hỏi một dữ kiện có trong file.
3. Kiểm tra câu trả lời và nguồn trích dẫn trước khi nạp PDF lớn.
4. Chạy `ollama ps` xem GPU/CPU; `ollama list` kiểm tra tên model khớp form.

Lỗi kết nối: mở Ollama, kiểm tra URL/cổng 11434. Model not found: tải đúng tên bằng `ollama pull`. Chậm/hết VRAM: đóng game và ứng dụng dùng GPU. PDF scan cần OCR trước vì parser hiện trích xuất văn bản.

## Máy đã khảo sát ngày 07/11/2026

Windows 11 Pro, Intel i5-12400F (6 nhân/12 luồng), RAM 31.8 GB, RTX 5060 8 GB. Chọn cặp model trên để ưu tiên tốc độ và chừa VRAM cho desktop. Model lưu ở `D:\Ollama\models` trên SSD Kingston; ổ E là HDD ngoài nên không dùng làm nơi chạy model. Đây là lựa chọn theo tài nguyên máy, không phải benchmark mọi model.

Đã kiểm tra thực tế: Ollama 0.40.15; hai model tải đủ và xác minh digest; cả hai báo `100% GPU` trong `ollama ps`; kiểm tra chat và embedding từ form DocuMind thành công. Đã lưu `OllamaLocal`, re-index `Database_SQLServer.txt` (1 thành công, 0 lỗi), hỏi app dùng SQLite hay SQL Server và nhận đúng câu trả lời SQLite kèm nguồn trang 1. Đây là smoke test cho setup, chưa phải đánh giá chất lượng trên toàn bộ PDF của nhóm. Các PDF/index cũ cần nạp lại file nguồn theo bước 3.

Nguồn: [Ollama Windows](https://docs.ollama.com/windows), [API](https://docs.ollama.com/api/openai-compatibility), [Qwen3 4B Instruct](https://ollama.com/library/qwen3:4b-instruct), [BGE-M3](https://ollama.com/library/bge-m3).
