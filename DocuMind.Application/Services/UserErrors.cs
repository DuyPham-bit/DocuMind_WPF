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
            OperationCanceledException => "The operation was canceled or timed out. Existing saved data was kept.",
            HttpRequestException http when http.StatusCode != null => $"The AI provider returned HTTP {(int)http.StatusCode}. Check the key or model, then try again.",
            HttpRequestException => "Unable to reach the provider. Check your network connection and base URL.",
            InvalidOperationException => "The operation cannot be completed. Check the chat or embedding provider, model, and DPAPI settings; re-index if needed.",
            IOException => "Unable to read or write a file. Check the path and permissions.",
            _ => "The operation could not be completed. Existing saved data was kept; check your settings and try again."
        };
    }
}
