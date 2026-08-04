using System;
using System.Threading.Tasks;
using Windows.Security.Credentials.UI;

namespace KeyValues.Providers;

/// <summary>
/// Windows Hello 生体認証プロバイダーの実装クラスです。
/// </summary>
public class WindowsHelloProvider
{
    private static WindowsHelloProvider _instance = new WindowsHelloProvider();

    /// <summary>
    /// テストや代替プロバイダー指定用の <see cref="WindowsHelloProvider"/> インスタンスを取得または設定します。
    /// </summary>
    public static WindowsHelloProvider Instance
    {
        get => _instance;
        set => _instance = value ?? new WindowsHelloProvider();
    }

    /// <summary>
    /// Windows Hello 生体認証が利用可能かどうかを非同期で確認します。
    /// </summary>
    /// <returns>利用可能な場合は <c>true</c>。それ以外（未設定・非対応・エラー発生時など）の場合は <c>false</c>。</returns>
    public async Task<bool> IsAvailableAsync()
    {
        try
        {
            var availability = await UserConsentVerifier.CheckAvailabilityAsync();
            return availability == UserConsentVerifierAvailability.Available;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 指定されたプロンプトメッセージを表示して Windows Hello 認証を非同期で要求します。
    /// </summary>
    /// <param name="promptMessage">認証ダイアログに表示するプロンプトメッセージ。</param>
    /// <returns>ユーザー認証に成功した場合は <c>true</c>。失敗、キャンセル、またはエラー発生時は <c>false</c>。</returns>
    public async Task<bool> RequestVerificationAsync(string promptMessage)
    {
        try
        {
            var result = await UserConsentVerifier.RequestVerificationAsync(promptMessage);
            return result == UserConsentVerificationResult.Verified;
        }
        catch
        {
            return false;
        }
    }
}
