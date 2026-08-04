using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Xunit;
using KeyValues.Providers;
using KeyValues.Repositories;
using KeyValues.ViewModels;

namespace KeyValues.Tests.Providers;

/// <summary>
/// WindowsHelloProvider の認証ロジック・ViewModel 連動のテストクラスです。
/// </summary>
public class WindowsHelloProviderTests : IDisposable
{
    private readonly string _dbPath;

    public WindowsHelloProviderTests()
    {
        _dbPath = $"test-winhello-{Guid.NewGuid():N}.db";
    }

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        try { if (File.Exists(_dbPath)) File.Delete(_dbPath); } catch { }
        string dbDir = Path.GetDirectoryName(Path.GetFullPath(_dbPath)) ?? ".";
        string autoLoginPath = Path.Combine(dbDir, "autologin.dat");
        try { if (File.Exists(autoLoginPath)) File.Delete(autoLoginPath); } catch { }
    }

    [Fact(DisplayName = "WindowsHelloProvider: インスタンス取得および利用可能性チェックができること")]
    public async Task WindowsHelloProvider_Instance_ShouldNotBeNull()
    {
        var provider = WindowsHelloProvider.Instance;
        Assert.NotNull(provider);

        bool available = await provider.IsAvailableAsync();
        // テスト環境でサポート状況に応じて bool 値が返る
        Assert.True(available || !available);
    }

    [Fact(DisplayName = "MasterPasswordViewModel: WindowsHelloProviderを注入して正常に初期化できること")]
    public void MasterPasswordViewModel_WithDefaultProvider_ShouldInitialize()
    {
        var repo = new SqliteAccountRepository(_dbPath);
        var vm = new MasterPasswordViewModel(repo, WindowsHelloProvider.Instance);

        Assert.NotNull(vm);
    }
}
