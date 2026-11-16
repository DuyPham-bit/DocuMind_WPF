using System.Diagnostics;

namespace DocuMind.Application.Services;

// An error whose message is written for the user and contains no secrets, so it may be shown as-is.
public sealed class UserFacingException(string message) : InvalidOperationException(message);

public static class UserErrors
{
    public static string Describe(Exception error)
    {
        // Type only: never log exception messages, inner exceptions, URLs, keys or ciphertext.
        Trace.TraceError("DocuMind operation failed: {0}", error.GetType().Name);
        return error switch
        {
            UserFacingException userFacing => userFacing.Message,
            OperationCanceledException => AppText.T("The operation was canceled or timed out. Existing saved data was kept.",
                "Thao tác đã bị hủy hoặc hết thời gian chờ. Dữ liệu đã lưu vẫn được giữ nguyên."),
            HttpRequestException http when http.StatusCode != null => AppText.T($"The AI provider returned HTTP {(int)http.StatusCode}. Check the key or model, then try again.",
                $"Nhà cung cấp AI trả lỗi HTTP {(int)http.StatusCode}. Hãy kiểm tra key hoặc model rồi thử lại."),
            HttpRequestException => AppText.T("Unable to reach the provider. Check your network connection and base URL.",
                "Không kết nối được tới nhà cung cấp AI. Hãy kiểm tra mạng và Base URL."),
            InvalidOperationException => AppText.T("The operation cannot be completed. Check the chat or embedding provider, model, and DPAPI settings; re-index if needed.",
                "Không thể hoàn tất thao tác. Hãy kiểm tra provider chat hoặc embedding, model và DPAPI; nạp lại tài liệu nếu cần."),
            IOException => AppText.T("Unable to read or write a file. Check the path and permissions.",
                "Không đọc hoặc ghi được file. Hãy kiểm tra đường dẫn và quyền truy cập."),
            _ => AppText.T("The operation could not be completed. Existing saved data was kept; check your settings and try again.",
                "Không thể hoàn tất thao tác. Dữ liệu đã lưu vẫn được giữ nguyên; hãy kiểm tra cài đặt rồi thử lại.")
        };
    }
}
