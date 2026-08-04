using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace KeyValues.Converters;

/// <summary>
/// 文字列が非空・非空白であるかどうかに応じて <see cref="Visibility"/>（Visible / Collapsed）へ変換するコンバーターです。
/// </summary>
public class StringVisibilityConverter : IValueConverter
{
    /// <summary>
    /// 文字列値を <see cref="Visibility"/> に変換します。
    /// </summary>
    /// <param name="value">バインディングソースから渡された文字列</param>
    /// <param name="targetType">ターゲットプロパティの型</param>
    /// <param name="parameter">使用するコンバーターパラメーター</param>
    /// <param name="culture">使用するカルチャ</param>
    /// <returns>有効な文字列の場合は <see cref="Visibility.Visible"/>、空または null の場合は <see cref="Visibility.Collapsed"/></returns>
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return string.IsNullOrWhiteSpace(value as string) ? Visibility.Collapsed : Visibility.Visible;
    }

    /// <summary>
    /// 逆変換処理はサポートしていません。
    /// </summary>
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
}
