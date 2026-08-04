using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using KeyValues.Providers;
using KeyValues.Repositories;
using KeyValues.Services;
using KeyValues.Views;
using Prism.DryIoc;
using Prism.Ioc;

namespace KeyValues;

/// <summary>
/// アプリケーションのエントリーポイントおよび DI コンテナのセットアップクラスです。
/// </summary>
public partial class App : PrismApplication
{
    private const string SettingsFileName = "data/settings.cfg";
    public static bool IsDarkMode { get; private set; } = true;

    /// <summary>
    /// Prism の ContainerLocator を介して解決する互換用プロパティです。
    /// </summary>
    public static IContainerProvider Services => ContainerLocator.Container;

    /// <summary>
    /// Prism コンテナから SqliteAccountRepository を取得します。
    /// </summary>
    public SqliteAccountRepository AccountRepository => Container.Resolve<SqliteAccountRepository>();

    /// <inheritdoc />
    protected override void RegisterTypes(IContainerRegistry containerRegistry)
    {
        // サービス・リポジトリ・プロバイダーの DI 登録 (Prism IContainerRegistry)
        containerRegistry.RegisterSingleton<CryptoService>();
        containerRegistry.RegisterSingleton<SqliteAccountRepository>();
        containerRegistry.RegisterSingleton<CsvAccountRepository>();
        containerRegistry.RegisterInstance<WindowsHelloProvider>(WindowsHelloProvider.Instance);
        containerRegistry.RegisterSingleton<PasswordGeneratorService>();
        containerRegistry.RegisterSingleton<ClipboardRepository>();
    }

    /// <inheritdoc />
    protected override Window CreateShell()
    {
        // 設定ファイルからテーマの読み込み
        LoadThemeSettings();
        ApplyTheme(IsDarkMode);

        var accountRepository = Container.Resolve<SqliteAccountRepository>();
        var winHello = Container.Resolve<WindowsHelloProvider>();

        // 1. 自動ログイン (Windows Hello) のチェック
        if (accountRepository.IsAutoLoginEnabled())
        {
            bool isAvailable = Task.Run(async () => await winHello.IsAvailableAsync()).GetAwaiter().GetResult();
            if (isAvailable)
            {
                bool verified = Task.Run(async () => await winHello.RequestVerificationAsync("KeyValues ロック解除のための本人確認を行います。")).GetAwaiter().GetResult();
                if (verified)
                {
                    string masterPassword = accountRepository.LoadAutoLoginPassword();
                    if (!string.IsNullOrEmpty(masterPassword))
                    {
                        try
                        {
                            var entries = accountRepository.Load(masterPassword);
                            return new MainWindow(masterPassword, entries);
                        }
                        catch
                        {
                            // 復号エラーなどが発生した場合は自動で手動ログイン画面へフォールバック
                        }
                    }
                }
            }
        }

        // 2. 手動ログイン（フォールバック）
        var masterPasswordWindow = Container.Resolve<MasterPasswordWindow>();
        bool? dialogResult = masterPasswordWindow.ShowDialog();

        if (dialogResult == true)
        {
            string masterPassword = masterPasswordWindow.MasterPassword;
            return new MainWindow(masterPassword, masterPasswordWindow.LoadedEntries);
        }

        return null!;
    }

    /// <inheritdoc />
    protected override void InitializeShell(Window shell)
    {
        if (shell != null)
        {
            base.InitializeShell(shell);
            shell.Show();
        }
        else
        {
            Shutdown();
        }
    }

    private void Application_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
    {
        MessageBox.Show(
            $"予期しないエラーが発生しました:\n{e.Exception.Message}\n\n【詳細】\n{e.Exception}",
            "KeyValues - 重大のエラー",
            MessageBoxButton.OK,
            MessageBoxImage.Error);
        e.Handled = true;
        Shutdown();
    }

    /// <summary>
    /// テーマ設定ファイルを読み込みます。
    /// </summary>
    private void LoadThemeSettings()
    {
        try
        {
            string settingsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, SettingsFileName);
            if (File.Exists(settingsPath))
            {
                string content = File.ReadAllText(settingsPath).Trim();
                if (content.Equals("theme=light", StringComparison.OrdinalIgnoreCase))
                {
                    IsDarkMode = false;
                    return;
                }
            }
        }
        catch
        {
            // 読み込み失敗時はデフォルトのダークテーマとする
        }
        IsDarkMode = true;
    }

    /// <summary>
    /// テーマ設定をファイルに保存します。
    /// </summary>
    public static void SaveThemeSettings(bool isDark)
    {
        try
        {
            string settingsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data");
            if (!Directory.Exists(settingsDir))
            {
                Directory.CreateDirectory(settingsDir);
            }

            string settingsPath = Path.Combine(settingsDir, "settings.cfg");
            File.WriteAllText(settingsPath, isDark ? "theme=dark" : "theme=light");
        }
        catch
        {
            // 保存失敗時は何もしない（ポータブル動作への影響を避ける）
        }
    }

    /// <summary>
    /// アプリ全体のテーマ（カラーブラシ）を動的に適用します。
    /// </summary>
    public static void ApplyTheme(bool isDark)
    {
        IsDarkMode = isDark;
        var resources = Current.Resources;

        if (isDark)
        {
            resources["WindowBackgroundBrush"] = new SolidColorBrush(Color.FromRgb(0x12, 0x12, 0x12));
            resources["CardBackgroundBrush"] = new SolidColorBrush(Color.FromRgb(0x1E, 0x1E, 0x1E));
            resources["InputBackgroundBrush"] = new SolidColorBrush(Color.FromRgb(0x2D, 0x2D, 0x2D));
            resources["InputForegroundBrush"] = new SolidColorBrush(Color.FromRgb(0xFF, 0xFF, 0xFF));
            resources["BorderBrush"] = new SolidColorBrush(Color.FromRgb(0x33, 0x33, 0x33));
            resources["TextPrimaryBrush"] = new SolidColorBrush(Color.FromRgb(0xFF, 0xFF, 0xFF));
            resources["TextSecondaryBrush"] = new SolidColorBrush(Color.FromRgb(0xA0, 0xA0, 0xA0));
            resources["AccentBrush"] = new SolidColorBrush(Color.FromRgb(0x2F, 0x80, 0xED));
            resources["AccentHoverBrush"] = new SolidColorBrush(Color.FromRgb(0x1F, 0x69, 0xC7));
        }
        else
        {
            resources["WindowBackgroundBrush"] = new SolidColorBrush(Color.FromRgb(0xF4, 0xF5, 0xF7));
            resources["CardBackgroundBrush"] = new SolidColorBrush(Color.FromRgb(0xFF, 0xFF, 0xFF));
            resources["InputBackgroundBrush"] = new SolidColorBrush(Color.FromRgb(0xEE, 0xF0, 0xF3));
            resources["InputForegroundBrush"] = new SolidColorBrush(Color.FromRgb(0x1E, 0x1E, 0x1E));
            resources["BorderBrush"] = new SolidColorBrush(Color.FromRgb(0xDF, 0xE2, 0xE6));
            resources["TextPrimaryBrush"] = new SolidColorBrush(Color.FromRgb(0x1E, 0x1E, 0x1E));
            resources["TextSecondaryBrush"] = new SolidColorBrush(Color.FromRgb(0x7A, 0x86, 0x9A));
            resources["AccentBrush"] = new SolidColorBrush(Color.FromRgb(0x00, 0x52, 0xCC));
            resources["AccentHoverBrush"] = new SolidColorBrush(Color.FromRgb(0x00, 0x65, 0xFF));
        }
    }
}
