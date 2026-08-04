using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using Microsoft.Data.Sqlite;
using Xunit;
using KeyValues.Models;
using KeyValues.Repositories;

namespace KeyValues.Tests.Repositories;

/// <summary>
/// SqliteAccountRepository の各機能（初期化、保存・読込、ヒント、バックアップ・復元、自動ログイン）を検証するテストクラスです。
/// </summary>
public class SqliteAccountRepositoryTests : IDisposable
{
    private readonly string _dbPath;

    public SqliteAccountRepositoryTests()
    {
        _dbPath = $"test-repo-{Guid.NewGuid():N}.db";
    }

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        try { if (File.Exists(_dbPath)) File.Delete(_dbPath); } catch { }
        string dbDir = Path.GetDirectoryName(Path.GetFullPath(_dbPath)) ?? ".";
        string autoLoginPath = Path.Combine(dbDir, "autologin.dat");
        try { if (File.Exists(autoLoginPath)) File.Delete(autoLoginPath); } catch { }
    }

    private SqliteAccountRepository CreateRepository() => new SqliteAccountRepository(_dbPath);

    [Fact(DisplayName = "FileExists: DB未初期化時にfalseが返されること")]
    public void FileExists_WhenDatabaseNotInitialized_ShouldReturnFalse()
    {
        var repo = CreateRepository();
        Assert.False(repo.FileExists());
    }

    [Fact(DisplayName = "FileExists: DB初期化・検証データ登録後にtrueが返されること")]
    public void FileExists_AfterInitAndRegister_ShouldReturnTrue()
    {
        var repo = CreateRepository();
        repo.InitializeDatabase();
        repo.RegisterVerification("TestPassword1");

        Assert.True(repo.FileExists());
    }

    [Fact(DisplayName = "InitializeDatabase: DBファイルが正しく作成されること")]
    public void InitializeDatabase_ShouldCreateDbFile()
    {
        var repo = CreateRepository();
        repo.InitializeDatabase();

        Assert.True(File.Exists(Path.GetFullPath(_dbPath)));
    }

    [Fact(DisplayName = "VerifyMasterPassword: 正しいパスワードの検証でtrueが返されること")]
    public void VerifyMasterPassword_WithCorrectPassword_ShouldReturnTrue()
    {
        var repo = CreateRepository();
        repo.InitializeDatabase();
        repo.RegisterVerification("CorrectPass123");

        Assert.True(repo.VerifyMasterPassword("CorrectPass123"));
    }

    [Fact(DisplayName = "VerifyMasterPassword: 誤ったパスワードの検証でfalseが返されること")]
    public void VerifyMasterPassword_WithWrongPassword_ShouldReturnFalse()
    {
        var repo = CreateRepository();
        repo.InitializeDatabase();
        repo.RegisterVerification("CorrectPass123");

        Assert.False(repo.VerifyMasterPassword("WrongPass456"));
    }

    [Fact(DisplayName = "Save/Load: アカウントデータの保存および正しく復元できること")]
    public void Save_And_Load_ShouldPersistAndRestoreEntries()
    {
        var repo = CreateRepository();
        repo.InitializeDatabase();
        repo.RegisterVerification("MasterPass1");

        var entries = new List<AccountEntry>
        {
            new AccountEntry
            {
                Id = Guid.NewGuid().ToString(),
                ServiceName = "GitHub",
                LoginId = "dev@github.com",
                Password = "gh-secret-pw",
                Url = "https://github.com",
                Memo = "Dev account",
                UpdatedAt = new DateTime(2024, 1, 15, 0, 0, 0)
            }
        };

        repo.Save(entries, "MasterPass1");
        SqliteConnection.ClearAllPools();

        var loaded = repo.Load("MasterPass1");

        Assert.Single(loaded);
        Assert.Equal("GitHub", loaded[0].ServiceName);
        Assert.Equal("dev@github.com", loaded[0].LoginId);
    }

    [Fact(DisplayName = "ヒント機能: パスワードヒントの保存および取得が正しく行えること")]
    public void SavePasswordHint_And_GetPasswordHint_ShouldPersistHint()
    {
        var repo = CreateRepository();
        repo.InitializeDatabase();
        repo.SavePasswordHint("My pet's name");
        SqliteConnection.ClearAllPools();

        string hint = repo.GetPasswordHint();
        Assert.Equal("My pet's name", hint);
    }

    [Fact(DisplayName = "バックアップ: データベースのバックアップファイルが正常に作成されること")]
    public void Backup_ShouldCreateCopyOfDatabase()
    {
        string backupPath = $"test-backup-{Guid.NewGuid():N}.db";
        try
        {
            var repo = CreateRepository();
            repo.InitializeDatabase();
            repo.RegisterVerification("MasterPass1");

            repo.Backup(backupPath);

            Assert.True(File.Exists(backupPath));
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            if (File.Exists(backupPath)) File.Delete(backupPath);
        }
    }

    [Fact(DisplayName = "自動ログイン: 自動ログインの有効化・パスワード読み込みが正常に行えること")]
    public void EnableAutoLogin_And_LoadAutoLoginPassword_ShouldRoundTripPassword()
    {
        var repo = CreateRepository();
        repo.EnableAutoLogin("SecretMasterPassword123");

        Assert.True(repo.IsAutoLoginEnabled());

        string loadedPassword = repo.LoadAutoLoginPassword();
        Assert.Equal("SecretMasterPassword123", loadedPassword);
    }

    [Fact(DisplayName = "リセット: ResetDatabase呼び出しでデータベースファイルが削除されること")]
    public void ResetDatabase_ShouldDeleteDatabaseFile()
    {
        var repo = CreateRepository();
        repo.InitializeDatabase();
        repo.RegisterVerification("TestPass");
        Assert.True(File.Exists(Path.GetFullPath(_dbPath)));

        SqliteConnection.ClearAllPools();
        repo.ResetDatabase();

        Assert.False(File.Exists(Path.GetFullPath(_dbPath)));
    }
}
