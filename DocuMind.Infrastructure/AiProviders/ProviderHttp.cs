using System.Net;

namespace DocuMind.Infrastructure.AiProviders;

internal static class ProviderHttp
{
    public static async Task<HttpResponseMessage> SendAsync(HttpClient http, Func<HttpRequestMessage> create,
        bool retry, CancellationToken ct)
    {
        for (var attempt = 0; ; attempt++)
        {
            using var request = create();
            HttpResponseMessage response;
            try { response = await http.SendAsync(request, ct); }
            catch (HttpRequestException) { throw new HttpRequestException("Không thể kết nối AI provider. Kiểm tra mạng và Base URL."); }
            if (response.IsSuccessStatusCode) return response;
            var status = response.StatusCode;
            response.Dispose();
            if (!retry || attempt >= 2 || !(status is HttpStatusCode.TooManyRequests or HttpStatusCode.RequestTimeout || (int)status >= 500))
                throw new HttpRequestException($"AI provider trả lỗi HTTP {(int)status}. Kiểm tra key/model hoặc thử lại sau.", null, status);
            await Task.Delay(TimeSpan.FromMilliseconds(200 * (attempt + 1)), ct);
        }
    }
}
