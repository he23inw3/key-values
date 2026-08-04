using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Security.Cryptography;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using KeyValues.Extensions;
using KeyValues.Models;
using KeyValues.Providers;
using KeyValues.Repositories;
using KeyValues.Services;
using Microsoft.Win32;
using Prism.Commands;
using Prism.Ioc;
using Prism.Mvvm;
using KeyValues.ViewModels.Components;

using Microsoft.Extensions.DependencyInjection;

namespace KeyValues.ViewModels;

/// <summary>
/// メイン画面（MainWindow）の全体状態を管轄し、子 ViewModel の調停（オーケストレーション）および上部操作を担当する親 ViewModel です。
/// </summary>
public class MainViewModel : BindableBase
{
    private readonly string _masterPassword;
    private readonly SqliteAccountRepository _accountRepository;
    private readonly CsvAccountRepository _csvAccountRepository;

    private readonly ObservableCollection<AccountEntry> _entries;

    // ステータス通知管理
    private string _statusMessage = string.Empty;
    private string _statusType = "Success"; // Success, Error, Info
    private DispatcherTimer? _statusTimer;

    // テーマ情報
    private bool _isDarkMode = App.IsDarkMode;

    // Windows Hello 自動ログイン情報
    private bool _isAutoLoginEnabled;
    private bool _isWindowsHelloSupported;

    private readonly WindowsHelloProvider _windowsHelloProvider;

    /// <summary>
    /// <see cref="MainViewModel"/> クラスの新しいインスタンスを初期化します。
    /// </summary>
    /// <param name="masterPassword">ログイン時のマスターパスワード</param>
    /// <param name="initialEntries">初期ロードされたアカウントエントリーのリスト</param>
    /// <param name="accountRepository">アカウントデータを操作する SQLite リポジトリ（省略時は DI コンテナより自動注入）</param>
    /// <param name="windowsHelloProvider">Windows Hello 生体認証プロバイダー（省略時は DI コンテナより自動注入）</param>
    /// <param name="csvAccountRepository">CSV アカウントインポート用リポジトリ（省略時は DI コンテナより自動注入）</param>
    public MainViewModel(
        string masterPassword,
        List<AccountEntry> initialEntries,
        SqliteAccountRepository? accountRepository = null,
        WindowsHelloProvider? windowsHelloProvider = null,
        CsvAccountRepository? csvAccountRepository = null)
    {
        _masterPassword = masterPassword;
        _accountRepository = accountRepository.ResolveOrDefault();
        _windowsHelloProvider = windowsHelloProvider.ResolveOrDefault(() => WindowsHelloProvider.Instance);
        _csvAccountRepository = csvAccountRepository.ResolveOrDefault();

        _entries = new ObservableCollection<AccountEntry>(initialEntries);

        // 子 ViewModel の初期化
        ListViewModel = new AccountListViewModel(this, _entries);
        DetailViewModel = new AccountDetailViewModel(this, _masterPassword, _accountRepository, ShowStatusMessage);

        // 自動ログイン設定の初期読み込みとサポートチェック
        _isAutoLoginEnabled = _accountRepository.IsAutoLoginEnabled();
        InitializeWindowsHelloSupport();

        // 上部ツールバー用コマンドの初期化 (Prism DelegateCommand)
        ImportCsvCommand = new DelegateCommand(ExecuteImportCsv);
        BackupCommand = new DelegateCommand(ExecuteBackup);
        RestoreCommand = new DelegateCommand(ExecuteRestore);
        ToggleThemeCommand = new DelegateCommand(ExecuteToggleTheme);
    }

    /// <summary>
    /// Windows Hello のサポート状況を非同期で検証し、非対応の場合は自動ログイン設定を安全に無効化します。
    /// </summary>
    private async void InitializeWindowsHelloSupport()
    {
        IsWindowsHelloSupported = await _windowsHelloProvider.IsAvailableAsync();
        if (!IsWindowsHelloSupported && IsAutoLoginEnabled)
        {
            IsAutoLoginEnabled = false;
        }
    }

    #region Properties
    /// <summary>
    /// 左側アカウントリスト用の子 ViewModel を取得します。
    /// </summary>
    public AccountListViewModel ListViewModel { get; }

    /// <summary>
    /// 右側詳細・編集パネル用の子 ViewModel を取得します。
    /// </summary>
    public AccountDetailViewModel DetailViewModel { get; }

