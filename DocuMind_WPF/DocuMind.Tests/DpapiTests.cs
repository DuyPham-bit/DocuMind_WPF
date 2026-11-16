using System.Runtime.Versioning;
using DocuMind.Infrastructure.Security;
using FluentAssertions;
using Xunit;

namespace DocuMind.Tests;

public class DpapiTests
{
    [Fact]
    [SupportedOSPlatform("windows")]
    public void Encrypt_And_Decrypt_ReturnsOriginalPlainText()
    {
        string secretApiKey = "AIzaSyDummyGeminiKey_123456789_abcdef";

        string encrypted = WindowsDpapiSecretManager.Encrypt(secretApiKey);
        encrypted.Should().NotBeNullOrEmpty();
        encrypted.Should().NotBe(secretApiKey);

        string decrypted = WindowsDpapiSecretManager.Decrypt(encrypted);
        decrypted.Should().Be(secretApiKey);
    }

    [Fact]
    [SupportedOSPlatform("windows")]
    public void Encrypt_EmptyString_ReturnsEmptyString()
    {
        WindowsDpapiSecretManager.Encrypt("").Should().BeEmpty();
        WindowsDpapiSecretManager.Decrypt("").Should().BeEmpty();
    }
}

