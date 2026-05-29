using System.Globalization;
using System.Windows.Data;

namespace AdaptiveVolumeMixer.Converters;

/// <summary>
/// 非空值转布尔值转换器
/// </summary>
public class NullToBoolConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value != null;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
