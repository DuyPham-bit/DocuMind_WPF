using System.Runtime.Versioning;
using System.Security.Cryptography;
using System.Text;

namespace DocuMind.Infrastructure.Security;

[SupportedOSPlatform("windows")]
public static class WindowsDpapiSecretManager
{
    /// <summary>
    /// Encrypts plain text using Windows DPAPI (CurrentUser scope) and encodes as Base64.
    /// </summary>
    public static string Encrypt(string plainText)
    {
        if (string.IsNullOrEmpty(plainText))
            return string.Empty;

        try
        {
            var plainBytes = Encoding.UTF8.GetBytes(plainText);
            var encryptedBytes = ProtectedData.Protect(plainBytes, null, DataProtectionScope.CurrentUser);
            return Convert.ToBase64String(encryptedBytes);
        }
        catch
        {
            // Fallback nếu không hỗ trợ DPAPI
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(plainText));
        }
    }

    /// <summary>
    /// Decrypts Base64 encrypted text using Windows DPAPI (CurrentUser scope).
    /// </summary>
    public static string Decrypt(string cipherText)
    {
        if (string.IsNullOrEmpty(cipherText))
            return string.Empty;

        try
        {
            var encryptedBytes = Convert.FromBase64String(cipherText);
            var decryptedBytes = ProtectedData.Unprotect(encryptedBytes, null, DataProtectionScope.CurrentUser);
            return Encoding.UTF8.GetString(decryptedBytes);
        }
        catch
        {
            try
            {
                // Fallback nếu chuỗi không phải DPAPI cipher
                return Encoding.UTF8.GetString(Convert.FromBase64String(cipherText));
            }
            catch
            {
                return cipherText;
            }
        }
    }
}

