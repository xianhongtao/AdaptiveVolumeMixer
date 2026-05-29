using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Data;
using System.Windows.Input;
using AdaptiveVolumeMixer.Models;
using AdaptiveVolumeMixer.Services;

namespace AdaptiveVolumeMixer.ViewModels;

/// <summary>
/// 主界面 ViewModel
/// </summary>
public class MainViewModel : INotifyPropertyChanged
{
    private readonly AudioSessionService _audioManager;
    private readonly VolumeController _volumeController;
    private readonly ConfigManager _configManager;

    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// 层级视图集合
    /// </summary>
    public ObservableCollection<LevelViewModel> Levels { get; } = new();

    /// <summary>
    /// 所有可用的音频进程（用于添加到层级）
    /// </summary>
    public ObservableCollection<AudioProcess> AvailableProcesses { get; } = new();

    /// <summary>
    /// 状态信息
    /// </summary>
    private string _statusText = "就绪";
    public string StatusText
    {
        get => _statusText;
        set { _statusText = value; OnPropertyChanged(); }
    }

    /// <summary>
    /// 监控是否运行中
    /// </summary>
    private bool _isMonitoring;
    public bool IsMonitoring
    {
        get => _isMonitoring;
        set { _isMonitoring = value; OnPropertyChanged(); OnPropertyChanged(nameof(IsNotMonitoring)); }
    }

    public bool IsNotMonitoring => !IsMonitoring;

    /// <summary>
    /// 选中的可用进程
    /// </summary>
    private AudioProcess? _selectedAvailableProcess;
    public AudioProcess? SelectedAvailableProcess
    {
        get => _selectedAvailableProcess;
        set { _selectedAvailableProcess = value; OnPropertyChanged(); }
    }

    public ICommand StartMonitoringCommand { get; }
    public ICommand StopMonitoringCommand { get; }
    public ICommand RefreshProcessesCommand { get; }
    public ICommand RemoveProcessCommand { get; }
    public ICommand SaveConfigCommand { get; }
    public ICommand AddLevelCommand { get; }
    public ICommand RemoveLevelCommand { get; }

    public MainViewModel()
    {
        _configManager = new ConfigManager();
        _audioManager = new AudioSessionService();
        _volumeController = new VolumeController(_audioManager, _configManager);

        StartMonitoringCommand = new RelayCommand(StartMonitoring, () => IsNotMonitoring);
        StopMonitoringCommand = new RelayCommand(StopMonitoring, () => IsMonitoring);
        RefreshProcessesCommand = new RelayCommand(RefreshAvailableProcesses);
        RemoveProcessCommand = new RelayCommand<LevelItemViewModel>(RemoveProcess);
        SaveConfigCommand = new RelayCommand(SaveConfig);
        AddLevelCommand = new RelayCommand(AddLevel);
        RemoveLevelCommand = new RelayCommand<LevelViewModel>(RemoveLevel, vm => Levels.Count > 1);

        _volumeController.OnStateChanged += OnVolumeStateChanged;

        LoadConfig();
        InitializeAudio();
        RefreshAvailableProcesses();
    }

    private void InitializeAudio()
    {
        bool success = _audioManager.Initialize();
        StatusText = success ? "音频系统初始化成功" : "音频系统初始化失败，请以管理员身份运行";
    }

    private void LoadConfig()
    {
        Levels.Clear();
        // 按 Level 降序排列（0 为最高优先级，排在最上面）
        var sorted = _configManager.Config.Levels.OrderByDescending(l => l.Level).ToList();
        for (int i = 0; i < sorted.Count; i++)
        {
            var level = sorted[i];
            var levelVm = new LevelViewModel
            {
                Level = level.Level,
                LevelIndex = i,
                DisplayName = level.DisplayName,
                SuppressRatio = level.SuppressVolumeRatio,
            };
            // 用闭包绑定，确保回调能识别是哪个 LevelViewModel
            var capturedVm = levelVm;
            levelVm.PropertyUpdated += (propName) => OnLevelPropertyUpdated(capturedVm, propName);
            Levels.Add(levelVm);
        }
    }

    /// <summary>
    /// 刷新可用进程列表
    /// </summary>
    public void RefreshAvailableProcesses()
    {
        var sessions = _audioManager.GetAllAudioSessions();
        AvailableProcesses.Clear();
        foreach (var session in sessions.OrderBy(s => s.DisplayName))
        {
            AvailableProcesses.Add(session);
        }
        StatusText = $"已发现 {sessions.Count} 个音频进程";
    }

    /// <summary>
    /// 启动监控
    /// </summary>
    public void StartMonitoring()
    {
        _volumeController.Start();
        IsMonitoring = true;
        StatusText = "监控已启动";
    }

    /// <summary>
    /// 停止监控
    /// </summary>
    public void StopMonitoring()
    {
        _volumeController.Stop();
        IsMonitoring = false;
        StatusText = "监控已停止";
    }

