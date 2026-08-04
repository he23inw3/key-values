using System;
using System.IO;
using System.Security.Cryptography;
using System.Windows;
using KeyValues.Providers;
using KeyValues.Repositories;
using KeyValues.Services;
using Prism.Mvvm;

using Microsoft.Extensions.DependencyInjection;

using KeyValues.Extensions;

namespace KeyValues.ViewModels;

/// <summary>
/// ロック解除・新規セットアップ画面（MasterPasswordWindow）の画面状態とロジックを管理する ViewModel です。
/// </summary>
public class MasterPasswordViewModel : BindableBase
{
    private readonly SqliteAccountRepository _accountRepository;

    private bool _isSetupMode;
    private string _password = string.Empty;
    private string _confirmPassword = string.Empty;
    private string _passwordHint = string.Empty;
    
    private string _errorMessage = string.Empty;
    private bool _isErrorMessageVisible;

    private bool _isAutoLoginEnabled;
    private bool _isWindowsHelloSupported;

    private string _passwordHintText = string.Empty;
    private bool _isHintVisible;

    private bool _isDarkMode = App.IsDarkMode;

    private readonly WindowsHelloProvider _windowsHelloProvider;

    /// <summary>
    /// <see cref="MasterPasswordViewModel"/> クラスの新しいインスタンスを初期化します。
    /// </summary>
    public MasterPasswordViewModel(SqliteAccountRepository? accountRepository = null, WindowsHelloProvider? windowsHelloProvider = null)
    {
        _accountRepository = accountRepository.ResolveOrDefault();
        _windowsHelloProvider = windowsHelloProvider.ResolveOrDefault(() => WindowsHelloProvider.Instance);

        // データベースファイルの有無で、新規セットアップかロック解除かを初期判定
        IsSetupMode = !_accountRepository.FileExists();

        // Windows Hello のサポートチェック
        InitializeWindowsHelloSupport();
    }

    private async void InitializeWindowsHelloSupport()
    {
        IsWindowsHelloSupported = await _windowsHelloProvider.IsAvailableAsync();
        if (IsSetupMode)
        {
            // セットアップ時はデフォルトでチェックを入れておき、選択しやすくする
            IsAutoLoginEnabled = IsWindowsHelloSupported;
        }
        else
        {
            // ロック解除時は保存されている現在の設定を反映
            IsAutoLoginEnabled = _accountRepository.IsAutoLoginEnabled();
        }
    }

    #region Properties
    /// <summary>
    /// 新規セットアップモード（true）か、ロック解除モード（false）かを取得または設定します。
    /// </summary>
    public bool IsSetupMode
    {
        get => _isSetupMode;
        set
        {
            if (SetProperty(ref _isSetupMode, value))
            {
                RaisePropertyChanged(nameof(TitleText));
                RaisePropertyChanged(nameof(SubtitleText));
                RaisePropertyChanged(nameof(SubmitButtonText));
                ClearInputs();
            }
        }
    }

    /// <summary>
    /// 現在のモードに対応する送信ボタンのテキストを取得します。
    /// </summary>
    public string SubmitButtonText => IsSetupMode ? "作成" : "ロック解除";

    /// <summary>
    /// 現在のモードに対応するタイトルテキストを取得します。
    /// </summary>
    public string TitleText => IsSetupMode ? "KeyValues 初期設定" : "KeyValues ロック解除";

    /// <summary>
    /// 現在のモードに対応するサブタイトルテキストを取得します。
    /// </summary>
    public string SubtitleText => IsSetupMode 
        ? "安全なマスターパスワードを新規作成します。" 
        : "マスターパスワードを入力してください。";

    /// <summary>
    /// 入力されたパスワードを取得または設定します。
    /// </summary>
    public string Password
    {
        get => _password;
        set => SetProperty(ref _password, value);
    }

    /// <summary>
    /// 新規セットアップ時の確認用パスワードを取得または設定します。
    /// </summary>
    public string ConfirmPassword
    {
        get => _confirmPassword;
        set => SetProperty(ref _confirmPassword, value);
    }

    /// <summary>
    /// パスワードのヒント（入力バッファ）を取得または設定します。
    /// </summary>
    public string PasswordHint
    {
        get => _passwordHint;
        set => SetProperty(ref _passwordHint, value);
    }

    /// <summary>
    /// 画面に表示する警告またはエラーメッセージを取得または設定します。
    /// </summary>
    public string ErrorMessage
    {
        get => _errorMessage;
        set
        {
            if (SetProperty(ref _errorMessage, value))
            {
                IsErrorMessageVisible = !string.IsNullOrEmpty(_errorMessage);
            }
        }
    }

    /// <summary>
    /// エラーメッセージが表示中かどうかを取得または設定します。
    /// </summary>
    public bool IsErrorMessageVisible
    {
        get => _isErrorMessageVisible;
        private set => SetProperty(ref _isErrorMessageVisible, value);
    }

    /// <summary>
    /// 自動ログイン設定が有効かどうかを取得または設定します。
    /// </summary>
    public bool IsAutoLoginEnabled
    {
        get => _isAutoLoginEnabled;
        set => SetProperty(ref _isAutoLoginEnabled, value);
    }

