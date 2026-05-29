using CommunityToolkit.Mvvm.ComponentModel;

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

    public string VolumeText => $"{CurrentVolume:P0}";
    public string StatusText => IsPlaying ? "🔊 播放中" : (IsSuppressed ? "🔇 被压制" : "🔈 静默");
}
