using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;
using KeyValues.Extensions;
using KeyValues.Models;
using KeyValues.Providers;
using KeyValues.Repositories;
using KeyValues.Services;
using KeyValues.ViewModels;

namespace KeyValues.Views;

/// <summary>
/// MasterPasswordWindow.xaml の相互作用ロジック。
/// 画面用の ViewModel (MasterPasswordViewModel) を DataContext にセットし、UIイベントの橋渡しを行います。
/// </summary>
public partial class MasterPasswordWindow : Window
{
    private readonly MasterPasswordViewModel _viewModel;
    
    // パスワード表示/非表示の状態（WPFのPasswordBox固有のイベントで制御するため、残す）
    private bool _isPasswordVisible;
    private bool _isSetupPasswordVisible1;
    private bool _isSetupPasswordVisible2;

    /// <summary>
    /// 認証に成功または登録したマスターパスワードを取得します。
    /// </summary>
    public string MasterPassword => _viewModel.Password;

    /// <summary>
    /// ロードされたアカウントエントリーのリストを取得します。
    /// </summary>
    public List<AccountEntry> LoadedEntries { get; private set; } = new List<AccountEntry>();

    /// <summary>
    /// <see cref="MasterPasswordWindow"/> クラスの新しいインスタンスを初期化します。
    /// </summary>
    /// <param name="viewModel">使用する ViewModel（省略時は DI コンテナより自動注入）。</param>
    public MasterPasswordWindow(MasterPasswordViewModel? viewModel = null)
    {
        InitializeComponent();

        _viewModel = viewModel.ResolveOrDefault();
        DataContext = _viewModel;

        // 初期テーマアイコンの反映
        UpdateThemeIcon();

        // Windows Hello 対応チェック時に、WPFコントロールのTooltipなどをUIスレッドで微調整
        Loaded += (s, e) =>
        {
            if (!_viewModel.IsWindowsHelloSupported)
            {
                string notAvailableMsg = "このデバイスは Windows Hello 認証（顔/指紋/PIN）に対応していません。";
                SetupAutoLoginCheckBox.ToolTip = notAvailableMsg;
                SetupAutoLoginCheckBox.Content = "Windows Hello 自動ログイン (デバイス非対応)";
            }
            else
            {
                string saveMsg = "マスターパスワード設定完了時に自動ログイン情報が保存されます。";
                SetupAutoLoginCheckBox.ToolTip = saveMsg;
            }
        };
    }

    #region Theme Toggling
    private void ThemeToggleButton_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.IsDarkMode = !_viewModel.IsDarkMode;
        UpdateThemeIcon();
    }

    private void UpdateThemeIcon()
    {
        ThemeToggleIcon.Text = _viewModel.IsDarkMode ? "\uE706" : "\uE708"; // E706=Sun, E708=Moon
    }
    #endregion

    #region Password Visibility Control
    private void ToggleUnlockPasswordVisibility_Click(object sender, RoutedEventArgs e)
    {
        _isPasswordVisible = !_isPasswordVisible;
        if (_isPasswordVisible)
        {
            UnlockTextBox.Text = UnlockPasswordBox.Password;
            UnlockPasswordBox.Visibility = Visibility.Collapsed;
            UnlockTextBox.Visibility = Visibility.Visible;
            UnlockVisibilityIcon.Text = "\uEC3E";
            UnlockTextBox.Focus();
        }
        else
        {
            UnlockPasswordBox.Password = UnlockTextBox.Text;
            UnlockTextBox.Visibility = Visibility.Collapsed;
            UnlockPasswordBox.Visibility = Visibility.Visible;
            UnlockVisibilityIcon.Text = "\uE890";
            UnlockPasswordBox.Focus();
        }
    }

    private void ToggleSetupVisibility1_Click(object sender, RoutedEventArgs e)
    {
        _isSetupPasswordVisible1 = !_isSetupPasswordVisible1;
        if (_isSetupPasswordVisible1)
        {
            SetupTextBox1.Text = SetupPasswordBox1.Password;
            SetupPasswordBox1.Visibility = Visibility.Collapsed;
            SetupTextBox1.Visibility = Visibility.Visible;
            SetupVisibilityIcon1.Text = "\uEC3E";
            SetupTextBox1.Focus();
        }
        else
        {
            SetupPasswordBox1.Password = SetupTextBox1.Text;
            SetupTextBox1.Visibility = Visibility.Collapsed;
            SetupPasswordBox1.Visibility = Visibility.Visible;
            SetupVisibilityIcon1.Text = "\uE890";
            SetupPasswordBox1.Focus();
        }
    }

    private void ToggleSetupVisibility2_Click(object sender, RoutedEventArgs e)
    {
        _isSetupPasswordVisible2 = !_isSetupPasswordVisible2;
        if (_isSetupPasswordVisible2)
        {
            SetupTextBox2.Text = SetupPasswordBox2.Password;
            SetupPasswordBox2.Visibility = Visibility.Collapsed;
            SetupTextBox2.Visibility = Visibility.Visible;
            SetupVisibilityIcon2.Text = "\uEC3E";
            SetupTextBox2.Focus();
        }
        else
        {
            SetupPasswordBox2.Password = SetupTextBox2.Text;
            SetupTextBox2.Visibility = Visibility.Collapsed;
            SetupPasswordBox2.Visibility = Visibility.Visible;
            SetupVisibilityIcon2.Text = "\uE890";
            SetupPasswordBox2.Focus();
        }
    }
    #endregion

    #region Actions
    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void SubmitButton_Click(object sender, RoutedEventArgs e)
    {
        // パスワードボックスの値を同期して、ViewModelのPasswordプロパティに注入する
        if (_viewModel.IsSetupMode)
        {
            _viewModel.Password = _isSetupPasswordVisible1 ? SetupTextBox1.Text : SetupPasswordBox1.Password;
            _viewModel.ConfirmPassword = _isSetupPasswordVisible2 ? SetupTextBox2.Text : SetupPasswordBox2.Password;
        }
        else
        {
            _viewModel.Password = _isPasswordVisible ? UnlockTextBox.Text : UnlockPasswordBox.Password;
        }

        if (_viewModel.Submit())
        {
            // 認証またはセットアップに成功した場合、データをロードして終了
            try
            {
                // Appのストレージサービス参照を取得してロード
                var accountRepository = ((App)Application.Current).AccountRepository;
                LoadedEntries = accountRepository.Load(_viewModel.Password);

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                _viewModel.ErrorMessage = $"データの読み込みに失敗しました: {ex.Message}";
            }
        }
    }

    private void PasswordInput_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            SubmitButton_Click(this, new RoutedEventArgs());
            e.Handled = true;
        }
    }

    private void ShowHintLink_MouseDown(object sender, MouseButtonEventArgs e)
    {
        _viewModel.LoadPasswordHint();
    }

    private void ResetButton_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.ResetDatabase();
        if (_viewModel.IsSetupMode)
        {
            // 画面がリセットされ、セットアップモードに移行した場合、パスワードボックス等のUIの入力表示を物理的にクリア
            UnlockPasswordBox.Password = string.Empty;
            UnlockTextBox.Text = string.Empty;
            SetupPasswordBox1.Password = string.Empty;
            SetupTextBox1.Text = string.Empty;
            SetupPasswordBox2.Password = string.Empty;
            SetupTextBox2.Text = string.Empty;
        }
    }
    #endregion
}
