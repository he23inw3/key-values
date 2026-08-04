using System;
using System.Globalization;
using System.Windows.Data;

namespace KeyValues.Converters;

/// <summary>
/// パスワード表示/非表示フラグに応じて Segoe MDL2 Assets アイコンフォント文字列へ変換するコンバーターです。
/// </summary>
public class VisibilityIconConverter : IValueConverter
{
    /// <summary>
    /// パスワード表示状態フラグに応じたアイコングリフ文字を返します。
    /// </summary>
    /// <param name="value">表示状態を示す boolean 値</param>
    /// <param name="targetType">ターゲットプロパティの型</param>
    /// <param name="parameter">使用するコンバーターパラメーター</param>
    /// <param name="culture">使用するカルチャ</param>
    /// <returns>表示状態時は EyeStrike アイコン（\uEC3E）、非表示時は Eye アイコン（\uE890）</returns>
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool visible)
        {
            return visible ? "\uEC3E" : "\uE890"; // EC3E = EyeStrike, E890 = Eye
        }
        return "\uE890";
    }

    /// <summary>
    /// 逆変換処理はサポートしていません。
    /// </summary>
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
}
