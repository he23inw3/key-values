using System;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Windows;
using System.Windows.Input;
using KeyValues.Extensions;
using KeyValues.Models;
using KeyValues.Repositories;
using KeyValues.Services;
using Prism.Commands;
using Prism.Ioc;
using Prism.Mvvm;

using Microsoft.Extensions.DependencyInjection;

namespace KeyValues.ViewModels.Components;

/// <summary>
/// アカウント詳細情報および編集フォームの画面状態とコマンドを管理する ViewModel です。
/// </summary>
public class AccountDetailViewModel : BindableBase
{
    private readonly MainViewModel _mainViewModel;
    private readonly string _masterPassword;
    private readonly SqliteAccountRepository _accountRepository;
    private readonly ClipboardRepository _clipboardRepository;
    private readonly DelegateCommand _saveEntryCommand;

    private AccountEntry? _selectedEntry;
    private bool _isSelected;
    private bool _isEditMode;
    private bool _isNewEntryMode;
    private bool _isPasswordVisible;
    private AccountEntry _editBuffer = new AccountEntry();

    /// <summary>
    /// <see cref="AccountDetailViewModel"/> クラスの新しいインスタンスを初期化します。
    /// </summary>
    public AccountDetailViewModel(
        MainViewModel mainViewModel,
        string masterPassword,
        SqliteAccountRepository? accountRepository = null,
        Action<string, string>? showStatusCallback = null,
        ClipboardRepository? clipboardRepository = null,
        PasswordGeneratorService? passwordGeneratorService = null)
    {
        _mainViewModel = mainViewModel ?? throw new ArgumentNullException(nameof(mainViewModel));
        _masterPassword = masterPassword;
        _accountRepository = accountRepository.ResolveOrDefault();

        Action<string, string> statusCallback = showStatusCallback ?? ((msg, type) => { });
        _clipboardRepository = clipboardRepository.ResolveOrDefault(() => new ClipboardRepository(msg => statusCallback(msg, "Info")));

        var pwGenService = passwordGeneratorService.ResolveOrDefault();

        // 子であるパスワード生成器 ViewModel を初期化
        PasswordGenerator = new PasswordGeneratorViewModel(
            generatedPassword =>
            {
                EditBuffer.Password = generatedPassword;
                // バインディングソースの更新を強制通知
                RaisePropertyChanged(nameof(EditBuffer));
                _saveEntryCommand?.RaiseCanExecuteChanged();
            },
            statusCallback,
            pwGenService
        );

        EditEntryCommand = new DelegateCommand(ExecuteEditEntry, () => SelectedEntry != null).ObservesProperty(() => SelectedEntry);
        _saveEntryCommand = new DelegateCommand(ExecuteSaveEntry, CanExecuteSave);
        SaveEntryCommand = _saveEntryCommand;
        CancelEditCommand = new DelegateCommand(ExecuteCancelEdit);
        DeleteEntryCommand = new DelegateCommand(ExecuteDeleteEntry, () => SelectedEntry != null).ObservesProperty(() => SelectedEntry);
        CopyIdCommand = new DelegateCommand(ExecuteCopyId, () => SelectedEntry != null).ObservesProperty(() => SelectedEntry);
        CopyPasswordCommand = new DelegateCommand(ExecuteCopyPassword, () => SelectedEntry != null).ObservesProperty(() => SelectedEntry);
        TogglePasswordVisibilityCommand = new DelegateCommand(() => IsPasswordVisible = !IsPasswordVisible);
        OpenUrlCommand = new DelegateCommand(ExecuteOpenUrl, () => SelectedEntry != null && !string.IsNullOrWhiteSpace(SelectedEntry.Url)).ObservesProperty(() => SelectedEntry);
        OpenGeneratorCommand = new DelegateCommand(() => PasswordGenerator.IsGeneratorOpen = !PasswordGenerator.IsGeneratorOpen);

        if (_editBuffer != null)
        {
            _editBuffer.PropertyChanged += OnEditBufferPropertyChanged;
        }
    }

    #region Properties
    /// <summary>
    /// パスワード自動生成ツールの子 ViewModel を取得します。
    /// </summary>
    public PasswordGeneratorViewModel PasswordGenerator { get; }

