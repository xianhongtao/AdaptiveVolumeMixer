using AdaptiveVolumeMixer.Models;

namespace AdaptiveVolumeMixer.Services;

/// <summary>
/// 音量控制核心逻辑
/// 当上级软件开始播放时，将下级音量降低到指定目标音量
/// </summary>
public class VolumeController : IDisposable
{
    private readonly AudioSessionService _audioSessionService;
    private readonly ConfigManager _configManager;
    private readonly Dictionary<int, AudioProcess> _trackedProcesses = new();
    private readonly object _lock = new();
    private CancellationTokenSource? _cts;
    private Task? _monitorTask;
    private bool _isRunning;

    /// <summary>
    /// 受追踪的进程列表
    /// </summary>
    public IReadOnlyDictionary<int, AudioProcess> TrackedProcesses => _trackedProcesses;

    /// <summary>
    /// 状态更新事件
    /// </summary>
    public event Action? OnStateChanged;

    private static bool IsProcessMatch(AudioProcess session, string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return false;

        if (session.ProcessName.Contains(token, StringComparison.OrdinalIgnoreCase) ||
            token.Contains(session.ProcessName, StringComparison.OrdinalIgnoreCase))
            return true;

        if (!string.IsNullOrWhiteSpace(session.DisplayName) &&
            (session.DisplayName.Contains(token, StringComparison.OrdinalIgnoreCase) ||
             token.Contains(session.DisplayName, StringComparison.OrdinalIgnoreCase)))
            return true;

        if (!string.IsNullOrWhiteSpace(session.MatchName) &&
            (session.MatchName.Contains(token, StringComparison.OrdinalIgnoreCase) ||
             token.Contains(session.MatchName, StringComparison.OrdinalIgnoreCase)))
            return true;

        return false;
    }

    public VolumeController(AudioSessionService audioSessionService, ConfigManager configManager)
    {
        _audioSessionService = audioSessionService;
        _configManager = configManager;
    }

    /// <summary>
    /// 启动监控
    /// </summary>
    public void Start()
    {
        if (_isRunning) return;
        _isRunning = true;

        _cts = new CancellationTokenSource();
        _monitorTask = Task.Run(() => MonitorLoop(_cts.Token));
    }

    /// <summary>
    /// 停止监控
    /// </summary>
    public void Stop()
    {
        _isRunning = false;
        _cts?.Cancel();
        _monitorTask?.Wait(2000);

        // 停止监控后恢复所有被压制的音量到原始值
        RestoreAllVolumes();
    }

    /// <summary>
    /// 恢复所有被压制进程的音量到原始值
    /// </summary>
    private void RestoreAllVolumes()
    {
        lock (_lock)
        {
            foreach (var process in _trackedProcesses.Values)
            {
                if (process.IsSuppressed)
                {
                    process.IsSuppressed = false;
                    process.CurrentVolume = process.OriginalVolume;
                    _audioSessionService.SetProcessVolume(process.ProcessId, process.OriginalVolume, process.DisplayName);
                }
            }
        }

        OnStateChanged?.Invoke();
    }

    /// <summary>
    /// 手动刷新进程列表
    /// </summary>
    public void RefreshProcesses()
    {
        var config = _configManager.Config;
        var allSessions = _audioSessionService.GetAllAudioSessions();

        lock (_lock)
        {
            // 标记所有现有进程为待移除
            var toRemove = new List<int>(_trackedProcesses.Keys);

            foreach (var session in allSessions)
            {
                // 检查该进程是否在配置的层级中
                int? matchedLevel = null;
                foreach (var level in config.Levels)
                {
                    if (level.ProcessNames.Any(p => IsProcessMatch(session, p)))
                    {
                        matchedLevel = level.Level;
                        break;
                    }
                }

                if (matchedLevel.HasValue)
                {
                    session.Level = matchedLevel.Value;
                    toRemove.Remove(session.ProcessId);

                    if (_trackedProcesses.TryGetValue(session.ProcessId, out var existing))
                    {
                        // 更新现有进程状态
                        existing.IsPlaying = session.IsPlaying;
                        existing.CurrentVolume = session.CurrentVolume;
                        if (!existing.IsSuppressed)
                        {
                            existing.OriginalVolume = session.CurrentVolume;
                        }
                    }
                    else
                    {
                        // 添加新进程
                        _trackedProcesses[session.ProcessId] = session;
                    }
                }
            }

            // 移除已退出的进程
            foreach (var pid in toRemove)
            {
                _trackedProcesses.Remove(pid);
            }
        }

        OnStateChanged?.Invoke();
    }

