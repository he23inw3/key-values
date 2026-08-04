using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Xunit;
using KeyValues.Models;
using KeyValues.Repositories;

namespace KeyValues.Tests.Repositories;

/// <summary>
/// CsvAccountRepository の CSV 解析ロジックを検証するテストクラスです。
/// </summary>
public class CsvAccountRepositoryTests
{
    private readonly CsvAccountRepository _repo = new CsvAccountRepository();

    private string WriteTempCsv(string content)
    {
        string path = $"test-csv-{Guid.NewGuid():N}.csv";
        File.WriteAllText(path, content, Encoding.UTF8);
        return path;
    }

    [Fact(DisplayName = "CSVインポート: ヘッダー行が存在する場合にスキップしてデータ行を読み込めること")]
    public void Import_WithHeader_ShouldSkipHeaderAndReturnData()
    {
        string path = WriteTempCsv("service,login,password,url,memo\nGoogle,user@g.com,pass123,https://google.com,work\n");
        try
        {
            var result = _repo.Import(path);
            Assert.Single(result);
            Assert.Equal("Google", result[0].ServiceName);
            Assert.Equal("user@g.com", result[0].LoginId);
        }
        finally { File.Delete(path); }
    }

    [Fact(DisplayName = "CSVインポート: ヘッダー行が存在しない場合に全行をデータとして読み込めること")]
    public void Import_WithoutHeader_ShouldReturnAllRows()
    {
        string path = WriteTempCsv("Amazon,buyer@amz.com,amazPw,https://amazon.co.jp,\n");
        try
        {
            var result = _repo.Import(path);
            Assert.Single(result);
            Assert.Equal("Amazon", result[0].ServiceName);
        }
        finally { File.Delete(path); }
    }

    [Fact(DisplayName = "CSVインポート: クォート（\"）で囲まれたカンマを含むフィールドを正しくパースできること")]
    public void Import_WithQuotedFields_ShouldParseCorrectly()
    {
        string path = WriteTempCsv("\"TestApp\",\"user@test.com\",\"pa,ss\",\"\",\"\"\n");
        try
        {
            var result = _repo.Import(path);
            Assert.Single(result);
            Assert.Equal("TestApp", result[0].ServiceName);
            Assert.Equal("pa,ss", result[0].Password);
        }
        finally { File.Delete(path); }
    }
}
