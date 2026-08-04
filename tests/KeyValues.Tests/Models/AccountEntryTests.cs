using System;
using Xunit;
using KeyValues.Models;

namespace KeyValues.Tests.Models;

/// <summary>
/// AccountEntry モデルのデフォルト値および基本プロパティ設定を検証するテストクラスです。
/// </summary>
public class AccountEntryTests
{
    [Fact(DisplayName = "デフォルトコンストラクタ: 各文字列プロパティが空文字列で初期化されること")]
    public void DefaultConstructor_ShouldSetEmptyStrings()
    {
        var entry = new AccountEntry();

        Assert.Equal(string.Empty, entry.ServiceName);
        Assert.Equal(string.Empty, entry.LoginId);
        Assert.Equal(string.Empty, entry.Password);
        Assert.Equal(string.Empty, entry.Url);
        Assert.Equal(string.Empty, entry.Memo);
    }

    [Fact(DisplayName = "デフォルトコンストラクタ: 新しいGUID形式のIDが割り当てられること")]
    public void DefaultConstructor_ShouldAssignNewGuidId()
    {
        var entry = new AccountEntry();

        Assert.False(string.IsNullOrEmpty(entry.Id));
        Assert.True(Guid.TryParse(entry.Id, out _));
    }

    [Fact(DisplayName = "デフォルトコンストラクタ: UpdatedAtが現在日時に設定されること")]
    public void DefaultConstructor_ShouldSetUpdatedAtToNow()
    {
        var before = DateTime.Now.AddSeconds(-1);
        var entry = new AccountEntry();
        var after = DateTime.Now.AddSeconds(1);

        Assert.InRange(entry.UpdatedAt, before, after);
    }

    [Fact(DisplayName = "プロパティ: 各プロパティが正しく設定・取得できること")]
    public void Properties_ShouldBeSettable()
    {
        var entry = new AccountEntry
        {
            ServiceName = "Amazon",
            LoginId = "buyer@amazon.com",
            Password = "SecretPassword!",
            Url = "https://amazon.co.jp",
            Memo = "Prime account",
            UpdatedAt = new DateTime(2024, 6, 1)
        };

        Assert.Equal("Amazon", entry.ServiceName);
        Assert.Equal("buyer@amazon.com", entry.LoginId);
        Assert.Equal("SecretPassword!", entry.Password);
        Assert.Equal("https://amazon.co.jp", entry.Url);
        Assert.Equal("Prime account", entry.Memo);
        Assert.Equal(new DateTime(2024, 6, 1), entry.UpdatedAt);
    }

    [Fact(DisplayName = "インスタンス生成: 異なるインスタンスで互いに異なるIDが割り当てられること")]
    public void TwoInstances_ShouldHaveDifferentIds()
    {
        var entry1 = new AccountEntry();
        var entry2 = new AccountEntry();

        Assert.NotEqual(entry1.Id, entry2.Id);
    }
}
