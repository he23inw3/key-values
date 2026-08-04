using System;
using System.Globalization;
using System.Windows.Data;

namespace KeyValues.Converters;

/// <summary>
/// ダークモード/ライトモードフラグに応じて Segoe MDL2 Assets アイコンフォント文字列へ変換するコンバーターです。
/// </summary>
public class ThemeIconConverter : IValueConverter
{
    /// <summary>
    /// 現在のテーマフラグに応じたアイコングリフ文字を返します。
    /// </summary>
    /// <param name="value">ダークモード有効状態を示す boolean 値</param>
    /// <param name="targetType">ターゲットプロパティの型</param>
    /// <param name="parameter">使用するコンバーターパラメーター</param>
    /// <param name="culture">使用するカルチャ</param>
    /// <returns>ダークモード時は Sun アイコン（\uE706）、ライトモード時は Moon アイコン（\uE708）</returns>
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool isDark)
        {
            return isDark ? "\uE706" : "\uE708"; // E706 = Sun, E708 = Moon
        }
        return "\uE706";
    }

    /// <summary>
    /// 逆変換処理はサポートしていません。
    /// </summary>
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
}