    /// <summary>
    /// 从层级移除进程
    /// </summary>
    private void RemoveProcess(LevelItemViewModel? item)
    {
        if (item == null || string.IsNullOrWhiteSpace(item.ProcessName))
            return;

        _volumeController.RemoveProcessFromLevel(item.ProcessName, item.Level);
        RefreshLevelViews();
        StatusText = $"已从层级 {item.Level} 移除 {item.ProcessName}";
    }

    /// <summary>
    /// 将指定音频进程添加到指定层级（供拖拽调用）
    /// </summary>
    public void AddProcessToLevel(AudioProcess process, int level)
    {
        _volumeController.AddProcessToLevel(process.ProcessName, level);
        RefreshLevelViews();
        var levelVm = Levels.FirstOrDefault(l => l.Level == level);
        StatusText = $"已添加 {process.DisplayName} 到 {levelVm?.DisplayName ?? level.ToString()}";
    }

    /// <summary>
    /// 保存配置
    /// </summary>
    private void SaveConfig()
    {
        _configManager.SaveConfig();
        StatusText = "配置已保存";
    }

    /// <summary>
    /// 添加新层级
    /// </summary>
    private void AddLevel()
    {
        // 新层级放在列表末尾（最低优先级），使用极低的 Level 值确保排序后排最后
        var newLevelConfig = new LevelConfig(int.MinValue, $"新层级");
        _configManager.Config.Levels.Add(newLevelConfig);

        RenumberLevels();
        _configManager.SaveConfig();

        StatusText = $"已添加新层级";
    }

    /// <summary>
    /// 删除指定层级
    /// </summary>
    private void RemoveLevel(LevelViewModel? levelVm)
    {
        if (levelVm == null || Levels.Count <= 1)
        {
            StatusText = "至少需要保留一个层级";
            return;
        }

        string removedName = levelVm.DisplayName;

        // 从配置中移除
        var configToRemove = _configManager.Config.Levels
            .FirstOrDefault(l => l.Level == levelVm.Level);
        if (configToRemove != null)
        {
            // 将该层级的进程全部移除追踪
            foreach (var processName in configToRemove.ProcessNames)
            {
                _volumeController.RemoveProcessFromLevel(processName, levelVm.Level);
            }
            _configManager.Config.Levels.Remove(configToRemove);
        }

        RenumberLevels();
        _configManager.SaveConfig();

        StatusText = $"已删除 {removedName}";
    }

    /// <summary>
    /// 按列表位置重新编号所有层级（索引 0 → Level 0，索引 N → Level -N）
    /// 同步更新配置、VolumeController 追踪进程的 Level，以及 LevelIndex
    /// </summary>
    private void RenumberLevels()
    {
        var config = _configManager.Config;
        // 按当前 Level 降序排列后重新编号
        var sortedConfigs = config.Levels.OrderByDescending(l => l.Level).ToList();

        // 更新配置中的 Level 值和 DisplayName
        for (int i = 0; i < sortedConfigs.Count; i++)
        {
            var cfg = sortedConfigs[i];
            cfg.Level = -i;

            // 如果名称仍为默认模板（如 "层级 N" 或 "新层级"），则自动更新
            if (string.IsNullOrWhiteSpace(cfg.DisplayName) ||
                cfg.DisplayName.StartsWith("层级 ") ||
                cfg.DisplayName == "新层级")
            {
                cfg.DisplayName = i == 0 ? "层级 0 (最高优先级)" : $"层级 {cfg.Level}";
            }
        }

        // 重新加载 UI 层级集合
        LoadConfig();

        // 同步 VolumeController 中追踪进程的层级
        _volumeController.ReassignProcessLevels();

        // 刷新层级视图
        RefreshLevelViews();
    }

    /// <summary>
    /// 层级属性变更回调（DisplayName 或 SuppressRatio 改变时同步到配置）
    /// </summary>
    private void OnLevelPropertyUpdated(LevelViewModel levelVm, string propertyName)
    {
        var configLevel = _configManager.Config.Levels.FirstOrDefault(l => l.Level == levelVm.Level);
        if (configLevel == null) return;

        configLevel.DisplayName = levelVm.DisplayName;
        configLevel.SuppressVolumeRatio = levelVm.SuppressRatio;
        _configManager.SaveConfig();
    }

    /// <summary>
    /// 刷新层级视图
    /// </summary>
    private void RefreshLevelViews()
    {
        var config = _configManager.Config;
        foreach (var levelVm in Levels)
        {
            var levelConfig = config.Levels.FirstOrDefault(l => l.Level == levelVm.Level);
            if (levelConfig == null) continue;

            levelVm.Processes.Clear();
            foreach (var processName in levelConfig.ProcessNames)
            {
                // 查找该进程是否在追踪列表中
                var tracked = _volumeController.TrackedProcesses.Values
                    .FirstOrDefault(p => p.ProcessName.Equals(processName, StringComparison.OrdinalIgnoreCase));

                levelVm.Processes.Add(new LevelItemViewModel
                {
                    Level = levelVm.Level,
                    ProcessName = processName,
                    DisplayName = tracked?.DisplayName ?? processName,
                    IsPlaying = tracked?.IsPlaying ?? false,
                    IsSuppressed = tracked?.IsSuppressed ?? false,
                    CurrentVolume = tracked?.CurrentVolume ?? 1.0f,
                    OriginalVolume = tracked?.OriginalVolume ?? 1.0f,
                });
            }
        }
    }

