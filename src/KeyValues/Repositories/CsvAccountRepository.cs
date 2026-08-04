using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using KeyValues.Models;

namespace KeyValues.Repositories;

/// <summary>
/// CSV ファイル形式のアカウントデータ解析および読み出しを行うリポジトリ実装です。
/// </summary>
public class CsvAccountRepository
{
    /// <summary>
    /// CSVファイルを読み込み、AccountEntryのリストに変換します。
    /// ヘッダー行が存在する場合はスキップします。
    /// </summary>
    public List<AccountEntry> Import(string filePath)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException("CSV file not found.", filePath);
        }

        var importedEntries = new List<AccountEntry>();

        using (var reader = new StreamReader(filePath, Encoding.UTF8))
        {
            string? headerLine = reader.ReadLine();
            if (headerLine == null)
                return importedEntries;

            bool hasHeader = DetectHeader(headerLine);
            
            string? currentLine = hasHeader ? reader.ReadLine() : headerLine;

            while (currentLine != null)
            {
                if (string.IsNullOrWhiteSpace(currentLine))
                {
                    currentLine = reader.ReadLine();
                    continue;
                }

                List<string> fields = ParseCsvLine(currentLine);
                if (fields.Count > 0)
                {
                    var entry = new AccountEntry
                    {
                        Id = Guid.NewGuid().ToString(),
                        ServiceName = fields.Count > 0 ? fields[0].Trim() : string.Empty,
                        LoginId = fields.Count > 1 ? fields[1].Trim() : string.Empty,
                        Password = fields.Count > 2 ? fields[2] : string.Empty,
                        Url = fields.Count > 3 ? fields[3].Trim() : string.Empty,
                        Memo = fields.Count > 4 ? fields[4] : string.Empty,
                        UpdatedAt = DateTime.Now
                    };

                    if (!string.IsNullOrEmpty(entry.ServiceName) || !string.IsNullOrEmpty(entry.LoginId))
                    {
                        importedEntries.Add(entry);
                    }
                }

                currentLine = reader.ReadLine();
            }
        }

        return importedEntries;
    }

    private List<string> ParseCsvLine(string line)
    {
        var result = new List<string>();
        bool inQuotes = false;
        var field = new StringBuilder();

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];
            if (inQuotes)
            {
                if (c == '"')
                {
                    if (i + 1 < line.Length && line[i + 1] == '"')
                    {
                        field.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = false;
                    }
                }
                else
                {
                    field.Append(c);
                }
            }
            else
            {
                if (c == '"')
                {
                    inQuotes = true;
                }
                else if (c == ',')
                {
                    result.Add(field.ToString());
                    field.Clear();
                }
                else
                {
                    field.Append(c);
                }
            }
        }
        result.Add(field.ToString());
        return result;
    }

    private bool DetectHeader(string firstLine)
    {
        string lower = firstLine.ToLower();
        return lower.Contains("service") || 
               lower.Contains("login") || 
               lower.Contains("password") || 
               lower.Contains("id") || 
               lower.Contains("url") || 
               lower.Contains("memo") ||
               lower.Contains("サービス") ||
               lower.Contains("ログイン") ||
               lower.Contains("パスワード") ||
               lower.Contains("メモ");
    }
}
