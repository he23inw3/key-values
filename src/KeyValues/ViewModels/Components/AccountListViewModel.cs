using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;
using System.Windows.Input;
using KeyValues.Models;
using Prism.Commands;
using Prism.Mvvm;

namespace KeyValues.ViewModels.Components;

/// <summary>
/// アカウント一覧リストおよび検索検索ボックスの画面状態とコマンドを管理する ViewModel です。
/// </summary>
public class AccountListViewModel : BindableBase
{
    private readonly MainViewModel _mainViewModel;
    private readonly ObservableCollection<AccountEntry> _entries;
    private readonly ICollectionView _filteredEntries;
    private string _searchText = string.Empty;
    private AccountEntry? _selectedEntry;

    /// <summary>
    /// <see cref="AccountListViewModel"/> クラスの新しいインスタンスを初期化します。
    /// </summary>
    public AccountListViewModel(MainViewModel mainViewModel, ObservableCollection<AccountEntry> entries)
    {
        _mainViewModel = mainViewModel ?? throw new ArgumentNullException(nameof(mainViewModel));
        _entries = entries ?? throw new ArgumentNullException(nameof(entries));
        _filteredEntries = CollectionViewSource.GetDefaultView(_entries);
        _filteredEntries.Filter = FilterEntries;

        AddEntryCommand = new DelegateCommand(ExecuteAddEntry);
    }

    #region Properties
    /// <summary>
    /// 検索条件で絞り込まれたアカウントエントリーのビューコレクションを取得します。
    /// </summary>
    public ICollectionView FilteredEntries => _filteredEntries;

    /// <summary>
    /// 検索キーワードを取得または設定します。
    /// </summary>
    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
            {
                _filteredEntries.Refresh();
            }
        }
    }

    /// <summary>
    /// リストで選択されているアカウントエントリーを取得または設定します。
    /// </summary>
    public AccountEntry? SelectedEntry
    {
        get => _selectedEntry;
        set
        {
            if (SetProperty(ref _selectedEntry, value))
            {
                // 親 ViewModel (MainViewModel) を介して詳細パネル側に変更を通知
                _mainViewModel.SelectedEntryChanged(_selectedEntry);
            }
        }
    }
    #endregion

    #region Commands
    public ICommand AddEntryCommand { get; }
    #endregion

    #region Actions
    private void ExecuteAddEntry()
    {
        SelectedEntry = null; // 選択をクリア
        _mainViewModel.StartAddEntry();
    }

    private bool FilterEntries(object item)
    {
        if (string.IsNullOrWhiteSpace(SearchText)) return true;
        var entry = item as AccountEntry;
        if (entry == null) return false;

        return entry.ServiceName.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
               entry.LoginId.Contains(SearchText, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// リストの選択を解除します。
    /// </summary>
    public void ClearSelection()
    {
        SelectedEntry = null;
    }
    #endregion
}
