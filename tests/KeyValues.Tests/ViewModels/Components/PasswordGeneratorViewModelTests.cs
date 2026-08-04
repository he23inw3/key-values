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
/// PasswordGeneratorViewModel のコマンド実行・プロパティ変更通知を検証するテストクラスです。
/// </summary>
public class PasswordGeneratorViewModelTests : IDisposable
{
    private readonly string _dbPath;
    private readonly SqliteAccountRepository _repository;
    private readonly MainViewModel _mainVm;
    private readonly PasswordGeneratorViewModel _vm;
    private string _appliedPassword = string.Empty;
    private readonly List<string> _statusMessages = new();

    public PasswordGeneratorViewModelTests()
    {
        _dbPath = $"test-pwgen-vm-{Guid.NewGuid():N}.db";
        SqliteConnection.ClearAllPools();
        _repository = new SqliteAccountRepository(_dbPath);
        _mainVm = new MainViewModel("test_password", new List<AccountEntry>(), _repository);
        _vm = _mainVm.DetailViewModel.PasswordGenerator;
    }

    public void Dispose()
    {
        _mainVm.Cleanup();
        SqliteConnection.ClearAllPools();
        if (File.Exists(_dbPath)) File.Delete(_dbPath);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // GeneratePasswordCommand
    // ──────────────────────────────────────────────────────────────────────────

    [Fact(DisplayName = "GeneratePasswordCommand: 生成されたパスワードがGeneratedPasswordに設定されること")]
    public void GeneratePasswordCommand_ShouldPopulateGeneratedPassword()
    {
        _vm.GenLength = 16;
        _vm.GenUseUppercase = true;
        _vm.GenUseLowercase = true;
        _vm.GenUseDigits = true;
        _vm.GenUseSymbols = false;

        _vm.GeneratePasswordCommand.Execute(null);

        Assert.Equal(16, _vm.GeneratedPassword.Length);
    }

    [Fact(DisplayName = "GeneratePasswordCommand: 全フラグOFF時に空文字列が生成されること")]
    public void GeneratePasswordCommand_WithAllFlagsOff_ShouldSetEmpty()
    {
        _vm.GenUseUppercase = false;
        _vm.GenUseLowercase = false;
        _vm.GenUseDigits = false;
        _vm.GenUseSymbols = false;

        _vm.GeneratePasswordCommand.Execute(null);

        Assert.Equal(string.Empty, _vm.GeneratedPassword);
    }

    [Fact(DisplayName = "GeneratePasswordCommand: 指定された長さに応じたパスワードが生成されること")]
    public void GeneratePasswordCommand_WithDifferentLengths_ShouldRespectLength()
    {
        _vm.GenUseUppercase = true;
        _vm.GenUseLowercase = true;
        _vm.GenUseDigits = true;
        _vm.GenUseSymbols = false;

        foreach (int len in new[] { 8, 16, 32 })
        {
            _vm.GenLength = len;
            _vm.GeneratePasswordCommand.Execute(null);
            Assert.Equal(len, _vm.GeneratedPassword.Length);
        }
    }

    // ──────────────────────────────────────────────────────────────────────────
    // IsGeneratorOpen
    // ──────────────────────────────────────────────────────────────────────────

    [Fact(DisplayName = "IsGeneratorOpen: 初期値がfalseであること")]
    public void IsGeneratorOpen_DefaultShouldBeFalse()
    {
        Assert.False(_vm.IsGeneratorOpen);
    }

    [Fact(DisplayName = "IsGeneratorOpen: 変更時にPropertyChangedイベントが発火すること")]
    public void IsGeneratorOpen_Set_ShouldRaisePropertyChanged()
    {
        bool fired = false;
        _vm.PropertyChanged += (s, e) => { if (e.PropertyName == nameof(_vm.IsGeneratorOpen)) fired = true; };

        _vm.IsGeneratorOpen = true;

        Assert.True(fired);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // ApplyGeneratedPasswordCommand
    // ──────────────────────────────────────────────────────────────────────────

    [Fact(DisplayName = "ApplyGeneratedPasswordCommand: 適用実行後にパネルが閉じること")]
    public void ApplyGeneratedPasswordCommand_AfterGenerate_ShouldClosePanel()
    {
        _vm.GenLength = 12;
        _vm.GenUseUppercase = true;
        _vm.GenUseLowercase = true;
        _vm.GeneratePasswordCommand.Execute(null);

        _vm.IsGeneratorOpen = true;
        _vm.ApplyGeneratedPasswordCommand.Execute(null);

        Assert.False(_vm.IsGeneratorOpen);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // プロパティ変更通知
    // ──────────────────────────────────────────────────────────────────────────

    [Theory(DisplayName = "プロパティ変更通知: 生成オプション変更時にPropertyChangedイベントが発火すること")]
    [InlineData(nameof(PasswordGeneratorViewModel.GenLength))]
    [InlineData(nameof(PasswordGeneratorViewModel.GenUseUppercase))]
    [InlineData(nameof(PasswordGeneratorViewModel.GenUseLowercase))]
    [InlineData(nameof(PasswordGeneratorViewModel.GenUseDigits))]
    [InlineData(nameof(PasswordGeneratorViewModel.GenUseSymbols))]
    public void Properties_WhenChanged_ShouldRaisePropertyChanged(string propName)
    {
        bool fired = false;
        _vm.PropertyChanged += (s, e) => { if (e.PropertyName == propName) fired = true; };

        switch (propName)
        {
            case nameof(PasswordGeneratorViewModel.GenLength):      _vm.GenLength = 20; break;
            case nameof(PasswordGeneratorViewModel.GenUseUppercase): _vm.GenUseUppercase = false; break;
            case nameof(PasswordGeneratorViewModel.GenUseLowercase): _vm.GenUseLowercase = false; break;
            case nameof(PasswordGeneratorViewModel.GenUseDigits):    _vm.GenUseDigits = false; break;
            case nameof(PasswordGeneratorViewModel.GenUseSymbols):   _vm.GenUseSymbols = false; break;
        }

        Assert.True(fired);
    }
}
