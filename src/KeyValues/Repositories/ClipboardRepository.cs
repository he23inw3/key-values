using System;
using System.Windows;
using System.Windows.Threading;

namespace KeyValues.Repositories;

/// <summary>
/// システムクリップボードへのデータ保存および指定秒数経過後の自動安全消去を管理するリポジトリ実装クラスです。
/// </summary>
public class ClipboardRepository
{
    private readonly DispatcherTimer _timer;
    private string? _lastCopiedValue;
    private readonly Action<string> _onClearedMessageCallback;

    /// <summary>
    /// <see cref="ClipboardRepository"/> の新しいインスタンスを初期化します。
    /// </summary>
    /// <param name="onClearedMessageCallback">クリップボード消去時にステータス通知メッセージを呼び出すためのコールバックアクション</param>
    public ClipboardRepository(Action<string> onClearedMessageCallback)
    {
        _onClearedMessageCallback = onClearedMessageCallback;
        _timer = new DispatcherTimer();
        _timer.Tick += Timer_Tick;
    }

    /// <summary>
    /// 指定された文字列をシステムクリップボードにコピーし、指定秒数経過後の自動消去タイマーを開始します。
    /// </summary>
    /// <param name="value">クリップボードに設定する文字列</param>
    /// <param name="clearDelaySeconds">自動消去を行うまでの秒数（0 以下の場合は自動消去を行いません）</param>
    /// <exception cref="InvalidOperationException">クリップボード操作に失敗した場合に発生します</exception>
    public void CopyToClipboard(string value, int clearDelaySeconds)
    {
        if (string.IsNullOrEmpty(value)) return;

        try
        {
            Clipboard.SetText(value);
            _lastCopiedValue = value;

            _timer.Stop();
            if (clearDelaySeconds > 0)
            {
                _timer.Interval = TimeSpan.FromSeconds(clearDelaySeconds);
                _timer.Start();
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"クリップボードへのコピーに失敗しました: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// 自動消去タイマーの発火イベントハンドラーです。
    /// コピーされた値が他アプリ等で上書きされていなければ、クリップボードを安全にクリアします。
    /// </summary>
    private void Timer_Tick(object? sender, EventArgs e)
    {
        _timer.Stop();
        try
        {
            if (Clipboard.ContainsText() && Clipboard.GetText() == _lastCopiedValue)
            {
                Clipboard.Clear();
                _onClearedMessageCallback?.Invoke("安全のためクリップボードのコピーデータを消去しました。");
            }
        }
        catch { }
        finally
        {
            _lastCopiedValue = null;
        }
    }

    /// <summary>
    /// タイマーを停止し、本アプリケーションでコピーしたクリップボードのデータを即座にクリアします。
    /// </summary>
    public void ClearImmediately()
    {
        _timer.Stop();
        try
        {
            if (_lastCopiedValue != null && Clipboard.ContainsText() && Clipboard.GetText() == _lastCopiedValue)
            {
                Clipboard.Clear();
            }
        }
        catch { }
        finally
        {
            _lastCopiedValue = null;
        }
    }
}