    /// <summary>
    /// 画面下部に表示するステータス通知メッセージを取得または設定します。
    /// </summary>
    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    /// <summary>
    /// ステータス通知のメッセージ種別（"Success", "Error", "Info"）を取得または設定します。
    /// </summary>
    public string StatusType
    {
        get => _statusType;
        set => SetProperty(ref _statusType, value);
    }

    /// <summary>
    /// 現在ダークモードが有効かどうかを取得または設定します。
    /// </summary>
    public bool IsDarkMode
    {
        get => _isDarkMode;
        set => SetProperty(ref _isDarkMode, value);
    }

    /// <summary>
    /// Windows Hello による自動ログインが有効かどうかを取得または設定します。
    /// 値の変更時には自働ログイン資格情報の保存または消去を行います。
    /// </summary>
    public bool IsAutoLoginEnabled
    {
        get => _isAutoLoginEnabled;
        set
        {
            if (SetProperty(ref _isAutoLoginEnabled, value))
            {
                if (_isAutoLoginEnabled)
                {
                    _accountRepository.EnableAutoLogin(_masterPassword);
                    ShowStatusMessage("このPCでの Windows Hello 自動ログインを有効にしました。");
                }
                else
                {
                    _accountRepository.DisableAutoLogin();
                    ShowStatusMessage("自動ログインを無効にしました。次回から手動入力が必要です。");
                }
            }
        }
    }

    /// <summary>
    /// 現在の環境で Windows Hello 生体認証がサポートされているかどうかを取得または設定します。
    /// </summary>
    public bool IsWindowsHelloSupported
    {
        get => _isWindowsHelloSupported;
        set => SetProperty(ref _isWindowsHelloSupported, value);
    }
    #endregion

    #region Commands
    /// <summary>
    /// CSV ファイルからアカウント情報をインポートするコマンドを取得します。
    /// </summary>
    public ICommand ImportCsvCommand { get; }

    /// <summary>
    /// データベースをバックアップファイルとして保存するコマンドを取得します。
    /// </summary>
    public ICommand BackupCommand { get; }

    /// <summary>
    /// バックアップファイルからデータベースを復元するコマンドを取得します。
    /// </summary>
    public ICommand RestoreCommand { get; }

    /// <summary>
    /// ダークモードとライトモードのテーマを切り替えるコマンドを取得します。
    /// </summary>
    public ICommand ToggleThemeCommand { get; }
    #endregion

    #region Mediator Methods (子 ViewModel 間の通信調停)
    /// <summary>
    /// リスト側での選択変更を詳細側へ伝搬します。
    /// </summary>
    /// <param name="entry">選択されたアカウントエントリー（未選択時は null）</param>
    public void SelectedEntryChanged(AccountEntry? entry)
    {
        DetailViewModel.SelectedEntry = entry;
    }

    /// <summary>
    /// リスト側から新規追加が指示されたことを詳細側へ伝搬します。
    /// </summary>
    public void StartAddEntry()
    {
        DetailViewModel.StartAddEntry();
    }

    /// <summary>
    /// 新規追加がキャンセルされたことをリスト側に伝搬します。
    /// </summary>
    public void CancelAddEntry()
    {
        ListViewModel.ClearSelection();
    }

    /// <summary>
    /// 詳細側で保存された新しい項目をマスターコレクションに追加します。
    /// </summary>
    /// <param name="entry">追加する新しいアカウントエントリー</param>
    public void AddNewEntry(AccountEntry entry)
    {
        _entries.Add(entry);
    }

    /// <summary>
    /// 詳細側で削除された項目をマスターコレクションから削除します。
    /// </summary>
    /// <param name="entry">削除するアカウントエントリー</param>
    public void RemoveEntry(AccountEntry entry)
    {
        _entries.Remove(entry);
    }

    /// <summary>
    /// データベースへ変更を保存します。
    /// </summary>
    public void SaveDatabase()
    {
        _accountRepository.Save(new List<AccountEntry>(_entries), _masterPassword);
    }

    /// <summary>
    /// リストのビューをリフレッシュ（インクリメンタルサーチの再評価等）します。
    /// </summary>
    public void RefreshList()
    {
        ListViewModel.FilteredEntries.Refresh();
    }

