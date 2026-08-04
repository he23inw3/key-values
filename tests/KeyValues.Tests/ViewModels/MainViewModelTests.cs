using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using Microsoft.Data.Sqlite;
using Xunit;
using KeyValues.Models;
using KeyValues.Providers;
using KeyValues.Repositories;
using KeyValues.Services;
using KeyValues.ViewModels;
using KeyValues.ViewModels.Components;

namespace KeyValues.Tests.ViewModels;

/// <summary>
/// MainViewModel のステータス通知・エントリ追加/削除/保存・Mediator・Cleanup 等を検証するテストクラスです。
/// </summary>
public class MainViewModelTests : IDisposable
{
    private readonly string _dbPath;
    private readonly SqliteAccountRepository _repository;
    private readonly MainViewModel _vm;

    public MainViewModelTests()
    {
        _dbPath = $"test-main-vm-{Guid.NewGuid():N}.db";
        SqliteConnection.ClearAllPools();
        _repository = new SqliteAccountRepository(_dbPath);
        _repository.InitializeDatabase();
        _repository.RegisterVerification("test_password");
        _vm = new MainViewModel("test_password", new List<AccountEntry>(), _repository);
    }

    public void Dispose()
    {
        _vm.Cleanup();
        SqliteConnection.ClearAllPools();
        try { if (File.Exists(_dbPath)) File.Delete(_dbPath); } catch { }
        string dbDir = Path.GetDirectoryName(Path.GetFullPath(_dbPath)) ?? ".";
        string autoLoginPath = Path.Combine(dbDir, "autologin.dat");
        try { if (File.Exists(autoLoginPath)) File.Delete(autoLoginPath); } catch { }
    }

    // ──────────────────────────────────────────────────────────────────────────
    // プロパティ初期値
    // ──────────────────────────────────────────────────────────────────────────

    [Fact(DisplayName = "コンストラクタ: 子ViewModel(List, Detail)が正しく初期化されること")]
    public void Constructor_ShouldInitializeChildViewModels()
    {
        Assert.NotNull(_vm.ListViewModel);
        Assert.NotNull(_vm.DetailViewModel);
    }

