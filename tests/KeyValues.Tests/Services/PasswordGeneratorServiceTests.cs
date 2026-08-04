using System.Linq;
using Xunit;
using KeyValues.Services;

namespace KeyValues.Tests.Services;

/// <summary>
/// PasswordGeneratorService のランダムパスワード生成ロジックを検証するテストクラスです。
/// </summary>
public class PasswordGeneratorServiceTests
{
    private readonly PasswordGeneratorService _service = new PasswordGeneratorService();

    [Theory(DisplayName = "パスワード生成: 指定された長さでパスワードが生成されること")]
    [InlineData(8)]
    [InlineData(16)]
    [InlineData(24)]
    public void Generate_VariousLengths_ShouldReturnCorrectLength(int length)
    {
        string result = _service.Generate(length, true, true, true, false);
        Assert.Equal(length, result.Length);
    }

    [Fact(DisplayName = "パスワード生成: 全文字種フラグがOFFの場合は空文字列が返されること")]
    public void Generate_AllFlagsOff_ShouldReturnEmpty()
    {
        string result = _service.Generate(16, false, false, false, false);
        Assert.Equal(string.Empty, result);
    }
}
