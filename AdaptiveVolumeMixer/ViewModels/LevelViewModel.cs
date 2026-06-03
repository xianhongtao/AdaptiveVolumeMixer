using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using AdaptiveVolumeMixer.Services;

namespace AdaptiveVolumeMixer.ViewModels;

/// <summary>
/// 层级视图模型
/// </summary>
public partial class LevelViewModel : ObservableObject
{
    /// <summary>
    /// 当属性变更时触发，用于通知 MainViewModel 同步配置
    /// </summary>
    public event Action<string>? PropertyUpdated;

    [ObservableProperty]
    private int _level;

    /// <summary>
    /// 是否为最高优先级层级（Level == 0）
    /// </summary>
    public bool IsHighestPriority => Level == 0;

    /// <summary>
    /// 层级在列表中的 0-based 索引，用于颜色转换器
    /// </summary>
    [ObservableProperty]
    private int _levelIndex;

    [ObservableProperty]
    private string _displayName = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SuppressRatioPercent))]
    private float _suppressRatio = 0.2f;

    /// <summary>
    /// 目标音量的百分比显示文本
    /// </summary>
    public string SuppressRatioPercent => $"{SuppressRatio:P0}";

    public ObservableCollection<LevelItemViewModel> Processes { get; } = [];

    [ObservableProperty]
    private string _processCount = LocalizationManager.Instance.GetString("Status.ProcessCount", 0);

    /// <summary>
    /// 是否有进程（用于控制空状态提示的显示）
    /// </summary>
    public bool HasProcesses => Processes.Count > 0;

    public LevelViewModel()
    {
        Processes.CollectionChanged += (_, _) =>
        {
            ProcessCount = LocalizationManager.Instance.GetString("Status.ProcessCount", Processes.Count);
            OnPropertyChanged(nameof(HasProcesses));
        };
    }

    partial void OnLevelChanged(int value)
    {
        OnPropertyChanged(nameof(IsHighestPriority));
        PropertyUpdated?.Invoke(nameof(Level));
    }

    partial void OnDisplayNameChanged(string value)
    {
        PropertyUpdated?.Invoke(nameof(DisplayName));
    }

    partial void OnSuppressRatioChanged(float value)
    {
        PropertyUpdated?.Invoke(nameof(SuppressRatio));
    }
}
