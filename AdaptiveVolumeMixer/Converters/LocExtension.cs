using System.Windows.Markup;

namespace AdaptiveVolumeMixer.Converters;

/// <summary>
/// WPF 本地化 Markup 扩展，在 XAML 中使用 {l:Loc KeyName} 获取翻译文本
/// </summary>
public class LocExtension : MarkupExtension
{
    /// <summary>
    /// 资源键名
    /// </summary>
    public string Key { get; }

    public LocExtension(string key)
    {
        Key = key;
    }

    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        return Services.LocalizationManager.Instance.GetString(Key);
    }
}
