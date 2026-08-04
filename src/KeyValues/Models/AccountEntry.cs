using System;
using Prism.Mvvm;

namespace KeyValues.Models;

/// <summary>
/// 登録された1件のアカウント情報（サービス名、ログインID、暗号化/復号用パスワード、URL、メモ等）を表すモデルクラスです。
/// </summary>
public class AccountEntry : BindableBase
{
    private string _id = Guid.NewGuid().ToString();
    private string _serviceName = string.Empty;
    private string _loginId = string.Empty;
    private string _password = string.Empty;
    private string _url = string.Empty;
    private string _memo = string.Empty;
    private DateTime _updatedAt = DateTime.Now;

    /// <summary>
    /// エントリーを一意に識別する GUID 文字列を取得または設定します。
    /// </summary>
    public string Id
    {
        get => _id;
        set => SetProperty(ref _id, value);
    }

    /// <summary>
    /// アカウントのサービス名（例: Amazon, Google）を取得または設定します。
    /// </summary>
    public string ServiceName
    {
        get => _serviceName;
        set => SetProperty(ref _serviceName, value);
    }

    /// <summary>
    /// ログイン ID またはユーザー名（例: example@gmail.com）を取得または設定します。
    /// </summary>
    public string LoginId
    {
        get => _loginId;
        set => SetProperty(ref _loginId, value);
    }

    /// <summary>
    /// アカウントのパスワード（メモリ上のみ平文で保持され、DB保存時は暗号化されます）を取得または設定します。
    /// </summary>
    public string Password
    {
        get => _password;
        set => SetProperty(ref _password, value);
    }

    /// <summary>
    /// サービスのログインページ等の URL（例: https://amazon.co.jp）を取得または設定します。
    /// </summary>
    public string Url
    {
        get => _url;
        set => SetProperty(ref _url, value);
    }

    /// <summary>
    /// アカウントに関する補足メモを取得または設定します。
    /// </summary>
    public string Memo
    {
        get => _memo;
        set => SetProperty(ref _memo, value);
    }

    /// <summary>
    /// このアカウント情報の最終更新日時を取得または設定します。
    /// </summary>
    public DateTime UpdatedAt
    {
        get => _updatedAt;
        set => SetProperty(ref _updatedAt, value);
    }
}
