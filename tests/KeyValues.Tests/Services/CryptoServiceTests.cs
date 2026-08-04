using System;
using System.Security.Cryptography;
using Xunit;
using KeyValues.Services;

namespace KeyValues.Tests.Services;

/// <summary>
/// CryptoService の暗号化・復号処理を検証するテストクラスです。
/// </summary>
public class CryptoServiceTests
{
    private readonly CryptoService _cryptoService = new CryptoService();

    [Fact(DisplayName = "暗号化・復号: 正しいマスターパスワードで元の文字列に復号できること")]
    public void EncryptAndDecrypt_WithCorrectPassword_ShouldReturnOriginalText()
    {
        string originalText = "My Secret Account Password 123!@#";
        string password = "StrongMasterPassword123";

        byte[] encrypted = _cryptoService.Encrypt(originalText, password);
        string decrypted = _cryptoService.Decrypt(encrypted, password);

        Assert.NotNull(encrypted);
        Assert.NotEmpty(encrypted);
        Assert.Equal(originalText, decrypted);
    }

    [Fact(DisplayName = "復号: 誤ったマスターパスワードを指定した場合にCryptographicExceptionが発生すること")]
    public void Decrypt_WithIncorrectPassword_ShouldThrowCryptographicException()
    {
        string originalText = "Sensitive Data";
        byte[] encrypted = _cryptoService.Encrypt(originalText, "CorrectPassword");

        Assert.Throws<CryptographicException>(() =>
        {
            _cryptoService.Decrypt(encrypted, "WrongPassword");
        });
    }
}