    /// <summary>
    /// ステータス通知を表示します。
    /// </summary>
    /// <param name="message">表示する通知メッセージ</param>
    /// <param name="type">通知の種別（"Success", "Error", "Info"）</param>
    public void ShowStatusMessage(string message, string type = "Success")
    {
        StatusMessage = message;
        StatusType = type;

        if (_statusTimer == null)
        {
            _statusTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(4) };
            _statusTimer.Tick += (s, e) => { StatusMessage = string.Empty; _statusTimer.Stop(); };
        }
        else
        {
            _statusTimer.Stop();
        }
        _statusTimer.Start();
    }
    #endregion

    #region Actions
    /// <summary>
    /// CSV ファイルダイアログを表示し、アカウント情報のインポートを実行します。
    /// </summary>
    private void ExecuteImportCsv()
    {
        var openFileDialog = new OpenFileDialog
        {
            Filter = "CSVファイル (*.csv)|*.csv|すべてのファイル (*.*)|*.*",
            Title = "CSVファイルのインポート"
        };

        if (openFileDialog.ShowDialog() == true)
        {
            try
            {
                List<AccountEntry> imported = _csvAccountRepository.Import(openFileDialog.FileName);
                if (imported.Count == 0)
                {
                    ShowStatusMessage("取り込める有効なアカウント情報が見つかりませんでした。", "Info");
                    return;
                }

                foreach (var entry in imported)
                {
                    _entries.Add(entry);
                }

                SaveDatabase();
                ShowStatusMessage($"{imported.Count} 件のアカウント情報をインポートしました。");
            }
            catch (Exception ex)
            {
                ShowStatusMessage($"CSVインポートに失敗しました: {ex.Message}", "Error");
            }
        }
    }

    /// <summary>
    /// ファイル保存ダイアログを表示し、現在のデータベースのバックアップを作成します。
    /// </summary>
    private void ExecuteBackup()
    {
        var saveFileDialog = new SaveFileDialog
        {
            Filter = "KeyValues データベース (*.db)|*.db",
            FileName = "key-values_backup.db",
            Title = "データのバックアップ保存"
        };

        if (saveFileDialog.ShowDialog() == true)
        {
            try
            {
                _accountRepository.Backup(saveFileDialog.FileName);
                ShowStatusMessage("データのバックアップが正常に作成されました。");
            }
            catch (Exception ex)
            {
                ShowStatusMessage($"バックアップの作成に失敗しました: {ex.Message}", "Error");
            }
        }
    }

    /// <summary>
    /// ファイル選択ダイアログを表示し、バックアップファイルからの復元処理を実行します。
    /// </summary>
    private void ExecuteRestore()
    {
        var openFileDialog = new OpenFileDialog
        {
            Filter = "KeyValues データベース (*.db)|*.db",
            Title = "バックアップデータの読み込み（復元）"
        };

        if (openFileDialog.ShowDialog() == true)
        {
            var result = MessageBox.Show(
                "バックアップからデータを復元すると、現在登録されているすべてのデータが上書きされます。\nよろしいですか？",
                "データ復元の確認",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    _accountRepository.Restore(openFileDialog.FileName, _masterPassword);
                    
                    var loaded = _accountRepository.Load(_masterPassword);
                    _entries.Clear();
                    foreach (var entry in loaded) _entries.Add(entry);
                    
                    // 選択をクリア
                    ListViewModel.ClearSelection();

                    ShowStatusMessage("データを正常に復元しました。");
                }
                catch (CryptographicException)
                {
                    ShowStatusMessage("復元データのマスターパスワードが現在のパスワードと一致しません。復元できません。", "Error");
                }
                catch (Exception ex)
                {
                    ShowStatusMessage($"復元に失敗しました: {ex.Message}", "Error");
                }
            }
        }
    }

    /// <summary>
    /// アプリケーションのダークモード・ライトモードのテーマを反転切り替えします。
    /// </summary>
    private void ExecuteToggleTheme()
    {
        IsDarkMode = !IsDarkMode;
        App.ApplyTheme(IsDarkMode);
        App.SaveThemeSettings(IsDarkMode);
    }

    /// <summary>
    /// アプリ終了時などに、タイマーの停止やメモリの安全クリアを実行します。
    /// </summary>
    public void Cleanup()
    {
        _statusTimer?.Stop();
        DetailViewModel.Cleanup();

        if (_entries != null)
        {
            foreach (var entry in _entries)
            {
                entry.Password = string.Empty;
                entry.LoginId = string.Empty;
                entry.ServiceName = string.Empty;
                entry.Memo = string.Empty;
                entry.Url = string.Empty;
            }
            _entries.Clear();
        }
    }
    #endregion
}
