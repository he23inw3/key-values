using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using KeyValues.Models;
using KeyValues.Providers;
using KeyValues.Repositories;
using KeyValues.Services;
using KeyValues.ViewModels;

namespace KeyValues.Views;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;

    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();

        _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        DataContext = _viewModel;

        // ウィンドウを閉じる時のクリーンアップ処理
        Closed += MainWindow_Closed;
    }

    public MainWindow(string masterPassword, List<AccountEntry> initialEntries, SqliteAccountRepository? accountRepository = null)
        : this(new MainViewModel(masterPassword, initialEntries, accountRepository))
    {
    }

    private void MainWindow_Closed(object? sender, EventArgs e)
    {
        // メモリ上の機密データをクリア
        _viewModel.Cleanup();
    }
}
