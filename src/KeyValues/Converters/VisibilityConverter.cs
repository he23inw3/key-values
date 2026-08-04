using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace KeyValues.Converters;

/// <summary>
/// <see cref="bool"/> や <see cref="int"/> の値に応じて <see cref="Visibility"/>（Visible / Collapsed）へ変換するコンバーターです。
/// </summary>
public class VisibilityConverter : IValueConverter
{
    /// <summary>
    /// 値を <see cref="Visibility"/> に変換します。
    /// </summary>
    /// <param name="value">バインディングソースから渡された値（bool または int）</param>
    /// <param name="targetType">ターゲットプロパティの型</param>
    /// <param name="parameter">使用するコンバーターパラメーター</param>
    /// <param name="culture">使用するカルチャ</param>
    /// <returns>条件を満たす場合は <see cref="Visibility.Visible"/>、それ以外は <see cref="Visibility.Collapsed"/></returns>
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool b) return b ? Visibility.Visible : Visibility.Collapsed;
        if (value is int i) return i > 0 ? Visibility.Visible : Visibility.Collapsed;
        return Visibility.Collapsed;
    }

    /// <summary>
    /// 逆変換処理はサポートしていません。
    /// </summary>
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
}
