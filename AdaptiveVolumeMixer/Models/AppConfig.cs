namespace AdaptiveVolumeMixer.Models;

/// <summary>
/// 应用程序全局配置
/// </summary>
public class AppConfig
{
    /// <summary>
    /// 层级列表（从高到低排序）
    /// </summary>
    public List<LevelConfig> Levels { get; set; } = new();

    /// <summary>
    /// 轮询间隔（毫秒），用于检测音频播放状态
    /// </summary>
    public int PollingIntervalMs { get; set; } = 1000;

    /// <summary>
    /// 界面语言（null=自动检测, "zh"=中文, "en"=英文）
    /// </summary>
    public string? Language { get; set; }

    /// <summary>
    /// 创建默认配置
    /// </summary>
    public static AppConfig CreateDefault()
    {
        return new AppConfig
        {
            PollingIntervalMs = 1000,
            Levels = new List<LevelConfig>
            {
                new(0, Services.LocalizationManager.Instance.GetString("Default.LevelName", 0)),
                new(-1, Services.LocalizationManager.Instance.GetString("Default.LevelName", -1)),
                new(-2, Services.LocalizationManager.Instance.GetString("Default.LevelName", -2)),
                new(-3, Services.LocalizationManager.Instance.GetString("Default.LevelName", -3)),
                new(-4, Services.LocalizationManager.Instance.GetString("Default.LevelName", -4)),
                new(-5, Services.LocalizationManager.Instance.GetString("Default.LevelName", -5)),
            }
        };
    }
}
