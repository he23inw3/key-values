using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Dapper;
using KeyValues.Dtos;
using KeyValues.Models;
using KeyValues.Services;
using Microsoft.Data.Sqlite;

namespace KeyValues.Repositories;

/// <summary>
/// Dapper (Micro-ORM) を使用して暗号化 SQLite データベースとのオブジェクトマッピング・永続化・認証管理を制御するリポジトリ実装です。
/// </summary>
public class SqliteAccountRepository
{
    private readonly string _dbDirectory;
    private readonly string _dbFilePath;
    private readonly string _connectionString;
    private readonly DbHelper _dbHelper;
    private static readonly byte[] Entropy = { 0x4B, 0x65, 0x79, 0x56, 0x61, 0x6C, 0x75, 0x65, 0x73, 0x41, 0x75, 0x74, 0x6F }; // "KeyValuesAuto"

    private readonly CryptoService _cryptoService;

    /// <summary>
    /// <see cref="SqliteAccountRepository"/> クラスの新しいインスタンスを初期化します。
    /// </summary>
    /// <param name="customDbPath">カスタムデータベースファイルパス（テスト用など）。省略した場合はデフォルトのパスが使用されます。</param>
    /// <param name="cryptoService">暗号化サービス。省略した場合は既定の実装が使用されます。</param>
    public SqliteAccountRepository(string? customDbPath = null, CryptoService? cryptoService = null)
    {
        _cryptoService = cryptoService ?? new CryptoService();

        if (!string.IsNullOrEmpty(customDbPath))
        {
            _dbFilePath = Path.GetFullPath(customDbPath);
            _dbDirectory = Path.GetDirectoryName(_dbFilePath) ?? AppDomain.CurrentDomain.BaseDirectory;
        }
        else
        {
            _dbDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data");
            _dbFilePath = Path.Combine(_dbDirectory, "key-values.db");
        }
        _connectionString = $"Data Source={_dbFilePath}";
        _dbHelper = new DbHelper(_connectionString);
    }

