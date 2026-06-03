using CommunityToolkit.Mvvm.ComponentModel;
using AdaptiveVolumeMixer.Services;

namespace AdaptiveVolumeMixer.ViewModels;

/// <summary>
/// 层级中的进程项视图模型
/// </summary>
public partial class LevelItemViewModel : ObservableObject
{
    [ObservableProperty]
    private int _level;

    [ObservableProperty]
    private string _processName = string.Empty;

    [ObservableProperty]
    private string _displayName = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(StatusText))]
    private bool _isPlaying;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(StatusText))]
    private bool _isSuppressed;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(VolumeText))]
    private float _currentVolume;

    [ObservableProperty]
    private float _originalVolume;

    /// <summary>
    /// 格式化显示名称（DisplayName + ProcessName）
    /// </summary>
    public string FullDisplayName => $"{DisplayName} ({ProcessName})";

    public string VolumeText => $"{CurrentVolume:P0}";
    public string StatusText => IsPlaying ? LocalizationManager.Instance.GetString("Status.Playing") : (IsSuppressed ? LocalizationManager.Instance.GetString("Status.Suppressed") : LocalizationManager.Instance.GetString("Status.Silent"));
}