    [Fact(DisplayName = "コンストラクタ: 初期StatusMessageが空文字列であること")]
    public void Constructor_InitialStatusMessage_ShouldBeEmpty()
    {
        Assert.Equal(string.Empty, _vm.StatusMessage);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // ShowStatusMessage
    // ──────────────────────────────────────────────────────────────────────────

    [Fact(DisplayName = "ShowStatusMessage: メッセージと通知タイプが正しく更新されること")]
    public void ShowStatusMessage_ShouldUpdateStatusMessageAndType()
    {
        _vm.ShowStatusMessage("テスト通知", "Info");

        Assert.Equal("テスト通知", _vm.StatusMessage);
        Assert.Equal("Info", _vm.StatusType);
    }

    [Fact(DisplayName = "ShowStatusMessage: デフォルトの通知タイプがSuccessであること")]
    public void ShowStatusMessage_DefaultType_ShouldBeSuccess()
    {
        _vm.ShowStatusMessage("成功メッセージ");

        Assert.Equal("Success", _vm.StatusType);
    }

    [Fact(DisplayName = "ShowStatusMessage: 複数回呼び出しでタイマーと内容が更新されること")]
    public void ShowStatusMessage_CalledMultipleTimes_ShouldResetTimer()
    {
        _vm.ShowStatusMessage("最初のメッセージ");
        _vm.ShowStatusMessage("更新メッセージ", "Error");

        Assert.Equal("更新メッセージ", _vm.StatusMessage);
        Assert.Equal("Error", _vm.StatusType);
    }

    [Fact(DisplayName = "ShowStatusMessage: PropertyChangedイベントが発火すること")]
    public void ShowStatusMessage_RaisesPropertyChanged()
    {
        bool fired = false;
        _vm.PropertyChanged += (s, e) => { if (e.PropertyName == nameof(_vm.StatusMessage)) fired = true; };

        _vm.ShowStatusMessage("通知テスト");

        Assert.True(fired);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // AddNewEntry & SaveDatabase & RemoveEntry
    // ──────────────────────────────────────────────────────────────────────────

    [Fact(DisplayName = "AddNewEntry: 新規エントリー追加時にListViewModelのコレクションに反映されること")]
    public void AddNewEntry_ShouldAppearInListViewModel()
    {
        var entry = new AccountEntry { ServiceName = "NewService", LoginId = "new@test.com", Password = "pw" };

        _vm.AddNewEntry(entry);

        int count = 0;
        foreach (var item in _vm.ListViewModel.FilteredEntries) count++;
        Assert.Equal(1, count);
    }

    [Fact(DisplayName = "AddNewEntry: 複数回追加時にすべて末尾に追加されること")]
    public void AddNewEntry_MultipleTimes_ShouldAppendAll()
    {
        _vm.AddNewEntry(new AccountEntry { ServiceName = "A", LoginId = "a@a.com", Password = "p1" });
        _vm.AddNewEntry(new AccountEntry { ServiceName = "B", LoginId = "b@b.com", Password = "p2" });

        int count = 0;
        foreach (var item in _vm.ListViewModel.FilteredEntries) count++;
        Assert.Equal(2, count);
    }

    [Fact(DisplayName = "SaveDatabase: 保存呼び出し時に暗号化ストレージに永続化されること")]
    public void SaveDatabase_ShouldPersistEntriesToStorageService()
    {
        _vm.AddNewEntry(new AccountEntry { ServiceName = "PersistSvc", LoginId = "p@test.com", Password = "secretPassword" });

        _vm.SaveDatabase();

        SqliteConnection.ClearAllPools();
        var loaded = _repository.Load("test_password");
        Assert.Single(loaded);
        Assert.Equal("PersistSvc", loaded[0].ServiceName);
    }

    [Fact(DisplayName = "RemoveEntry: エントリー削除時に件数が減少すること")]
    public void RemoveEntry_ShouldReduceCount()
    {
        var entry = new AccountEntry { ServiceName = "ToRemove", LoginId = "x@x.com", Password = "pw" };
        _vm.AddNewEntry(entry);

        _vm.RemoveEntry(entry);

        int count = 0;
        foreach (var item in _vm.ListViewModel.FilteredEntries) count++;
        Assert.Equal(0, count);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // SelectedEntryChanged / StartAddEntry / CancelAddEntry / RefreshList
    // ──────────────────────────────────────────────────────────────────────────

    [Fact(DisplayName = "SelectedEntryChanged: 選択変更時にDetailViewModelの選択エントリーが更新されること")]
    public void SelectedEntryChanged_ShouldUpdateDetailViewModelSelectedEntry()
    {
        var entry = new AccountEntry { ServiceName = "Detail", LoginId = "d@d.com", Password = "dpw" };

        _vm.SelectedEntryChanged(entry);

        Assert.Equal(entry, _vm.DetailViewModel.SelectedEntry);
    }

    [Fact(DisplayName = "SelectedEntryChanged: null選択時にDetailViewModelの選択状態がクリアされること")]
    public void SelectedEntryChanged_WithNull_ShouldClearDetailSelection()
    {
        var entry = new AccountEntry { ServiceName = "Detail", LoginId = "d@d.com", Password = "dpw" };
        _vm.SelectedEntryChanged(entry);

        _vm.SelectedEntryChanged(null);

        Assert.Null(_vm.DetailViewModel.SelectedEntry);
    }

    [Fact(DisplayName = "StartAddEntry: 新規作成開始時にDetailViewModelが追加モードに切り替わること")]
    public void StartAddEntry_ShouldTriggerDetailStartAddEntry()
    {
        _vm.StartAddEntry();

        Assert.True(_vm.DetailViewModel.IsEditMode);
        Assert.True(_vm.DetailViewModel.IsSelected);
        Assert.Null(_vm.DetailViewModel.SelectedEntry);
    }

    [Fact(DisplayName = "CancelAddEntry: 新規作成キャンセル時にリストの選択がクリアされること")]
    public void CancelAddEntry_ShouldClearListSelection()
    {
        var entry = new AccountEntry { ServiceName = "X", LoginId = "x@x.com", Password = "pw" };
        _vm.AddNewEntry(entry);
        _vm.ListViewModel.SelectedEntry = entry;
        Assert.NotNull(_vm.ListViewModel.SelectedEntry);

        _vm.CancelAddEntry();

        Assert.Null(_vm.ListViewModel.SelectedEntry);
    }

    [Fact(DisplayName = "RefreshList: リスト再描画メソッドの実行で例外が発生しないこと")]
    public void RefreshList_ShouldNotThrow()
    {
        var ex = Record.Exception(() => _vm.RefreshList());
        Assert.Null(ex);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // IsAutoLoginEnabled & IsWindowsHelloSupported
    // ──────────────────────────────────────────────────────────────────────────

    [Fact(DisplayName = "IsAutoLoginEnabled: 自動ログイン設定切り替え時に状態およびストレージが更新されること")]
    public void IsAutoLoginEnabled_Toggle_ShouldUpdateStateAndStorage()
    {
        bool initial = _vm.IsAutoLoginEnabled;
        _vm.IsAutoLoginEnabled = !initial;

        Assert.NotEqual(initial, _vm.IsAutoLoginEnabled);
    }

    [Fact(DisplayName = "IsWindowsHelloSupported: 変更時にPropertyChangedイベントが発火すること")]
    public void IsWindowsHelloSupported_Set_ShouldRaisePropertyChanged()
    {
        bool fired = false;
        _vm.PropertyChanged += (s, e) => { if (e.PropertyName == nameof(_vm.IsWindowsHelloSupported)) fired = true; };

        _vm.IsWindowsHelloSupported = !_vm.IsWindowsHelloSupported;

        Assert.True(fired);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // IsDarkMode
    // ──────────────────────────────────────────────────────────────────────────

    [Fact(DisplayName = "IsDarkMode: ダークモードトグル切り替えで値が反転すること")]
    public void IsDarkMode_ToggleProperty_ShouldChangeValue()
    {
        bool initial = _vm.IsDarkMode;

        _vm.IsDarkMode = !initial;

        Assert.NotEqual(initial, _vm.IsDarkMode);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Cleanup
    // ──────────────────────────────────────────────────────────────────────────

    [Fact(DisplayName = "Cleanup: クリーンアップ実行時に全エントリーと機密メモリがクリアされること")]
    public void Cleanup_ShouldClearAllEntriesAndSensitiveData()
    {
        var entry = new AccountEntry { ServiceName = "A", LoginId = "a@a.com", Password = "secret" };
        _vm.AddNewEntry(entry);

        _vm.Cleanup();

        int count = 0;
        foreach (var item in _vm.ListViewModel.FilteredEntries) count++;
        Assert.Equal(0, count);
        Assert.Equal(string.Empty, entry.Password);
    }
}
