using System;
using System.Windows.Input;
using KeyValues.Extensions;
using KeyValues.Services;
using Prism.Commands;
using Prism.Ioc;
using Prism.Mvvm;

using Microsoft.Extensions.DependencyInjection;

namespace KeyValues.ViewModels.Components;

/// <summary>
/// パスワード自動生成機能の画面状態とコマンドを管理する ViewModel です。
/// </summary>
public class PasswordGeneratorViewModel : BindableBase
{
    private readonly Action<string> _onApplyPasswordCallback;
    private readonly Action<string, string> _showStatusCallback;
    private readonly PasswordGeneratorService _passwordGeneratorService;

    private bool _isGeneratorOpen;
    private string _generatedPassword = string.Empty;
    private int _genLength = 16;
    private bool _genUseUppercase = true;
    private bool _genUseLowercase = true;
    private bool _genUseDigits = true;
    private bool _genUseSymbols = true;

    /// <summary>
    /// <see cref="PasswordGeneratorViewModel"/> クラスの新しいインスタンスを初期化します。
    /// </summary>
    public PasswordGeneratorViewModel(Action<string> onApplyPasswordCallback, Action<string, string> showStatusCallback, PasswordGeneratorService? passwordGeneratorService = null)
    {
        _onApplyPasswordCallback = onApplyPasswordCallback;
        _showStatusCallback = showStatusCallback;
        _passwordGeneratorService = passwordGeneratorService.ResolveOrDefault();

        GeneratePasswordCommand = new DelegateCommand(ExecuteGeneratePassword);
        ApplyGeneratedPasswordCommand = new DelegateCommand(ExecuteApplyGeneratedPassword, () => !string.IsNullOrEmpty(GeneratedPassword))
            .ObservesProperty(() => GeneratedPassword);
    }

    #region Properties
    /// <summary>
    /// パスワード生成パネルが開いているかどうかを取得または設定します。
    /// </summary>
    public bool IsGeneratorOpen
    {
        get => _isGeneratorOpen;
        set => SetProperty(ref _isGeneratorOpen, value);
    }

    /// <summary>
    /// 生成されたパスワードを取得または設定します。
    /// </summary>
    public string GeneratedPassword
    {
        get => _generatedPassword;
        set => SetProperty(ref _generatedPassword, value);
    }

    /// <summary>
    /// 生成するパスワードの長さを取得または設定します。
    /// </summary>
    public int GenLength
    {
        get => _genLength;
        set => SetProperty(ref _genLength, value);
    }

    /// <summary>
    /// 英大文字を使用するかどうかを取得または設定します。
    /// </summary>
    public bool GenUseUppercase
    {
        get => _genUseUppercase;
        set => SetProperty(ref _genUseUppercase, value);
    }

    /// <summary>
    /// 英小文字を使用するかどうかを取得または設定します。
    /// </summary>
    public bool GenUseLowercase
    {
        get => _genUseLowercase;
        set => SetProperty(ref _genUseLowercase, value);
    }

    /// <summary>
    /// 数字を使用するかどうかを取得または設定します。
    /// </summary>
    public bool GenUseDigits
    {
        get => _genUseDigits;
        set => SetProperty(ref _genUseDigits, value);
    }

    /// <summary>
    /// 記号を使用するかどうかを取得または設定します。
    /// </summary>
    public bool GenUseSymbols
    {
        get => _genUseSymbols;
        set => SetProperty(ref _genUseSymbols, value);
    }
    #endregion

    #region Commands
    public ICommand GeneratePasswordCommand { get; }
    public ICommand ApplyGeneratedPasswordCommand { get; }
    #endregion

    #region Actions
    private void ExecuteGeneratePassword()
    {
        GeneratedPassword = _passwordGeneratorService.Generate(GenLength, GenUseUppercase, GenUseLowercase, GenUseDigits, GenUseSymbols);
        if (string.IsNullOrEmpty(GeneratedPassword))
        {
            _showStatusCallback?.Invoke("パスワードに使用する文字種を少なくとも1つ選択してください。", "Error");
        }
    }

    private void ExecuteApplyGeneratedPassword()
    {
        _onApplyPasswordCallback?.Invoke(GeneratedPassword);
        IsGeneratorOpen = false;
        _showStatusCallback?.Invoke("生成されたパスワードを適用しました。", "Success");
    }
    #endregion
}