    /// <summary>
    /// 监控循环
    /// </summary>
    private async Task MonitorLoop(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            try
            {
                RefreshProcesses();
                ApplyVolumeRules();
                await Task.Delay(_configManager.Config.PollingIntervalMs, token);
            }
            catch (TaskCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"监控循环异常: {ex.Message}");
                await Task.Delay(2000, CancellationToken.None);
            }
        }
    }

    /// <summary>
    /// 应用音量规则
    /// 核心逻辑：如果某个层级有软件正在播放，则所有下级音量降至各层级配置的目标音量
    /// </summary>
    public void ApplyVolumeRules()
    {
        var config = _configManager.Config;
        var levels = config.Levels.OrderByDescending(l => l.Level).ToList();

        lock (_lock)
        {
            // 先实时刷新所有追踪进程的播放状态
            foreach (var process in _trackedProcesses.Values)
            {
                var state = _audioSessionService.GetProcessPlayingState(process.ProcessId, process.DisplayName);
                if (state.HasValue)
                {
                    process.IsPlaying = state.Value;
                }
            }

            // 检查每个层级是否有进程正在播放
            var playingLevels = new HashSet<int>();
            foreach (var process in _trackedProcesses.Values)
            {
                if (process.IsPlaying)
                {
                    playingLevels.Add(process.Level);
                }
            }

            // 如果有上级正在播放，则下级需要被压制
            foreach (var process in _trackedProcesses.Values)
            {
                var suppressingLevels = new List<int>();
                float targetVolume = process.OriginalVolume;
                bool wasSuppressed = process.IsSuppressed;

                foreach (var upperLevel in playingLevels)
                {
                    // 层级数值越大优先级越高：0 > -1 > -2 > -3 > -4 > -5
                    if (upperLevel > process.Level)
                    {
                        suppressingLevels.Add(upperLevel);
                    }
                }

                bool shouldSuppress = suppressingLevels.Count > 0;

                if (shouldSuppress)
                {
                    // 找到对应的层级配置，获取目标音量（0-1）
                    var levelConfig = levels.FirstOrDefault(l => l.Level == process.Level);
                    float suppressRatio = levelConfig?.SuppressVolumeRatio ?? 0.2f;
                    targetVolume = Math.Clamp(suppressRatio, 0.0f, 1.0f);
                }

                // 始终应用音量（移除阈值判断，确保每次都能生效）
                if (shouldSuppress)
                {
                    process.CurrentVolume = targetVolume;
                    process.IsSuppressed = true;
                    _audioSessionService.SetProcessVolume(process.ProcessId, targetVolume, process.DisplayName);
                }
                else
                {
                    process.IsSuppressed = false;
                    if (wasSuppressed)
                    {
                        process.CurrentVolume = targetVolume;
                        _audioSessionService.SetProcessVolume(process.ProcessId, targetVolume, process.DisplayName);
                    }
                }

            }
        }

        OnStateChanged?.Invoke();
    }

    /// <summary>
    /// 手动设置进程的原始音量
    /// </summary>
    public void SetProcessOriginalVolume(int processId, float volume)
    {
        lock (_lock)
        {
            if (_trackedProcesses.TryGetValue(processId, out var process))
            {
                process.OriginalVolume = Math.Clamp(volume, 0.0f, 1.0f);
                if (!process.IsSuppressed)
                {
                    process.CurrentVolume = process.OriginalVolume;
                    _audioSessionService.SetProcessVolume(processId, process.OriginalVolume, process.DisplayName);
                }
            }
        }
        OnStateChanged?.Invoke();
    }

    /// <summary>
    /// 手动添加进程到指定层级
    /// </summary>
    public void AddProcessToLevel(string processName, int level)
    {
        var config = _configManager.Config;
        var levelConfig = config.Levels.FirstOrDefault(l => l.Level == level);
        if (levelConfig != null)
        {
            if (!levelConfig.ProcessNames.Contains(processName, StringComparer.OrdinalIgnoreCase))
            {
                levelConfig.ProcessNames.Add(processName);
                _configManager.SaveConfig();
                RefreshProcesses();
            }
        }
    }

    /// <summary>
    /// 从层级中移除进程
    /// </summary>
    public void RemoveProcessFromLevel(string processName, int level)
    {
        var config = _configManager.Config;
        var levelConfig = config.Levels.FirstOrDefault(l => l.Level == level);
        if (levelConfig != null)
        {
            levelConfig.ProcessNames.RemoveAll(p =>
                p.Equals(processName, StringComparison.OrdinalIgnoreCase));
            _configManager.SaveConfig();
            RefreshProcesses();
        }
    }

    /// <summary>
    /// 根据当前配置重新匹配所有追踪进程的层级（用于层级重编号后同步）
    /// </summary>
    public void ReassignProcessLevels()
    {
        var config = _configManager.Config;
        lock (_lock)
        {
            foreach (var process in _trackedProcesses.Values)
            {
                int? matchedLevel = null;
                foreach (var level in config.Levels)
                {
                    if (level.ProcessNames.Any(p => IsProcessMatch(process, p)))
                    {
                        matchedLevel = level.Level;
                        break;
                    }
                }

                if (matchedLevel.HasValue)
                    process.Level = matchedLevel.Value;
            }
        }
        OnStateChanged?.Invoke();
    }

    public void Dispose()
    {
        Stop();
        _audioSessionService.Dispose();
    }
}
