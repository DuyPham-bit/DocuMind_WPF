using System.Runtime.Versioning;
using System.Security.Cryptography;
using System.Text;
using DocuMind.Application.Interfaces;

namespace DocuMind.Infrastructure.Security;

[SupportedOSPlatform("windows")]
public sealed class WindowsDpapiSecretProtector : ISecretProtector
{
    public string Protect(string plainText) => WindowsDpapiSecretManager.Encrypt(plainText);
    public string Unprotect(string cipherText) => WindowsDpapiSecretManager.Decrypt(cipherText);
}

[SupportedOSPlatform("windows")]
public static class WindowsDpapiSecretManager
{
    public static string Encrypt(string plainText)
    {
        if (string.IsNullOrEmpty(plainText)) return "";
        try
        {
            return Convert.ToBase64String(ProtectedData.Protect(Encoding.UTF8.GetBytes(plainText), null, DataProtectionScope.CurrentUser));
        }
        catch (Exception ex) when (ex is CryptographicException or PlatformNotSupportedException)
        {
            throw new InvalidOperationException("Windows DPAPI không thể mã hóa key. Cấu hình chưa được lưu.");
        }
    }

    public static string Decrypt(string cipherText)
    {
        if (string.IsNullOrEmpty(cipherText)) return "";
        try
        {
            return Encoding.UTF8.GetString(ProtectedData.Unprotect(Convert.FromBase64String(cipherText), null, DataProtectionScope.CurrentUser));
        }
        catch (Exception ex) when (ex is CryptographicException or FormatException or PlatformNotSupportedException)
        {
            throw new InvalidOperationException("Không thể giải mã key bằng DPAPI CurrentUser. Hãy nhập lại key và Lưu.");
        }
    }
}
