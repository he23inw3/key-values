using System;
using System.Collections.Generic;
using System.Data;
using Dapper;
using Microsoft.Data.Sqlite;

namespace KeyValues.Repositories;

/// <summary>
/// SQLite データベースへのオープン済み Dapper コネクションおよび簡易ヘルパーを提供するクラスです。
/// </summary>
public class DbHelper
{
    private readonly string _connectionString;

    public DbHelper(string connectionString)
    {
        _connectionString = connectionString;
    }

    /// <summary>
    /// オープン済みの SQLite 接続を作成して返します。
    /// </summary>
    public SqliteConnection CreateOpenConnection()
    {
        var connection = new SqliteConnection(_connectionString);
        connection.Open();
        return connection;
    }

    /// <summary>
    /// Dapper を使用して SQL コマンド（INSERT / UPDATE / DELETE 等）を実行します。
    /// </summary>
    public int Execute(string sql, object? param = null, IDbTransaction? transaction = null)
    {
        using var connection = CreateOpenConnection();
        return connection.Execute(sql, param, transaction);
    }

    /// <summary>
    /// Dapper を使用して単一スカラー値を取得します。
    /// </summary>
    public T? ExecuteScalar<T>(string sql, object? param = null, IDbTransaction? transaction = null)
    {
        using var connection = CreateOpenConnection();
        return connection.ExecuteScalar<T>(sql, param, transaction);
    }
}