    /// <summary>
    /// データベースファイルが存在し、検証用メタデータが登録済みであるかを確認します。
    /// </summary>
    /// <returns>有効なデータベースが存在する場合は true、それ以外は false。</returns>
    public bool FileExists()
    {
        if (!File.Exists(_dbFilePath)) return false;
        try
        {
            int count = _dbHelper.ExecuteScalar<int>("SELECT COUNT(*) FROM metadata WHERE key = 'verification';");
            return count > 0;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// データベース保存用ディレクトリおよび必要なテーブル構造（metadata, accounts）を作成・初期化します。
    /// </summary>
    public void InitializeDatabase()
    {
        if (!Directory.Exists(_dbDirectory))
        {
            Directory.CreateDirectory(_dbDirectory);
        }

        _dbHelper.Execute(@"
            CREATE TABLE IF NOT EXISTS metadata (
                key TEXT PRIMARY KEY,
                value TEXT NOT NULL
            );
            CREATE TABLE IF NOT EXISTS accounts (
                id TEXT PRIMARY KEY,
                service_name TEXT NOT NULL,
                login_id TEXT NOT NULL,
                password TEXT NOT NULL,
                url TEXT,
                memo TEXT,
                updated_at TEXT NOT NULL
            );");
    }

    /// <summary>
    /// 指定されたマスターパスワードで検証用固定トークンを暗号化し、metadata テーブルに登録します。
    /// </summary>
    /// <param name="masterPassword">登録するマスターパスワード。</param>
    public void RegisterVerification(string masterPassword)
    {
        byte[] encBytes = _cryptoService.Encrypt("KeyValuesVaultVerification", masterPassword);
        string encBase64 = Convert.ToBase64String(encBytes);

        _dbHelper.Execute(
            "INSERT OR REPLACE INTO metadata (key, value) VALUES ('verification', @value);",
            new { value = encBase64 }
        );
    }

    /// <summary>
    /// 入力されたマスターパスワードが、データベースに保存された検証トークンを正常に復号できるか照合します。
    /// </summary>
    /// <param name="masterPassword">検証するマスターパスワード。</param>
    /// <returns>パスワードが正しければ true、誤りまたはエラー時は false。</returns>
    public bool VerifyMasterPassword(string masterPassword)
    {
        if (!File.Exists(_dbFilePath)) return false;

        try
        {
            string? encBase64 = _dbHelper.ExecuteScalar<string>("SELECT value FROM metadata WHERE key = 'verification';");
            if (string.IsNullOrEmpty(encBase64)) return false;

            byte[] encBytes = Convert.FromBase64String(encBase64);
            string decrypted = _cryptoService.Decrypt(encBytes, masterPassword);
            return decrypted == "KeyValuesVaultVerification";
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// アカウントエントリーの一覧をマスターパスワードで暗号化し、Dapper を使用してデータベースに上書き一括保存します。
    /// </summary>
    /// <param name="entries">保存するアカウントエントリーのリスト。</param>
    /// <param name="masterPassword">暗号化に使用するマスターパスワード。</param>
    public void Save(List<AccountEntry> entries, string masterPassword)
    {
        InitializeDatabase();

        if (!VerifyMasterPassword(masterPassword))
        {
            RegisterVerification(masterPassword);
        }

        using var connection = _dbHelper.CreateOpenConnection();
        using var transaction = connection.BeginTransaction();

        connection.Execute("DELETE FROM accounts;", transaction: transaction);

        var dbEntities = entries.Select(entry =>
        {
            byte[] encLoginIdBytes = _cryptoService.Encrypt(entry.LoginId, masterPassword);
            byte[] encPasswordBytes = _cryptoService.Encrypt(entry.Password, masterPassword);
            byte[] encUrlBytes = string.IsNullOrEmpty(entry.Url) ? Array.Empty<byte>() : _cryptoService.Encrypt(entry.Url, masterPassword);
            byte[] encMemoBytes = string.IsNullOrEmpty(entry.Memo) ? Array.Empty<byte>() : _cryptoService.Encrypt(entry.Memo, masterPassword);

            var entity = new AccountDto(
                id: entry.Id,
                service_name: entry.ServiceName,
                login_id: Convert.ToBase64String(encLoginIdBytes),
                password: Convert.ToBase64String(encPasswordBytes),
                url: encUrlBytes.Length == 0 ? null : Convert.ToBase64String(encUrlBytes),
                memo: encMemoBytes.Length == 0 ? null : Convert.ToBase64String(encMemoBytes),
                updated_at: entry.UpdatedAt.ToString("yyyy-MM-dd HH:mm:ss")
            );

            Array.Clear(encLoginIdBytes, 0, encLoginIdBytes.Length);
            Array.Clear(encPasswordBytes, 0, encPasswordBytes.Length);
            if (encUrlBytes.Length > 0) Array.Clear(encUrlBytes, 0, encUrlBytes.Length);
            if (encMemoBytes.Length > 0) Array.Clear(encMemoBytes, 0, encMemoBytes.Length);

            return entity;
        }).ToList();

        connection.Execute(@"
            INSERT INTO accounts (id, service_name, login_id, password, url, memo, updated_at)
            VALUES (@id, @service_name, @login_id, @password, @url, @memo, @updated_at);",
            dbEntities, transaction: transaction);

        transaction.Commit();
    }

    /// <summary>
    /// マスターパスワードでデータベースを復号し、Dapper を使用して全アカウントエントリーをマッピング・取得します。
    /// </summary>
    /// <param name="masterPassword">復号に使用するマスターパスワード。</param>
    /// <returns>復号されたアカウントエントリーのリスト。</returns>
    /// <exception cref="CryptographicException">マスターパスワードが不一致の場合に発生します。</exception>
    public List<AccountEntry> Load(string masterPassword)
    {
        if (!FileExists()) return new List<AccountEntry>();

        if (!VerifyMasterPassword(masterPassword))
        {
            throw new CryptographicException("Incorrect master password.");
        }

        using var connection = _dbHelper.CreateOpenConnection();
        var entities = connection.Query<AccountDto>("SELECT id, service_name, login_id, password, url, memo, updated_at FROM accounts;");

        var entries = new List<AccountEntry>();

        foreach (var entity in entities)
        {
            byte[] encLoginIdBytes = Convert.FromBase64String(entity.login_id);
            byte[] encPasswordBytes = Convert.FromBase64String(entity.password);
            byte[] encUrlBytes = string.IsNullOrEmpty(entity.url) ? Array.Empty<byte>() : Convert.FromBase64String(entity.url);
            byte[] encMemoBytes = string.IsNullOrEmpty(entity.memo) ? Array.Empty<byte>() : Convert.FromBase64String(entity.memo);

            string loginId = _cryptoService.Decrypt(encLoginIdBytes, masterPassword);
            string password = _cryptoService.Decrypt(encPasswordBytes, masterPassword);
            string url = encUrlBytes.Length == 0 ? "" : _cryptoService.Decrypt(encUrlBytes, masterPassword);
            string memo = encMemoBytes.Length == 0 ? "" : _cryptoService.Decrypt(encMemoBytes, masterPassword);

            entries.Add(new AccountEntry
            {
                Id = entity.id,
                ServiceName = entity.service_name,
                LoginId = loginId,
                Password = password,
                Url = url,
                Memo = memo,
                UpdatedAt = DateTime.Parse(entity.updated_at)
            });

            Array.Clear(encLoginIdBytes, 0, encLoginIdBytes.Length);
            Array.Clear(encPasswordBytes, 0, encPasswordBytes.Length);
            if (encUrlBytes.Length > 0) Array.Clear(encUrlBytes, 0, encUrlBytes.Length);
            if (encMemoBytes.Length > 0) Array.Clear(encMemoBytes, 0, encMemoBytes.Length);
        }

        return entries;
    }

    /// <summary>
    /// 現在の SQLite データベースファイルを指定されたバックアップ先パスへ複製コピーします。
    /// </summary>
    /// <param name="destinationPath">バックアップ先のファイルパス。</param>
    /// <exception cref="FileNotFoundException">データベースファイルが存在しない場合に発生します。</exception>
    public void Backup(string destinationPath)
    {
        if (!File.Exists(_dbFilePath))
        {
            throw new FileNotFoundException("Source database file does not exist.");
        }

        string? dir = Path.GetDirectoryName(destinationPath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        File.Copy(_dbFilePath, destinationPath, true);
    }

    /// <summary>
    /// 指定されたバックアップファイルからマスターパスワードを照合し、現在のデータベースファイルへ上書き復元します。
    /// </summary>
    /// <param name="sourcePath">復元元のバックアップファイルパス。</param>
    /// <param name="masterPassword">現在のマスターパスワード。</param>
    /// <exception cref="FileNotFoundException">指定のファイルが存在しない場合に発生します。</exception>
    /// <exception cref="InvalidOperationException">バックアップファイルの形式やメタデータが不正な場合に発生します。</exception>
    /// <exception cref="CryptographicException">マスターパスワードが一致しない場合に発生します。</exception>
    public void Restore(string sourcePath, string masterPassword)
    {
        if (!File.Exists(sourcePath))
        {
            throw new FileNotFoundException("Backup file to restore from does not exist.", sourcePath);
        }

        string tempConnString = $"Data Source={sourcePath}";
        string? encBase64 = null;
        try
        {
            var tempDbHelper = new DbHelper(tempConnString);
            encBase64 = tempDbHelper.ExecuteScalar<string>("SELECT value FROM metadata WHERE key = 'verification';");
        }
        catch
        {
            throw new InvalidOperationException("バックアップファイルの形式が正しくありません。");
        }

        if (string.IsNullOrEmpty(encBase64))
        {
            throw new InvalidOperationException("バックアップファイルに必要な認証メタデータが含まれていません。");
        }

        try
        {
            byte[] encBytes = Convert.FromBase64String(encBase64);
            string decrypted = _cryptoService.Decrypt(encBytes, masterPassword);
            if (decrypted != "KeyValuesVaultVerification")
            {
                throw new CryptographicException();
            }
        }
        catch (CryptographicException)
        {
            throw new CryptographicException("現在のマスターパスワードと一致しないため、復元できません。");
        }

        if (!Directory.Exists(_dbDirectory))
        {
            Directory.CreateDirectory(_dbDirectory);
        }

        File.Copy(sourcePath, _dbFilePath, true);
    }

    /// <summary>
    /// マスターパスワードのヒント文字列を metadata テーブルに保存します。
    /// </summary>
    /// <param name="hint">保存するヒント文字列。</param>
    public void SavePasswordHint(string hint)
    {
        InitializeDatabase();
        _dbHelper.Execute(
            "INSERT OR REPLACE INTO metadata (key, value) VALUES ('hint', @hint);",
            new { hint = hint ?? string.Empty }
        );
    }

    /// <summary>
    /// 保存されているマスターパスワードのヒント文字列を取得します。
    /// </summary>
    /// <returns>ヒント文字列（未設定時は空文字列）。</returns>
    public string GetPasswordHint()
    {
        if (!File.Exists(_dbFilePath)) return string.Empty;

        try
        {
            return _dbHelper.ExecuteScalar<string>("SELECT value FROM metadata WHERE key = 'hint';") ?? string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }

    /// <summary>
    /// Windows DPAPI (Data Protection API) を利用してマスターパスワードを安全に暗号化し、自動サインイン用ファイルとして保存します。
    /// </summary>
    /// <param name="masterPassword">暗号化保存するマスターパスワード。</param>
    public void EnableAutoLogin(string masterPassword)
    {
        if (string.IsNullOrEmpty(masterPassword)) return;

        byte[] plaintextBytes = System.Text.Encoding.UTF8.GetBytes(masterPassword);
        byte[] encryptedBytes = ProtectedData.Protect(plaintextBytes, Entropy, DataProtectionScope.CurrentUser);

        if (!Directory.Exists(_dbDirectory))
        {
            Directory.CreateDirectory(_dbDirectory);
        }

        string autoLoginFilePath = Path.Combine(_dbDirectory, "autologin.dat");
        File.WriteAllBytes(autoLoginFilePath, encryptedBytes);

        Array.Clear(plaintextBytes, 0, plaintextBytes.Length);
        Array.Clear(encryptedBytes, 0, encryptedBytes.Length);
    }

    /// <summary>
    /// 自動サインイン用暗号化ファイルを削除して、自動サインイン機能を無効化します。
    /// </summary>
    public void DisableAutoLogin()
    {
        string autoLoginFilePath = Path.Combine(_dbDirectory, "autologin.dat");
        if (File.Exists(autoLoginFilePath))
        {
            try { File.Delete(autoLoginFilePath); } catch { }
        }
    }

    /// <summary>
    /// 自動サインイン設定ファイルが存在し、有効化されているか判定します。
    /// </summary>
    /// <returns>自動サインインが有効であれば true、それ以外は false。</returns>
    public bool IsAutoLoginEnabled()
    {
        string autoLoginFilePath = Path.Combine(_dbDirectory, "autologin.dat");
        return File.Exists(autoLoginFilePath);
    }

    /// <summary>
    /// DPAPI で暗号化された自動サインイン用ファイルからマスターパスワードを復号して読み出します。
    /// </summary>
    /// <returns>復号されたマスターパスワード（失敗時や未存在時は空文字列）。</returns>
    public string LoadAutoLoginPassword()
    {
        string autoLoginFilePath = Path.Combine(_dbDirectory, "autologin.dat");
        if (!File.Exists(autoLoginFilePath)) return string.Empty;

        try
        {
            byte[] encryptedBytes = File.ReadAllBytes(autoLoginFilePath);
            byte[] plaintextBytes = ProtectedData.Unprotect(encryptedBytes, Entropy, DataProtectionScope.CurrentUser);
            string password = System.Text.Encoding.UTF8.GetString(plaintextBytes);

            Array.Clear(encryptedBytes, 0, encryptedBytes.Length);
            Array.Clear(plaintextBytes, 0, plaintextBytes.Length);

            return password;
        }
        catch
        {
            DisableAutoLogin();
            return string.Empty;
        }
    }

    /// <summary>
    /// データベースファイルおよび自動サインイン設定ファイルを完全に削除し、データベースをリセットします。
    /// </summary>
    /// <exception cref="IOException">データベースファイルの削除に失敗した場合に発生します。</exception>
    public void ResetDatabase()
    {
        DisableAutoLogin();

        try
        {
            if (File.Exists(_dbFilePath))
            {
                File.Delete(_dbFilePath);
            }
        }
        catch (Exception ex)
        {
            throw new IOException($"データベースファイルの削除に失敗しました: {ex.Message}", ex);
        }
    }
}