    /// <summary>
    /// デバイスが Windows Hello (指紋・顔・PIN) に対応しているかどうかを取得または設定します。
    /// </summary>
    public bool IsWindowsHelloSupported
    {
        get => _isWindowsHelloSupported;
        set => SetProperty(ref _isWindowsHelloSupported, value);
    }

    /// <summary>
    /// 画面に表示するヒントテキスト（読み出し後）を取得または設定します。
    /// </summary>
    public string PasswordHintText
    {
        get => _passwordHintText;
        set => SetProperty(ref _passwordHintText, value);
    }

    /// <summary>
    /// パスワードヒントが表示中かどうかを取得または設定します。
    /// </summary>
    public bool IsHintVisible
    {
        get => _isHintVisible;
        set => SetProperty(ref _isHintVisible, value);
    }

    /// <summary>
    /// ダークモードが有効かどうかを取得または設定します。
    /// </summary>
    public bool IsDarkMode
    {
        get => _isDarkMode;
        set
        {
            if (SetProperty(ref _isDarkMode, value))
            {
                App.ApplyTheme(_isDarkMode);
                App.SaveThemeSettings(_isDarkMode);
            }
        }
    }
    #endregion

    #region Public Methods
    /// <summary>
    /// 現在入力されている内容に基づいて、マスターパスワードの検証（ロック解除）または新規登録（初期設定）を実行します。
    /// </summary>
    /// <returns>処理が正常完了した場合は true、入力チェックエラーや認証失敗などの場合は false。</returns>
    public bool Submit()
    {
        ErrorMessage = string.Empty;

        if (IsSetupMode)
        {
            // 新規セットアップ処理
            if (string.IsNullOrEmpty(Password))
            {
                ErrorMessage = "マスターパスワードを入力してください。";
                return false;
            }

            if (Password.Length < 8)
            {
                ErrorMessage = "セキュリティ保護のため、パスワードは8文字以上にしてください。";
                return false;
            }

            if (Password != ConfirmPassword)
            {
                ErrorMessage = "入力された確認用パスワードが一致しません。";
                return false;
            }

            try
            {
                // データベーステーブル初期化と暗号検証キーの登録
                _accountRepository.InitializeDatabase();
                _accountRepository.RegisterVerification(Password);

                // パスワードヒントの保存
                if (!string.IsNullOrWhiteSpace(PasswordHint))
                {
                    _accountRepository.SavePasswordHint(PasswordHint.Trim());
                }

                // Windows Hello 自動ログインの有効化
                if (IsAutoLoginEnabled)
                {
                    _accountRepository.EnableAutoLogin(Password);
                }
                else
                {
                    _accountRepository.DisableAutoLogin();
                }

                return true;
            }
            catch (Exception ex)
            {
                ErrorMessage = $"初期設定に失敗しました: {ex.Message}";
                return false;
            }
        }
        else
        {
            // ロック解除処理
            if (string.IsNullOrEmpty(Password))
            {
                ErrorMessage = "マスターパスワードを入力してください。";
                return false;
            }

            if (_accountRepository.VerifyMasterPassword(Password))
            {
                // 認証成功時、自動ログイン設定を更新
                if (IsAutoLoginEnabled)
                {
                    _accountRepository.EnableAutoLogin(Password);
                }
                else
                {
                    _accountRepository.DisableAutoLogin();
                }
                return true;
            }
            else
            {
                ErrorMessage = "マスターパスワードが正しくありません。";
                return false;
            }
        }
    }

    /// <summary>
    /// 登録されているパスワードのヒントを読み出し、メッセージボックスで表示します。
    /// </summary>
    public void LoadPasswordHint()
    {
        try
        {
            string hint = _accountRepository.GetPasswordHint();
            if (string.IsNullOrWhiteSpace(hint))
            {
                MessageBox.Show("パスワードのヒントは登録されていません。", "パスワードヒント", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show($"パスワードのヒント:\n\n{hint}", "パスワードヒント", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"ヒントの読み込みに失敗しました: {ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// データベースファイルを物理削除してリセットし、画面を新規セットアップモードへ再遷移させます。
    /// </summary>
    public void ResetDatabase()
    {
        var result = MessageBox.Show(
            "データベースファイルを削除し、すべての登録アカウント情報を消去して初期化します。\nこの操作は取り消せません。本当に行いますか？",
            "データベースの完全消去",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result == MessageBoxResult.Yes)
        {
            var finalConfirm = MessageBox.Show(
                "本当に消去しますか？（最終確認）",
                "データベースの完全消去（最終確認）",
                MessageBoxButton.YesNo,
                MessageBoxImage.Error);

            if (finalConfirm == MessageBoxResult.Yes)
            {
                try
                {
                    _accountRepository.ResetDatabase();
                    IsSetupMode = true;
                    ErrorMessage = "データベースを消去しました。新しいパスワードを設定してください。";
                }
                catch (Exception ex)
                {
                    ErrorMessage = $"消去に失敗しました: {ex.Message}";
                }
            }
        }
    }
    #endregion

    #region Helper Methods
    private void ClearInputs()
    {
        Password = string.Empty;
        ConfirmPassword = string.Empty;
        PasswordHint = string.Empty;
        ErrorMessage = string.Empty;
        PasswordHintText = string.Empty;
        IsHintVisible = false;
    }
    #endregion
}
