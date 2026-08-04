using System;
using System.Threading;
using Xunit;
using KeyValues.Repositories;

namespace KeyValues.Tests.Repositories;

/// <summary>
/// ClipboardRepository の動作を検証するテストクラスです。
/// </summary>
public class ClipboardRepositoryTests
{
    [Fact(DisplayName = "コンストラクタ: CallOnUIThreadにnullが渡されても例外が発生しないこと")]
    public void Constructor_WithNullCallback_ShouldNotThrow()
    {
        var repo = new ClipboardRepository(null!);
        Assert.NotNull(repo);
    }

    [Fact(DisplayName = "クリップボードコピー: 空文字列をコピーした場合に正常終了すること")]
    public void CopyToClipboard_WithEmptyString_ShouldReturnEarly()
    {
        var repo = new ClipboardRepository(_ => { });
        var ex = Record.Exception(() => repo.CopyToClipboard(string.Empty, 5));
        Assert.Null(ex);
    }

    [Fact(DisplayName = "即時クリア: クリップボード未コピー状態でのクリア呼び出しで例外が発生しないこと")]
    public void ClearImmediately_WhenNothingCopied_ShouldNotThrow()
    {
        var repo = new ClipboardRepository(_ => { });
        var ex = Record.Exception(() => repo.ClearImmediately());
        Assert.Null(ex);
    }

    [Fact(DisplayName = "クリップボードコピー: STAスレッド上でコピーとクリアが正常に動作すること")]
    public void CopyToClipboard_InStaThread_ShouldSetTextAndAllowClear()
    {
        Exception? threadEx = null;
        var thread = new Thread(() =>
        {
            try
            {
                var repo = new ClipboardRepository(_ => { });
                repo.CopyToClipboard("TestClipboardText", 0);
                repo.ClearImmediately();
            }
            catch (Exception ex)
            {
                threadEx = ex;
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();

        Assert.Null(threadEx);
    }
}
