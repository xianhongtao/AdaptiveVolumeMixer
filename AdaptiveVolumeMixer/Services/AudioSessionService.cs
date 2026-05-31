using System.Diagnostics;
using AdaptiveVolumeMixer.Models;
using NAudio.CoreAudioApi;
using NAudio.CoreAudioApi.Interfaces;

namespace AdaptiveVolumeMixer.Services;

/// <summary>
/// 音频会话管理器，使用 NAudio 封装 Windows Core Audio API
/// </summary>
public class AudioSessionService : IDisposable
{
    private MMDeviceEnumerator? _deviceEnumerator;
    private MMDevice? _defaultDevice;
    private NAudio.CoreAudioApi.AudioSessionManager? _sessionManager;
    private bool _disposed;
    private const float PlaybackThreshold = 0.001f;

    private static bool IsSessionPlaying(AudioSessionControl session)
    {
        try
        {
            if (session.AudioMeterInformation != null)
            {
                return session.AudioMeterInformation.MasterPeakValue > PlaybackThreshold;
            }
        }
        catch
        {
            // ignore meter errors and fall back to state
        }

        return session.State == AudioSessionState.AudioSessionStateActive;
    }

    /// <summary>
    /// 初始化音频会话管理器
    /// </summary>
    public bool Initialize()
    {
        try
        {
            _deviceEnumerator = new MMDeviceEnumerator();
            _defaultDevice = _deviceEnumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
            _sessionManager = _defaultDevice.AudioSessionManager;
            return _sessionManager != null;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"初始化音频管理器失败: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 获取所有音频会话的进程信息
    /// </summary>
    public List<AudioProcess> GetAllAudioSessions()
    {
        var result = new List<AudioProcess>();

        if (_sessionManager == null)
            return result;

        try
        {
            _sessionManager.RefreshSessions();
            var sessions = _sessionManager.Sessions;
            if (sessions == null)
                return result;

            for (int i = 0; i < sessions.Count; i++)
            {
                try
                {
                    var session = sessions[i];
                    if (session == null)
                        continue;

                    uint processIdUint = session.GetProcessID;
                    if (processIdUint == 0)
                        continue;

                    int processId = (int)processIdUint;

                    try
                    {
                        var process = Process.GetProcessById(processId);
                        string processName = process.ProcessName;
                        string processExeName = processName + ".exe";
                        string displayName = string.IsNullOrWhiteSpace(session.DisplayName)
                            ? processName
                            : session.DisplayName;
                        string matchName = !string.IsNullOrWhiteSpace(displayName) &&
                                           !displayName.Equals(processName, StringComparison.OrdinalIgnoreCase) &&
                                           !displayName.Equals(processExeName, StringComparison.OrdinalIgnoreCase)
                            ? displayName
                            : processExeName;

                        var audioProcess = new AudioProcess
                        {
                            ProcessId = processId,
                            ProcessName = processExeName,
                            DisplayName = displayName,
                            MatchName = matchName,
                            CurrentVolume = session.SimpleAudioVolume.Volume,
                            OriginalVolume = session.SimpleAudioVolume.Volume,
                            IsPlaying = IsSessionPlaying(session),
                        };

                        result.Add(audioProcess);
                    }
                    catch (ArgumentException)
                    {
                        // 进程已退出
                        continue;
                    }
                }
                catch
                {
                    continue;
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"获取音频会话失败: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// 设置指定进程的音量
    /// </summary>
    public bool SetProcessVolume(int processId, float volume, string? displayName = null)
    {
        if (_sessionManager == null)
            return false;

        try
        {
            var sessions = _sessionManager.Sessions;
            if (sessions == null)
                return false;

            bool anySet = false;
            volume = Math.Clamp(volume, 0.0f, 1.0f);

            for (int i = 0; i < sessions.Count; i++)
            {
                try
                {
                    var session = sessions[i];
                    if (session == null)
                        continue;

                    bool matchesProcess = (int)session.GetProcessID == processId;
                    bool matchesDisplay = !string.IsNullOrWhiteSpace(displayName) &&
                                          !string.IsNullOrWhiteSpace(session.DisplayName) &&
                                          session.DisplayName.Equals(displayName, StringComparison.OrdinalIgnoreCase);

                    if (matchesProcess || matchesDisplay)
                    {
                        session.SimpleAudioVolume.Volume = volume;
                        anySet = true;
                    }
                }
                catch
                {
                    continue;
                }
            }

            return anySet;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"设置进程音量失败: {ex.Message}");
        }

        return false;
    }

    /// <summary>
    /// 获取指定进程的播放状态
    /// </summary>
    public bool? GetProcessPlayingState(int processId, string? displayName = null)
    {
        if (_sessionManager == null)
            return null;

        try
        {
            var sessions = _sessionManager.Sessions;
            if (sessions == null)
                return null;

            bool? matchedState = null;

            for (int i = 0; i < sessions.Count; i++)
            {
                try
                {
                    var session = sessions[i];
                    if (session == null)
                        continue;

                    bool matchesProcess = (int)session.GetProcessID == processId;
                    bool matchesDisplay = !string.IsNullOrWhiteSpace(displayName) &&
                                          !string.IsNullOrWhiteSpace(session.DisplayName) &&
                                          session.DisplayName.Equals(displayName, StringComparison.OrdinalIgnoreCase);

                    if (matchesProcess || matchesDisplay)
                    {
                        bool isPlaying = IsSessionPlaying(session);
                        if (isPlaying)
                            return true;
                        matchedState = false;
                    }
                }
                catch
                {
                    continue;
                }
            }

            return matchedState;
        }
        catch
        {
            // ignore
        }

        return null;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _sessionManager?.Dispose();
        _defaultDevice?.Dispose();
        _deviceEnumerator?.Dispose();
    }
}
