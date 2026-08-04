概要
==

accounts (アカウント情報)

各サービスの認証情報（ログインID、パスワード、URL、メモ、更新日時など）を暗号化して管理するテーブルです。

テーブル定義
======

| カラム名 | 型 | モード | 説明 |
| :--- | :--- | :--- | :--- |
| `id` | TEXT | REQUIRED | アカウント一意識別子 (主キー, UUID) |
| `service_name` | TEXT | REQUIRED | サービス名 |
| `login_id` | TEXT | REQUIRED | ログインID / ユーザー名 (マスターパスワードで暗号化されたBase64文字列) |
| `password` | TEXT | REQUIRED | パスワード (マスターパスワードで暗号化されたBase64文字列) |
| `url` | TEXT | NULLABLE | 関連WebサイトのURL (マスターパスワードで暗号化されたBase64文字列) |
| `memo` | TEXT | NULLABLE | メモ・補足情報 (マスターパスワードで暗号化されたBase64文字列) |
| `updated_at` | TEXT | REQUIRED | 最終更新日時 (フォーマット: `yyyy-MM-dd HH:mm:ss`) |

一意キー
======

| No | シーケンス | カラム名 | 説明 |
| :--- | :--- | :--- | :--- |
| 1 | 1 | `id` | 主キー (PRIMARY KEY) |
