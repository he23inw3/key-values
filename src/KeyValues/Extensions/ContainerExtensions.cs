using System;
using Prism.Ioc;

namespace KeyValues.Extensions;

/// <summary>
/// Prism DI コンテナの安全な依存性解決およびフォールバック処理を提供する拡張メソッドです。
/// </summary>
public static class ContainerExtensions
{
    /// <summary>
    /// 引数のインスタンスが null の場合、Prism コンテナからの解決を試み、
    /// コンテナ未登録・未初期化時（単体テスト時など）は既定のファクトリまたは引数なしコンストラクタによるインスタンス化を行います。
    /// </summary>
    /// <typeparam name="T">解決対象の型</typeparam>
    /// <param name="instance">直接注入されたインスタンス（null 許容）</param>
    /// <param name="defaultFactory">DI コンテナにも存在しない場合のフォールバック生成関数（省略可能）</param>
    /// <returns>解決または生成されたインスタンス</returns>
    public static T ResolveOrDefault<T>(this T? instance, Func<T>? defaultFactory = null) where T : class
    {
        if (instance != null)
        {
            return instance;
        }

        try
        {
            if (ContainerLocator.Container != null && ContainerLocator.Container.IsRegistered<T>())
            {
                return ContainerLocator.Container.Resolve<T>();
            }
        }
        catch
        {
            // DI コンテナ解決例外発生時はフォールバックへ
        }

        if (defaultFactory != null)
        {
            return defaultFactory();
        }

        return Activator.CreateInstance<T>();
    }
}
