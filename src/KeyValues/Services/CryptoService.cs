using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace KeyValues.Services;

/// <summary>
/// AES-256 (CBC) と PBKDF2 鍵派生を使用した暗号化・復号処理を提供するサービスです。
/// </summary>
public class CryptoService
{
    private const int KeySize = 256; // AES-256
    private const int BlockSize = 128; // AES block size
    private const int SaltSize = 16; // 128 bits
    private const int IvSize = 16; // 128 bits
    private const int Pbkdf2Iterations = 100000;

    /// <summary>
    /// 平文の文字列をマスターパスワードで暗号化し、ソルトとIVを付与したバイト配列を返します。
    /// </summary>
    public byte[] Encrypt(string plainText, string password)
    {
        if (string.IsNullOrEmpty(plainText))
            throw new ArgumentException("Plain text cannot be empty.", nameof(plainText));
        if (string.IsNullOrEmpty(password))
            throw new ArgumentException("Password cannot be empty.", nameof(password));

        byte[] salt = new byte[SaltSize];
        byte[] iv = new byte[IvSize];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(salt);
            rng.GetBytes(iv);
        }

        byte[] key = DeriveKey(password, salt);
        byte[] encryptedData;

        try
        {
            using (var aes = Aes.Create())
            {
                aes.KeySize = KeySize;
                aes.BlockSize = BlockSize;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;
                aes.Key = key;
                aes.IV = iv;

                using (var ms = new MemoryStream())
                {
                    using (var encryptor = aes.CreateEncryptor())
                    using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                    {
                        byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);
                        cs.Write(plainBytes, 0, plainBytes.Length);
                        cs.FlushFinalBlock();
                        Array.Clear(plainBytes, 0, plainBytes.Length);
                    }
                    encryptedData = ms.ToArray();
                }
            }

            byte[] result = new byte[salt.Length + iv.Length + encryptedData.Length];
            Buffer.BlockCopy(salt, 0, result, 0, salt.Length);
            Buffer.BlockCopy(iv, 0, result, salt.Length, iv.Length);
            Buffer.BlockCopy(encryptedData, 0, result, salt.Length + iv.Length, encryptedData.Length);

            return result;
        }
        finally
        {
            Array.Clear(key, 0, key.Length);
        }
    }

    /// <summary>
    /// 暗号化されたバイト配列をマスターパスワードで復号し、平文の文字列を返します。
    /// </summary>
    public string Decrypt(byte[] cipherTextWithHeader, string password)
    {
        if (cipherTextWithHeader == null || cipherTextWithHeader.Length < SaltSize + IvSize)
            throw new ArgumentException("Invalid cipher text length.", nameof(cipherTextWithHeader));
        if (string.IsNullOrEmpty(password))
            throw new ArgumentException("Password cannot be empty.", nameof(password));

        byte[] salt = new byte[SaltSize];
        byte[] iv = new byte[IvSize];
        byte[] encryptedData = new byte[cipherTextWithHeader.Length - SaltSize - IvSize];

        Buffer.BlockCopy(cipherTextWithHeader, 0, salt, 0, SaltSize);
        Buffer.BlockCopy(cipherTextWithHeader, SaltSize, iv, 0, IvSize);
        Buffer.BlockCopy(cipherTextWithHeader, SaltSize + IvSize, encryptedData, 0, encryptedData.Length);

        byte[] key = DeriveKey(password, salt);

        try
        {
            using (var aes = Aes.Create())
            {
                aes.KeySize = KeySize;
                aes.BlockSize = BlockSize;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;
                aes.Key = key;
                aes.IV = iv;

                using (var ms = new MemoryStream(encryptedData))
                using (var cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Read))
                using (var sr = new StreamReader(cs, Encoding.UTF8))
                {
                    return sr.ReadToEnd();
                }
            }
        }
        finally
        {
            Array.Clear(key, 0, key.Length);
        }
    }

    private static byte[] DeriveKey(string password, byte[] salt)
    {
        using (var pbkdf2 = new Rfc2898DeriveBytes(password, salt, Pbkdf2Iterations, HashAlgorithmName.SHA256))
        {
            return pbkdf2.GetBytes(KeySize / 8);
        }
    }
}
