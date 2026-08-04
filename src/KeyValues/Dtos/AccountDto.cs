namespace KeyValues.Dtos;

/// <summary>
/// SQLite データベースの accounts テーブルとのオブジェクトマッピング・データ転送を行う DTO (Data Transfer Object) です。
/// </summary>
/// <param name="id">アカウントの一意な識別子 (UUID)。</param>
/// <param name="service_name">サービス名。</param>
/// <param name="login_id">ログイン ID またはユーザー名。</param>
/// <param name="password">パスワード。</param>
/// <param name="url">関連する Web サイトの URL。</param>
/// <param name="memo">アカウントに関するメモ・備考。</param>
/// <param name="updated_at">更新日時文字列。</param>
public record AccountDto(
    string id,
    string service_name,
    string login_id,
    string password,
    string? url,
    string? memo,
    string updated_at
);
