using System.Globalization;
using System.Windows.Data;
using Color = System.Windows.Media.Color;
using SolidColorBrush = System.Windows.Media.SolidColorBrush;

namespace AdaptiveVolumeMixer.Converters;

/// <summary>
/// 层级索引转颜色画笔转换器
/// 输入 0-based 索引，返回预设 10 色调色板中的颜色（循环复用）
/// </summary>
public class LevelIndexToColorBrushConverter : IValueConverter
{
    private static readonly Color[] Palette =
    [
        Color.FromRgb(0x1A, 0x23, 0x7E), // 深靛蓝
        Color.FromRgb(0x28, 0x35, 0x93),
        Color.FromRgb(0x30, 0x3F, 0x9F),
        Color.FromRgb(0x39, 0x49, 0xAB),
        Color.FromRgb(0x3F, 0x51, 0xB5),
        Color.FromRgb(0x5C, 0x6B, 0xC0), // 靛蓝
        Color.FromRgb(0x7E, 0x57, 0xC2), // 深紫
        Color.FromRgb(0x8E, 0x24, 0xAA), // 紫
        Color.FromRgb(0x00, 0x89, 0x7B), // 青色
        Color.FromRgb(0x43, 0xA0, 0x47), // 绿色
    ];

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        int index = 0;
        if (value is int i)
            index = i;
        else if (value is string s && int.TryParse(s, out int parsed))
            index = parsed;

        var color = Palette[Math.Abs(index) % Palette.Length];
        return new SolidColorBrush(color);
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
