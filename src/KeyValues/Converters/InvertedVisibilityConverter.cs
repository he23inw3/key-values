using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace KeyValues.Converters;

/// <summary>
/// 条件判定結果を反転させて <see cref="Visibility"/>（Collapsed / Visible）へ変換するコンバーターです。
/// </summary>
public class InvertedVisibilityConverter : IValueConverter
{
    /// <summary>
    /// 値を反転した <see cref="Visibility"/> に変換します。
    /// </summary>
    /// <param name="value">バインディングソースから渡された値（bool, int, または string）</param>
    /// <param name="targetType">ターゲットプロパティの型</param>
    /// <param name="parameter">使用するコンバーターパラメーター</param>
    /// <param name="culture">使用するカルチャ</param>
    /// <returns>条件を満たす場合は <see cref="Visibility.Collapsed"/>、それ以外は <see cref="Visibility.Visible"/></returns>
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool b) return b ? Visibility.Collapsed : Visibility.Visible;
        if (value is int i) return i > 0 ? Visibility.Collapsed : Visibility.Visible;
        if (value is string s) return !string.IsNullOrEmpty(s) ? Visibility.Collapsed : Visibility.Visible;
        return Visibility.Visible;
    }

    /// <summary>
    /// 逆変換処理はサポートしていません。
    /// </summary>
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
}
