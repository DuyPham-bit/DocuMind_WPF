using System.Diagnostics;

namespace DocuMind.Application.Services;

public static class UserErrors
{
    public static string Describe(Exception error)
    {
        // Type only: never log exception messages, inner exceptions, URLs, keys or ciphertext.
        Trace.TraceError("DocuMind operation failed: {0}", error.GetType().Name);
        return error switch
        {
            OperationCanceledException => "Thao tác đã hủy hoặc hết thời gian chờ. Dữ liệu đã lưu được giữ nguyên.",
            HttpRequestException http when http.StatusCode != null => $"AI provider trả HTTP {(int)http.StatusCode}. Kiểm tra key/model hoặc thử lại sau.",
            HttpRequestException => "Không thể kết nối provider. Kiểm tra mạng và Base URL.",
            InvalidOperationException => "Thao tác không hợp lệ. Kiểm tra cấu hình provider chat/embedding, model và DPAPI CurrentUser; re-index nếu cần.",
            IOException => "Không thể đọc/ghi tệp. Kiểm tra đường dẫn và quyền truy cập.",
            _ => "Không thể hoàn tất thao tác. Dữ liệu đã lưu được giữ nguyên; hãy kiểm tra cấu hình rồi thử lại."
        };
    }
}
