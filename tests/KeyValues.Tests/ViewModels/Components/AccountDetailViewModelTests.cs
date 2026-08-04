using System;
using System.Collections.Generic;
using System.IO;
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
/// AccountDetailViewModel の選択・編集バッファ・コマンド（Edit, Save, Cancel, Copy）などのロジックを検証するテストクラスです。
/// </summary>
public class AccountDetailViewModelTests : IDisposable
{
    private readonly string _dbPath;
    private readonly SqliteAccountRepository _repository;
    private readonly MainViewModel _mainVm;
    private readonly AccountDetailViewModel _detailVm;

    public AccountDetailViewModelTests()
    {
        _dbPath = $"test-detail-vm-{Guid.NewGuid():N}.db";
        SqliteConnection.ClearAllPools();
        _repository = new SqliteAccountRepository(_dbPath);
        _mainVm = new MainViewModel("test_password", new List<AccountEntry>(), _repository);
        _detailVm = _mainVm.DetailViewModel;
    }

    public void Dispose()
    {
        _mainVm.Cleanup();
        SqliteConnection.ClearAllPools();
        if (File.Exists(_dbPath)) File.Delete(_dbPath);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // SelectedEntry 設定時の動作
    // ──────────────────────────────────────────────────────────────────────────

    [Fact(DisplayName = "SelectedEntry: 値設定時にIsSelectedがtrueになること")]
    public void SelectedEntry_WhenSet_ShouldSetIsSelectedToTrue()
    {
        _detailVm.SelectedEntry = new AccountEntry { ServiceName = "Test", LoginId = "t@t.com", Password = "pw" };
        Assert.True(_detailVm.IsSelected);
    }

    [Fact(DisplayName = "SelectedEntry: null設定時にIsSelectedがfalseになること")]
    public void SelectedEntry_WhenSetToNull_ShouldSetIsSelectedToFalse()
    {
        _detailVm.SelectedEntry = new AccountEntry { ServiceName = "Test", LoginId = "t@t.com", Password = "pw" };
        _detailVm.SelectedEntry = null;
        Assert.False(_detailVm.IsSelected);
    }

    [Fact(DisplayName = "SelectedEntry: 変更時に編集モードが解除されること")]
    public void SelectedEntry_WhenSet_ShouldExitEditMode()
    {
        _detailVm.SelectedEntry = new AccountEntry { ServiceName = "Test", LoginId = "t@t.com", Password = "pw" };
        _detailVm.IsEditMode = true;
        _detailVm.SelectedEntry = new AccountEntry { ServiceName = "Other", LoginId = "o@o.com", Password = "pw2" };

        Assert.False(_detailVm.IsEditMode);
    }

    [Fact(DisplayName = "SelectedEntry: 設定時に編集バッファへディープコピーされること")]
    public void SelectedEntry_WhenSet_ShouldCloneToEditBuffer()
    {
        var entry = new AccountEntry
        {
            ServiceName = "EditTest",
            LoginId = "e@e.com",
            Password = "ep",
            Url = "https://test.com",
            Memo = "memo here"
        };
        _detailVm.SelectedEntry = entry;

        Assert.Equal("EditTest", _detailVm.EditBuffer.ServiceName);
        Assert.Equal("e@e.com", _detailVm.EditBuffer.LoginId);
        Assert.Equal("ep", _detailVm.EditBuffer.Password);
        Assert.Equal("https://test.com", _detailVm.EditBuffer.Url);
        Assert.Equal("memo here", _detailVm.EditBuffer.Memo);
    }

    [Fact(DisplayName = "SelectedEntry: 設定時にパスワード表示フラグがリセット（非表示）されること")]
    public void SelectedEntry_WhenSet_ShouldHidePassword()
    {
        _detailVm.IsPasswordVisible = true;
        _detailVm.SelectedEntry = new AccountEntry { ServiceName = "S", LoginId = "l@l.com", Password = "p" };

        Assert.False(_detailVm.IsPasswordVisible);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // StartAddEntry
    // ──────────────────────────────────────────────────────────────────────────

    [Fact(DisplayName = "StartAddEntry: 新規追加モードで編集フラグと選択状態が適切に設定されること")]
    public void StartAddEntry_ShouldSetEditAndSelectedFlags()
    {
        _detailVm.StartAddEntry();

        Assert.True(_detailVm.IsSelected);
        Assert.True(_detailVm.IsEditMode);
        Assert.Null(_detailVm.SelectedEntry);
    }

    [Fact(DisplayName = "StartAddEntry: 空の編集バッファが生成されること")]
    public void StartAddEntry_ShouldCreateEmptyEditBuffer()
    {
        _detailVm.StartAddEntry();

        Assert.Equal(string.Empty, _detailVm.EditBuffer.ServiceName);
        Assert.Equal(string.Empty, _detailVm.EditBuffer.LoginId);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // ExecuteEditEntry / CancelEditCommand
    // ──────────────────────────────────────────────────────────────────────────

    [Fact(DisplayName = "EditEntryCommand: 選択エントリーが存在する場合に編集モードへ移行すること")]
    public void EditEntryCommand_WhenSelectedEntryNotNull_ShouldEnterEditMode()
    {
        var entry = new AccountEntry { ServiceName = "Svc", LoginId = "user@test.com", Password = "pw" };
        _detailVm.SelectedEntry = entry;
        Assert.False(_detailVm.IsEditMode);

        _detailVm.EditEntryCommand.Execute(null);

        Assert.True(_detailVm.IsEditMode);
    }

    [Fact(DisplayName = "EditEntryCommand: 選択エントリーがnullの場合に編集モードに移行しないこと")]
    public void EditEntryCommand_WhenSelectedEntryIsNull_ShouldNotEnterEditMode()
    {
        _detailVm.SelectedEntry = null;

        _detailVm.EditEntryCommand.Execute(null);

        Assert.False(_detailVm.IsEditMode);
    }

    [Fact(DisplayName = "CancelEditCommand: 新規追加中のキャンセルで状態が初期化されること")]
    public void CancelEditCommand_InNewEntryMode_ShouldResetFlagsAndClearSelection()
    {
        _detailVm.StartAddEntry();
        Assert.True(_detailVm.IsEditMode);

        _detailVm.CancelEditCommand.Execute(null);

        Assert.False(_detailVm.IsEditMode);
        Assert.False(_detailVm.IsSelected);
    }

    [Fact(DisplayName = "CancelEditCommand: 既存編集中のキャンセルで元の内容に復元されること")]
    public void CancelEditCommand_InEditExistingMode_ShouldRestoreOriginalBuffer()
    {
        var entry = new AccountEntry { ServiceName = "Original", LoginId = "orig@test.com", Password = "pw" };
        _detailVm.SelectedEntry = entry;
        _detailVm.EditEntryCommand.Execute(null);

        _detailVm.EditBuffer.ServiceName = "Modified";
        _detailVm.CancelEditCommand.Execute(null);

        Assert.False(_detailVm.IsEditMode);
        Assert.Equal("Original", _detailVm.EditBuffer.ServiceName);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // ExecuteSaveEntry (SaveEntryCommand)
    // ──────────────────────────────────────────────────────────────────────────

    [Fact(DisplayName = "SaveEntryCommand: 必須項目（サービス名・ログインID・パスワード）のいずれかが空の場合にCanExecuteがfalseを返すこと")]
    public void SaveEntryCommand_CanExecute_WithMissingRequiredFields_ShouldReturnFalse()
    {
        _detailVm.StartAddEntry();
        _detailVm.EditBuffer.ServiceName = "GitHub";
        _detailVm.EditBuffer.LoginId = "user";
        _detailVm.EditBuffer.Password = ""; // パスワード未入力

        Assert.False(_detailVm.SaveEntryCommand.CanExecute(null));
    }

    [Fact(DisplayName = "SaveEntryCommand: 必須項目（サービス名・ログインID・パスワード）がすべて入力されている場合にCanExecuteがtrueを返すこと")]
    public void SaveEntryCommand_CanExecute_WithAllRequiredFields_ShouldReturnTrue()
    {
        _detailVm.StartAddEntry();
        _detailVm.EditBuffer.ServiceName = "GitHub";
        _detailVm.EditBuffer.LoginId = "user@test.com";
        _detailVm.EditBuffer.Password = "password123";

        Assert.True(_detailVm.SaveEntryCommand.CanExecute(null));
    }

    [Fact(DisplayName = "SaveEntryCommand: 新規追加モードで新規エントリーが保存・登録されること")]
    public void SaveEntryCommand_NewEntryMode_ShouldAddNewEntryToMainViewModel()
    {
        _detailVm.StartAddEntry();
        _detailVm.EditBuffer.ServiceName = "NewService";
        _detailVm.EditBuffer.LoginId = "newuser@test.com";
        _detailVm.EditBuffer.Password = "newpass123";

        _detailVm.SaveEntryCommand.Execute(null);

        Assert.False(_detailVm.IsEditMode);
        Assert.NotNull(_detailVm.SelectedEntry);
        Assert.Equal("NewService", _detailVm.SelectedEntry.ServiceName);
        Assert.Equal("newuser@test.com", _detailVm.SelectedEntry.LoginId);
    }

    [Fact(DisplayName = "SaveEntryCommand: 既存エントリーの更新内容が正しく保存されること")]
    public void SaveEntryCommand_ExistingEntryMode_ShouldUpdateEntry()
    {
        var original = new AccountEntry { ServiceName = "OldName", LoginId = "old@test.com", Password = "old" };
        _mainVm.AddNewEntry(original);
        _detailVm.SelectedEntry = original;

        _detailVm.EditEntryCommand.Execute(null);
        _detailVm.EditBuffer.ServiceName = "UpdatedName";
        _detailVm.EditBuffer.LoginId = "updated@test.com";
        _detailVm.EditBuffer.Password = "updatedPw";

        _detailVm.SaveEntryCommand.Execute(null);

        Assert.False(_detailVm.IsEditMode);
        Assert.Equal("UpdatedName", _detailVm.SelectedEntry!.ServiceName);
        Assert.Equal("updated@test.com", _detailVm.SelectedEntry.LoginId);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // TogglePasswordVisibility & OpenGenerator
    // ──────────────────────────────────────────────────────────────────────────

    [Fact(DisplayName = "TogglePasswordVisibilityCommand: パスワード表示トグルでフラグが反転すること")]
    public void TogglePasswordVisibilityCommand_ShouldToggleFlag()
    {
        bool initialState = _detailVm.IsPasswordVisible;

        _detailVm.TogglePasswordVisibilityCommand.Execute(null);
        Assert.NotEqual(initialState, _detailVm.IsPasswordVisible);

        _detailVm.TogglePasswordVisibilityCommand.Execute(null);
        Assert.Equal(initialState, _detailVm.IsPasswordVisible);
    }

    [Fact(DisplayName = "PasswordGenerator: インスタンスが正しく初期化されていること")]
    public void PasswordGenerator_ShouldBeInitialized()
    {
        Assert.NotNull(_detailVm.PasswordGenerator);
    }

    [Fact(DisplayName = "OpenGeneratorCommand: 生成ダイアログの表示表示フラグが切り替わること")]
    public void OpenGeneratorCommand_ShouldToggleIsGeneratorOpen()
    {
        bool initial = _detailVm.PasswordGenerator.IsGeneratorOpen;

        _detailVm.OpenGeneratorCommand.Execute(null);
        Assert.NotEqual(initial, _detailVm.PasswordGenerator.IsGeneratorOpen);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // PropertyChanged 通知
    // ──────────────────────────────────────────────────────────────────────────

    [Fact(DisplayName = "IsEditMode: 変更時にPropertyChangedイベントが発火すること")]
    public void IsEditMode_Set_ShouldRaisePropertyChanged()
    {
        bool fired = false;
        _detailVm.PropertyChanged += (s, e) => { if (e.PropertyName == nameof(_detailVm.IsEditMode)) fired = true; };

        _detailVm.IsEditMode = true;

        Assert.True(fired);
    }

    [Fact(DisplayName = "IsPasswordVisible: 変更時にPropertyChangedイベントが発火すること")]
    public void IsPasswordVisible_Set_ShouldRaisePropertyChanged()
    {
        bool fired = false;
        _detailVm.PropertyChanged += (s, e) => { if (e.PropertyName == nameof(_detailVm.IsPasswordVisible)) fired = true; };

        _detailVm.IsPasswordVisible = true;

        Assert.True(fired);
    }

    [Fact(DisplayName = "EditBuffer: 変更時にPropertyChangedイベントが発火すること")]
    public void EditBuffer_Set_ShouldRaisePropertyChanged()
    {
        bool fired = false;
        _detailVm.PropertyChanged += (s, e) => { if (e.PropertyName == nameof(_detailVm.EditBuffer)) fired = true; };

        _detailVm.EditBuffer = new AccountEntry();

        Assert.True(fired);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Cleanup
    // ──────────────────────────────────────────────────────────────────────────

    [Fact(DisplayName = "Cleanup: クリーンアップ処理で例外が発生しないこと")]
    public void Cleanup_ShouldNotThrow()
    {
        var ex = Record.Exception(() => _detailVm.Cleanup());
        Assert.Null(ex);
    }
}
