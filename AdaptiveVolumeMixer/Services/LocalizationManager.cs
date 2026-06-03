using System.Globalization;
using System.Reflection;
using System.Resources;

namespace AdaptiveVolumeMixer.Services;

/// <summary>
/// 本地化管理器：启动时检测系统语言，提供字符串翻译
/// 单例模式，通过 Instance 访问
/// </summary>
public class LocalizationManager
{
    private static readonly Lazy<LocalizationManager> _instance = new(() => new LocalizationManager());

    /// <summary>
    /// 全局单例
    /// </summary>
    public static LocalizationManager Instance => _instance.Value;

    private readonly ResourceManager _resourceManager;

    /// <summary>
    /// 当前语言代码（"zh" 或 "en"）
    /// </summary>
    public string CurrentLanguage { get; private set; } = "zh";

    private LocalizationManager()
    {
        _resourceManager = new ResourceManager("AdaptiveVolumeMixer.Resources.Strings", Assembly.GetExecutingAssembly());
    }

    /// <summary>
    /// 初始化本地化，在 App.OnStartup 最早调用
    /// </summary>
    /// <param name="languageOverride">配置中的语言覆盖值，null 表示自动检测</param>
    public void Initialize(string? languageOverride)
    {
        if (!string.IsNullOrWhiteSpace(languageOverride) &&
            (languageOverride.StartsWith("zh", StringComparison.OrdinalIgnoreCase) ||
             languageOverride.StartsWith("en", StringComparison.OrdinalIgnoreCase)))
        {
            CurrentLanguage = languageOverride.StartsWith("zh", StringComparison.OrdinalIgnoreCase) ? "zh" : "en";
        }
        else
        {
            CurrentLanguage = CultureInfo.CurrentUICulture.Name.StartsWith("zh", StringComparison.OrdinalIgnoreCase)
                ? "zh"
                : "en";
        }
    }

    /// <summary>
    /// 获取翻译字符串
    /// </summary>
    public string GetString(string key)
    {
        try
        {
            var culture = CurrentLanguage == "zh"
                ? CultureInfo.GetCultureInfo("zh-CN")
                : CultureInfo.GetCultureInfo("en-US");

            var value = _resourceManager.GetString(key, culture);
            return value ?? $"[{key}]";
        }
        catch
        {
            // 回退到默认资源（中文）
            var value = _resourceManager.GetString(key);
            return value ?? $"[{key}]";
        }
    }

    /// <summary>
    /// 获取格式化翻译字符串
    /// </summary>
    public string GetString(string key, params object[] args)
    {
        var format = GetString(key);
        try
        {
            return string.Format(format, args);
        }
        catch
        {
            return format;
        }
    }
}
