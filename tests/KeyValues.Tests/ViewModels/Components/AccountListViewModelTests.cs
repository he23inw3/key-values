using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Data;
using Microsoft.Data.Sqlite;
using Xunit;
using KeyValues.Models;
using KeyValues.Providers;
using KeyValues.Repositories;
using KeyValues.Services;
using KeyValues.ViewModels;
using KeyValues.ViewModels.Components;

namespace KeyValues.Tests.ViewModels.Components;

/// <summary>
/// AccountListViewModel の検索フィルタリング・選択変更・ClearSelection を検証するテストクラスです。
/// </summary>
public class AccountListViewModelTests : IDisposable
{
    private readonly string _dbPath;
    private readonly SqliteAccountRepository _repository;
    private readonly MainViewModel _mainVm;
    private readonly AccountListViewModel _listVm;

    public AccountListViewModelTests()
    {
        _dbPath = $"test-list-vm-{Guid.NewGuid():N}.db";
        SqliteConnection.ClearAllPools();
        _repository = new SqliteAccountRepository(_dbPath);

        var initialEntries = new List<AccountEntry>
        {
            new AccountEntry { ServiceName = "GitHub", LoginId = "dev@github.com", Password = "pw1" },
            new AccountEntry { ServiceName = "Google", LoginId = "user@gmail.com", Password = "pw2" },
            new AccountEntry { ServiceName = "Amazon", LoginId = "buyer@amz.com", Password = "pw3" },
        };

        _mainVm = new MainViewModel("test_password", initialEntries, _repository);
        _listVm = _mainVm.ListViewModel;
    }

    public void Dispose()
    {
        _mainVm.Cleanup();
        SqliteConnection.ClearAllPools();
        if (File.Exists(_dbPath)) File.Delete(_dbPath);
    }

    private int CountFiltered()
    {
        int count = 0;
        foreach (var _ in _listVm.FilteredEntries) count++;
        return count;
    }

    // ──────────────────────────────────────────────────────────────────────────
    // 初期状態
    // ──────────────────────────────────────────────────────────────────────────

    [Fact(DisplayName = "初期状態: すべてのエントリーがフィルタに表示されること")]
    public void InitialState_ShouldShowAllEntries()
    {
        Assert.Equal(3, CountFiltered());
    }

    [Fact(DisplayName = "初期状態: 検索テキストが空文字列であること")]
    public void InitialSearchText_ShouldBeEmpty()
    {
        Assert.Equal(string.Empty, _listVm.SearchText);
    }

    [Fact(DisplayName = "初期状態: 選択エントリーがnullであること")]
    public void InitialSelectedEntry_ShouldBeNull()
    {
        Assert.Null(_listVm.SelectedEntry);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // SearchText によるフィルタリング
    // ──────────────────────────────────────────────────────────────────────────

    [Fact(DisplayName = "検索フィルター: サービス名での部分一致検索ができること")]
    public void SearchText_MatchingServiceName_ShouldFilterResults()
    {
        _listVm.SearchText = "GitHub";

        Assert.Equal(1, CountFiltered());
    }

    [Fact(DisplayName = "検索フィルター: 大文字・小文字を区別せず検索できること")]
    public void SearchText_CaseInsensitive_ShouldFilterResults()
    {
        _listVm.SearchText = "github";

        Assert.Equal(1, CountFiltered());
    }

    [Fact(DisplayName = "検索フィルター: ログインIDでの部分一致検索ができること")]
    public void SearchText_MatchingLoginId_ShouldFilterResults()
    {
        _listVm.SearchText = "gmail.com";

        Assert.Equal(1, CountFiltered());
    }

    [Fact(DisplayName = "検索フィルター: 一致するものが存在しない場合に空リストになること")]
    public void SearchText_NoMatch_ShouldReturnEmpty()
    {
        _listVm.SearchText = "xyzNotExist123";

        Assert.Equal(0, CountFiltered());
    }

    [Fact(DisplayName = "検索フィルター: 検索文字列クリア時に全件表示に戻ること")]
    public void SearchText_ClearedAfterFilter_ShouldRestoreAll()
    {
        _listVm.SearchText = "GitHub";
        _listVm.SearchText = string.Empty;

        Assert.Equal(3, CountFiltered());
    }

    [Fact(DisplayName = "検索フィルター: 中間一致検索が正しく機能すること")]
    public void SearchText_PartialMatch_ShouldFilterCorrectly()
    {
        _listVm.SearchText = "oo"; // GitHub(no), Google(yes), Amazon(no)

        Assert.Equal(1, CountFiltered());
    }

    // ──────────────────────────────────────────────────────────────────────────
    // SelectedEntry の変更通知
    // ──────────────────────────────────────────────────────────────────────────

    [Fact(DisplayName = "SelectedEntry: 変更時にPropertyChangedイベントが発火すること")]
    public void SelectedEntry_WhenSet_ShouldRaisePropertyChanged()
    {
        bool fired = false;
        _listVm.PropertyChanged += (s, e) => { if (e.PropertyName == nameof(_listVm.SelectedEntry)) fired = true; };

        var entry = new AccountEntry { ServiceName = "X", LoginId = "x@x.com", Password = "pw" };
        _mainVm.AddNewEntry(entry);
        _listVm.SelectedEntry = entry;

        Assert.True(fired);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // ClearSelection
    // ──────────────────────────────────────────────────────────────────────────

    [Fact(DisplayName = "ClearSelection: 選択状態がnullに解除されること")]
    public void ClearSelection_ShouldSetSelectedEntryToNull()
    {
        var entry = new AccountEntry { ServiceName = "Y", LoginId = "y@y.com", Password = "pw" };
        _mainVm.AddNewEntry(entry);
        _listVm.SelectedEntry = entry;
        Assert.NotNull(_listVm.SelectedEntry);

        _listVm.ClearSelection();

        Assert.Null(_listVm.SelectedEntry);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // SearchText プロパティ変更通知
    // ──────────────────────────────────────────────────────────────────────────

    [Fact(DisplayName = "SearchText: 変更時にPropertyChangedイベントが発火すること")]
    public void SearchText_WhenChanged_ShouldRaisePropertyChanged()
    {
        bool fired = false;
        _listVm.PropertyChanged += (s, e) => { if (e.PropertyName == nameof(_listVm.SearchText)) fired = true; };

        _listVm.SearchText = "Test";

        Assert.True(fired);
    }
}