    private void OnVolumeStateChanged()
    {
        App.Current.Dispatcher.Invoke(() =>
        {
            RefreshLevelViews();
        });
    }

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

/// <summary>
/// 层级视图模型
/// </summary>
public class LevelViewModel : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// 当属性变更时触发，用于通知 MainViewModel 同步配置
    /// </summary>
    public event Action<string>? PropertyUpdated;

    private int _level;
    public int Level
    {
        get => _level;
        set { _level = value; OnPropertyChanged(); }
    }

    private int _levelIndex;
    /// <summary>
    /// 层级在列表中的 0-based 索引，用于颜色转换器
    /// </summary>
    public int LevelIndex
    {
        get => _levelIndex;
        set { _levelIndex = value; OnPropertyChanged(); }
    }

    private string _displayName = string.Empty;
    public string DisplayName
    {
        get => _displayName;
        set { _displayName = value; OnPropertyChanged(); PropertyUpdated?.Invoke(nameof(DisplayName)); }
    }

    private float _suppressRatio = 0.2f;
    public float SuppressRatio
    {
        get => _suppressRatio;
        set { _suppressRatio = value; OnPropertyChanged(); OnPropertyChanged(nameof(SuppressRatioPercent)); PropertyUpdated?.Invoke(nameof(SuppressRatio)); }
    }

    /// <summary>
    /// 压制比例的百分比显示文本
    /// </summary>
    public string SuppressRatioPercent => $"{_suppressRatio:P0}";

    public ObservableCollection<LevelItemViewModel> Processes { get; } = new();

    private string _processCount = "(0 个进程)";
    public string ProcessCount
    {
        get => _processCount;
        private set { _processCount = value; OnPropertyChanged(); }
    }

    public LevelViewModel()
    {
        Processes.CollectionChanged += (_, _) =>
        {
            ProcessCount = $"({Processes.Count} 个进程)";
        };
    }

    public void OnPropertyChanged([CallerMemberName] string? name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}

/// <summary>
/// 层级中的进程项视图模型
/// </summary>
public class LevelItemViewModel : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    public int Level { get; set; }
    public string ProcessName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;

    private bool _isPlaying;
    public bool IsPlaying
    {
        get => _isPlaying;
        set { _isPlaying = value; OnPropertyChanged(); OnPropertyChanged(nameof(StatusText)); }
    }

    private bool _isSuppressed;
    public bool IsSuppressed
    {
        get => _isSuppressed;
        set { _isSuppressed = value; OnPropertyChanged(); OnPropertyChanged(nameof(StatusText)); }
    }

    private float _currentVolume;
    public float CurrentVolume
    {
        get => _currentVolume;
        set { _currentVolume = value; OnPropertyChanged(); OnPropertyChanged(nameof(VolumeText)); }
    }

    private float _originalVolume;
    public float OriginalVolume
    {
        get => _originalVolume;
        set { _originalVolume = value; OnPropertyChanged(); }
    }

    public string VolumeText => $"{CurrentVolume:P0}";
    public string StatusText => IsPlaying ? "🔊 播放中" : (IsSuppressed ? "🔇 被压制" : "🔈 静默");

    public void OnPropertyChanged([CallerMemberName] string? name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}

/// <summary>
/// 简单的 ICommand 实现
/// </summary>
public class RelayCommand : ICommand
{
    private readonly Action _execute;
    private readonly Func<bool>? _canExecute;

    public event EventHandler? CanExecuteChanged
    {
        add { CommandManager.RequerySuggested += value; }
        remove { CommandManager.RequerySuggested -= value; }
    }

    public RelayCommand(Action execute, Func<bool>? canExecute = null)
    {
        _execute = execute;
        _canExecute = canExecute;
    }

    public bool CanExecute(object? parameter) => _canExecute?.Invoke() ?? true;
    public void Execute(object? parameter) => _execute();
}

public class RelayCommand<T> : ICommand
{
    private readonly Action<T?> _execute;
    private readonly Func<T?, bool>? _canExecute;

    public event EventHandler? CanExecuteChanged
    {
        add { CommandManager.RequerySuggested += value; }
        remove { CommandManager.RequerySuggested -= value; }
    }

    public RelayCommand(Action<T?> execute, Func<T?, bool>? canExecute = null)
    {
        _execute = execute;
        _canExecute = canExecute;
    }

    public bool CanExecute(object? parameter)
    {
        if (parameter is T t)
            return _canExecute?.Invoke(t) ?? true;
        return _canExecute?.Invoke(default) ?? true;
    }

    public void Execute(object? parameter)
    {
        if (parameter is T t)
            _execute(t);
        else
            _execute(default);
    }
}