    /// <summary>
    /// 詳細表示するアカウントエントリーを取得または設定します。
    /// </summary>
    public AccountEntry? SelectedEntry
    {
        get => _selectedEntry;
        set
        {
            if (SetProperty(ref _selectedEntry, value))
            {
                IsSelected = _selectedEntry != null;
                IsEditMode = false;
                IsPasswordVisible = false;

                if (_selectedEntry != null)
                {
                    // 編集バッファにクローンを作成
                    EditBuffer = new AccountEntry
                    {
                        Id = _selectedEntry.Id,
                        ServiceName = _selectedEntry.ServiceName,
                        LoginId = _selectedEntry.LoginId,
                        Password = _selectedEntry.Password,
                        Url = _selectedEntry.Url,
                        Memo = _selectedEntry.Memo,
                        UpdatedAt = _selectedEntry.UpdatedAt
                    };
                }
                CommandManager.InvalidateRequerySuggested();
            }
        }
    }

    /// <summary>
    /// アカウントが選択中かどうかを取得または設定します。
    /// </summary>
    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }

    /// <summary>
    /// 編集モードかどうかを取得または設定します。
    /// </summary>
    public bool IsEditMode
    {
        get => _isEditMode;
        set => SetProperty(ref _isEditMode, value);
    }

    /// <summary>
    /// パスワードが平文表示されているかどうかを取得または設定します。
    /// </summary>
    public bool IsPasswordVisible
    {
        get => _isPasswordVisible;
        set => SetProperty(ref _isPasswordVisible, value);
    }

    /// <summary>
    /// 編集時の一時データ保持バッファを取得または設定します。
    /// </summary>
    public AccountEntry EditBuffer
    {
        get => _editBuffer;
        set
        {
            var target = value ?? new AccountEntry();
            if (_editBuffer != null)
            {
                _editBuffer.PropertyChanged -= OnEditBufferPropertyChanged;
            }
            if (SetProperty(ref _editBuffer!, target))
            {
                _editBuffer.PropertyChanged += OnEditBufferPropertyChanged;
                _saveEntryCommand?.RaiseCanExecuteChanged();
            }
        }
    }

    private void OnEditBufferPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        _saveEntryCommand?.RaiseCanExecuteChanged();
    }
    #endregion

    #region Commands
    public ICommand EditEntryCommand { get; }
    public ICommand SaveEntryCommand { get; }
    public ICommand CancelEditCommand { get; }
    public ICommand DeleteEntryCommand { get; }
    public ICommand CopyIdCommand { get; }
    public ICommand CopyPasswordCommand { get; }
    public ICommand TogglePasswordVisibilityCommand { get; }
    public ICommand OpenUrlCommand { get; }
    public ICommand OpenGeneratorCommand { get; }
    #endregion

    #region Actions
    /// <summary>
    /// 新規アカウント登録用の空フォームを表示します。
    /// </summary>
    public void StartAddEntry()
    {
        SelectedEntry = null;
        IsSelected = true;
        IsEditMode = true;
        _isNewEntryMode = true;
        EditBuffer = new AccountEntry();
    }

    private void ExecuteEditEntry()
    {
        if (SelectedEntry == null) return;
        IsEditMode = true;
        _isNewEntryMode = false;
    }

    private bool CanExecuteSave()
    {
        return !string.IsNullOrWhiteSpace(EditBuffer.ServiceName) && 
               !string.IsNullOrWhiteSpace(EditBuffer.LoginId) && 
               !string.IsNullOrWhiteSpace(EditBuffer.Password);
    }

    private void ExecuteSaveEntry()
    {
        try
        {
            if (_isNewEntryMode)
            {
                var newEntry = new AccountEntry
                {
                    Id = Guid.NewGuid().ToString(),
                    ServiceName = EditBuffer.ServiceName.Trim(),
                    LoginId = EditBuffer.LoginId.Trim(),
                    Password = EditBuffer.Password,
                    Url = EditBuffer.Url.Trim(),
                    Memo = EditBuffer.Memo,
                    UpdatedAt = DateTime.Now
                };

                _mainViewModel.AddNewEntry(newEntry);
                SelectedEntry = newEntry;
                _mainViewModel.ShowStatusMessage("新規アカウントを保存しました。");
            }
            else
            {
                if (SelectedEntry == null) return;

                SelectedEntry.ServiceName = EditBuffer.ServiceName.Trim();
                SelectedEntry.LoginId = EditBuffer.LoginId.Trim();
                SelectedEntry.Password = EditBuffer.Password;
                SelectedEntry.Url = EditBuffer.Url.Trim();
                SelectedEntry.Memo = EditBuffer.Memo;
                SelectedEntry.UpdatedAt = DateTime.Now;

                _mainViewModel.RefreshList();

                // バインディング反映用に一度 null にしてから再割り当て
                var temp = SelectedEntry;
                SelectedEntry = null;
                SelectedEntry = temp;

                _mainViewModel.ShowStatusMessage("変更を保存しました。");
            }

            _mainViewModel.SaveDatabase();
            IsEditMode = false;
            _isNewEntryMode = false;
        }
        catch (Exception ex)
        {
            _mainViewModel.ShowStatusMessage($"保存に失敗しました: {ex.Message}", "Error");
        }
    }

    private void ExecuteCancelEdit()
    {
        IsEditMode = false;
        if (_isNewEntryMode)
        {
            IsSelected = false;
            _isNewEntryMode = false;
            _mainViewModel.CancelAddEntry();
        }
        else if (SelectedEntry != null)
        {
            EditBuffer = new AccountEntry
            {
                Id = SelectedEntry.Id,
                ServiceName = SelectedEntry.ServiceName,
                LoginId = SelectedEntry.LoginId,
                Password = SelectedEntry.Password,
                Url = SelectedEntry.Url,
                Memo = SelectedEntry.Memo,
                UpdatedAt = SelectedEntry.UpdatedAt
            };
        }
    }

    private void ExecuteDeleteEntry()
    {
        if (SelectedEntry == null) return;

        var result = MessageBox.Show(
            $"「{SelectedEntry.ServiceName}」のアカウント情報を削除しますか？\nこの操作は取り消せません。",
            "削除の確認",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result == MessageBoxResult.Yes)
        {
            try
            {
                _mainViewModel.RemoveEntry(SelectedEntry);
                SelectedEntry = null;
                _mainViewModel.SaveDatabase();
                _mainViewModel.ShowStatusMessage("アカウント情報を削除しました。");
            }
            catch (Exception ex)
            {
                _mainViewModel.ShowStatusMessage($"削除に失敗しました: {ex.Message}", "Error");
            }
        }
    }

    private void ExecuteCopyId()
    {
        if (SelectedEntry == null) return;
        try
        {
            _clipboardRepository.CopyToClipboard(SelectedEntry.LoginId, 0); // コピー自動クリアなし
            _mainViewModel.ShowStatusMessage("ログインIDをコピーしました。");
        }
        catch (Exception ex)
        {
            _mainViewModel.ShowStatusMessage($"コピーに失敗しました: {ex.Message}", "Error");
        }
    }

    private void ExecuteCopyPassword()
    {
        if (SelectedEntry == null) return;
        try
        {
            _clipboardRepository.CopyToClipboard(SelectedEntry.Password, 20); // 20秒後自動消去
            _mainViewModel.ShowStatusMessage("パスワードをコピーしました（20秒後にクリップボードはクリアされます）。");
        }
        catch (Exception ex)
        {
            _mainViewModel.ShowStatusMessage($"コピーに失敗しました: {ex.Message}", "Error");
        }
    }

    private void ExecuteOpenUrl()
    {
        if (SelectedEntry == null || string.IsNullOrWhiteSpace(SelectedEntry.Url)) return;

        try
        {
            string url = SelectedEntry.Url;
            if (!url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) && 
                !url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                url = "https://" + url;
            }

            Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });
        }
        catch (Exception ex)
        {
            _mainViewModel.ShowStatusMessage($"URLを開くことができませんでした: {ex.Message}", "Error");
        }
    }

    /// <summary>
    /// クリップボード自動消去タイマーを即時停止し、クリップボードをクリアします。
    /// </summary>
    public void Cleanup()
    {
        _clipboardRepository.ClearImmediately();
    }
    #endregion
}
