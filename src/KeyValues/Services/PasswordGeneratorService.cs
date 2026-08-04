using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace KeyValues.Services;

/// <summary>
/// 暗号強度の高いランダムパスワード生成機能を提供するサービスです。
/// </summary>
public class PasswordGeneratorService
{
    /// <summary>
    /// 指定された条件に従ってランダムなパスワードを生成します。
    /// </summary>
    public string Generate(int length, bool useUpper, bool useLower, bool useDigits, bool useSymbols)
    {
        if (!useUpper && !useLower && !useDigits && !useSymbols)
            return string.Empty;

        const string uppercase = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        const string lowercase = "abcdefghijklmnopqrstuvwxyz";
        const string digits = "0123456789";
        const string symbols = "!@#$%^&*()_+-=[]{}|;:,.<>?";

        var charPool = new StringBuilder();
        var requiredChars = new List<char>();

        if (useUpper)
        {
            charPool.Append(uppercase);
            requiredChars.Add(uppercase[RandomNumberGenerator.GetInt32(uppercase.Length)]);
        }
        if (useLower)
        {
            charPool.Append(lowercase);
            requiredChars.Add(lowercase[RandomNumberGenerator.GetInt32(lowercase.Length)]);
        }
        if (useDigits)
        {
            charPool.Append(digits);
            requiredChars.Add(digits[RandomNumberGenerator.GetInt32(digits.Length)]);
        }
        if (useSymbols)
        {
            charPool.Append(symbols);
            requiredChars.Add(symbols[RandomNumberGenerator.GetInt32(symbols.Length)]);
        }

        if (length < requiredChars.Count)
            length = requiredChars.Count;

        string pool = charPool.ToString();
        var resultChars = new List<char>(requiredChars);

        for (int i = requiredChars.Count; i < length; i++)
        {
            resultChars.Add(pool[RandomNumberGenerator.GetInt32(pool.Length)]);
        }

        // フィッシャー–イェーツのシャッフルアルゴリズムで文字順をランダム化
        for (int i = resultChars.Count - 1; i > 0; i--)
        {
            int j = RandomNumberGenerator.GetInt32(i + 1);
            (resultChars[i], resultChars[j]) = (resultChars[j], resultChars[i]);
        }

        return new string(resultChars.ToArray());
    }
}
