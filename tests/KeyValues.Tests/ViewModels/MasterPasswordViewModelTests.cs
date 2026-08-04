using System;
using System.IO;
using Microsoft.Data.Sqlite;
using Xunit;
using KeyValues.Providers;
using KeyValues.Repositories;
using KeyValues.Services;
using KeyValues.ViewModels;

namespace KeyValues.Tests.ViewModels;

/// <summary>
/// MasterPasswordViewModel の Submit メソッドの各分岐（セットアップ・ロック解除）と
/// プロパティ変更通知を検証するテストクラスです。
/// </summary>
public class MasterPasswordViewModelTests : IDisposable
{
    private readonly string _dbPath;

    public MasterPasswordViewModelTests()
    {
        _dbPath = $"test-mpvm-{Guid.NewGuid():N}.db";
    }

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        try { if (File.Exists(_dbPath)) File.Delete(_dbPath); } catch { }
        string dbDir = Path.GetDirectoryName(Path.GetFullPath(_dbPath)) ?? ".";
        string autoLoginPath = Path.Combine(dbDir, "autologin.dat");
        try { if (File.Exists(autoLoginPath)) File.Delete(autoLoginPath); } catch { }
    }

    private MasterPasswordViewModel CreateVm()
    {
        SqliteConnection.ClearAllPools();
        var repo = new SqliteAccountRepository(_dbPath);
        return new MasterPasswordViewModel(repo, WindowsHelloProvider.Instance);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // IsSetupMode の初期値
    // ──────────────────────────────────────────────────────────────────────────

    [Fact(DisplayName = "コンストラクタ: データベース未存在時はセットアップモード(IsSetupMode=true)になること")]
    public void Constructor_WhenNoDatabaseExists_ShouldBeInSetupMode()
    {
        var vm = CreateVm();
        Assert.True(vm.IsSetupMode);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // TitleText / SubtitleText（computed プロパティ）
    // ──────────────────────────────────────────────────────────────────────────

    [Fact(DisplayName = "TitleText: セットアップモード時のタイトル文字列が含まれること")]
    public void TitleText_WhenSetupMode_ShouldContainSetup()
    {
        var vm = CreateVm();
        Assert.Contains("初期設定", vm.TitleText);
    }

    [Fact(DisplayName = "SubtitleText: セットアップモード時のサブタイトル文字列が含まれること")]
    public void SubtitleText_WhenSetupMode_ShouldContainCreate()
    {
        var vm = CreateVm();
        Assert.Contains("新規作成", vm.SubtitleText);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Submit — セットアップモード（新規登録）
    // ──────────────────────────────────────────────────────────────────────────

    [Fact(DisplayName = "Submit(セットアップ): 空のパスワード入力時に失敗(false)しエラーメッセージが設定されること")]
    public void Submit_SetupMode_WithEmptyPassword_ShouldReturnFalse()
    {
        var vm = CreateVm();
        vm.Password = string.Empty;

        bool result = vm.Submit();

        Assert.False(result);
        Assert.False(string.IsNullOrEmpty(vm.ErrorMessage));
    }

    [Fact(DisplayName = "Submit(セットアップ): 8文字未満の短いパスワード入力時に失敗(false)すること")]
    public void Submit_SetupMode_WithShortPassword_ShouldReturnFalse()
    {
        var vm = CreateVm();
        vm.Password = "1234567"; // 7文字 < 8文字

        bool result = vm.Submit();

        Assert.False(result);
        Assert.Contains("8文字以上", vm.ErrorMessage);
    }

    [Fact(DisplayName = "Submit(セットアップ): 確認用パスワード不一致時に失敗(false)すること")]
    public void Submit_SetupMode_WithMismatchedConfirmPassword_ShouldReturnFalse()
    {
        var vm = CreateVm();
        vm.Password = "StrongPass1";
        vm.ConfirmPassword = "DifferentPass1";

        bool result = vm.Submit();

        Assert.False(result);
        Assert.Contains("一致しません", vm.ErrorMessage);
    }

    [Fact(DisplayName = "Submit(セットアップ): ヒントなしで正常なパスワード入力時に成功(true)すること")]
    public void Submit_SetupMode_WithValidInputAndNoHint_ShouldReturnTrue()
    {
        var vm = CreateVm();
        vm.Password = "ValidPass123";
        vm.ConfirmPassword = "ValidPass123";
        vm.PasswordHint = string.Empty;

        bool result = vm.Submit();

        Assert.True(result);
    }

    [Fact(DisplayName = "Submit(セットアップ): ヒントありで正常なパスワード入力時に成功(true)すること")]
    public void Submit_SetupMode_WithValidInputAndHint_ShouldReturnTrue()
    {
        var vm = CreateVm();
        vm.Password = "ValidPass456";
        vm.ConfirmPassword = "ValidPass456";
        vm.PasswordHint = "My pet's name";

        bool result = vm.Submit();

        Assert.True(result);
    }

    [Fact(DisplayName = "Submit(セットアップ): 登録成功時にエラーメッセージがクリアされること")]
    public void Submit_SetupMode_Success_ShouldClearErrorMessage()
    {
        var vm = CreateVm();
        vm.Password = "ValidPass789";
        vm.ConfirmPassword = "ValidPass789";

        vm.Submit();

        Assert.Empty(vm.ErrorMessage);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Submit — ロック解除モード
    // ──────────────────────────────────────────────────────────────────────────

    [Fact(DisplayName = "Submit(ロック解除): 空のパスワード入力時に失敗(false)すること")]
    public void Submit_UnlockMode_WithEmptyPassword_ShouldReturnFalse()
    {
        // まずセットアップで DB を作成
        var setupVm = CreateVm();
        setupVm.Password = "CorrectPw123";
        setupVm.ConfirmPassword = "CorrectPw123";
        setupVm.Submit();
        SqliteConnection.ClearAllPools();

        // 同じ DB をロック解除モードで開く
        var unlockVm = CreateVm();
        unlockVm.Password = string.Empty;

        bool result = unlockVm.Submit();

        Assert.False(result);
        Assert.False(string.IsNullOrEmpty(unlockVm.ErrorMessage));
    }

    [Fact(DisplayName = "Submit(ロック解除): 正しいマスターパスワード入力時に成功(true)すること")]
    public void Submit_UnlockMode_WithCorrectPassword_ShouldReturnTrue()
    {
        // セットアップ
        var setupVm = CreateVm();
        setupVm.Password = "CorrectPw123";
        setupVm.ConfirmPassword = "CorrectPw123";
        setupVm.Submit();
        SqliteConnection.ClearAllPools();

        // ロック解除
        var unlockVm = CreateVm();
        unlockVm.Password = "CorrectPw123";

        bool result = unlockVm.Submit();

        Assert.True(result);
    }

    [Fact(DisplayName = "Submit(ロック解除): 誤ったマスターパスワード入力時に失敗(false)すること")]
    public void Submit_UnlockMode_WithWrongPassword_ShouldReturnFalse()
    {
        // セットアップ
        var setupVm = CreateVm();
        setupVm.Password = "CorrectPw123";
        setupVm.ConfirmPassword = "CorrectPw123";
        setupVm.Submit();
        SqliteConnection.ClearAllPools();

        // 誤ったパスワードで解除試行
        var unlockVm = CreateVm();
        unlockVm.Password = "WrongPassword!";

        bool result = unlockVm.Submit();

        Assert.False(result);
        Assert.Contains("正しくありません", unlockVm.ErrorMessage);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // ErrorMessage / IsErrorMessageVisible 連動
    // ──────────────────────────────────────────────────────────────────────────

    [Fact(DisplayName = "ErrorMessage: エラーメッセージ設定時にIsErrorMessageVisibleがtrueになること")]
    public void ErrorMessage_WhenSet_ShouldMakeIsErrorMessageVisibleTrue()
    {
        var vm = CreateVm();
        vm.Password = string.Empty;

        vm.Submit();

        Assert.True(vm.IsErrorMessageVisible);
    }

    [Fact(DisplayName = "ErrorMessage: エラー解除時にIsErrorMessageVisibleがfalseになること")]
    public void ErrorMessage_WhenCleared_ShouldMakeIsErrorMessageVisibleFalse()
    {
        var vm = CreateVm();
        vm.Password = string.Empty;
        vm.Submit(); // エラー表示

        vm.Password = "ValidPass123";
        vm.ConfirmPassword = "ValidPass123";
        vm.Submit(); // 成功 → エラークリア

        Assert.False(vm.IsErrorMessageVisible);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // IsSetupMode 切り替え時の ClearInputs 副作用
    // ──────────────────────────────────────────────────────────────────────────

    [Fact(DisplayName = "IsSetupMode: モード変更時に入力値とエラーメッセージがクリアされること")]
    public void IsSetupMode_WhenChanged_ShouldClearPasswordAndError()
    {
        var vm = CreateVm();
        vm.Password = "SomePass";
        vm.ConfirmPassword = "OtherPass";
        vm.Submit(); // エラー発生

        vm.IsSetupMode = false; // 切り替えでClearInputs呼ばれる

        Assert.Equal(string.Empty, vm.Password);
        Assert.Equal(string.Empty, vm.ErrorMessage);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // プロパティ変更通知
    // ──────────────────────────────────────────────────────────────────────────

    [Fact(DisplayName = "Password: 変更時にPropertyChangedイベントが発火すること")]
    public void Password_WhenSet_ShouldRaisePropertyChanged()
    {
        var vm = CreateVm();
        bool fired = false;
        vm.PropertyChanged += (s, e) => { if (e.PropertyName == nameof(vm.Password)) fired = true; };

        vm.Password = "NewPass";

        Assert.True(fired);
    }

    [Fact(DisplayName = "IsSetupMode: 変更時にTitleText/SubtitleTextのPropertyChangedイベントが発火すること")]
    public void IsSetupMode_WhenChanged_ShouldRaiseTitleTextAndSubtitleTextChanged()
    {
        var vm = CreateVm();
        bool titleFired = false, subtitleFired = false;
        vm.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(vm.TitleText)) titleFired = true;
            if (e.PropertyName == nameof(vm.SubtitleText)) subtitleFired = true;
        };

        vm.IsSetupMode = false;

        Assert.True(titleFired);
        Assert.True(subtitleFired);
    }
}
