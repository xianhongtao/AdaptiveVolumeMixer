using System.Windows;
using System.Windows.Forms;
using Microsoft.Extensions.DependencyInjection;
using AdaptiveVolumeMixer.Services;
using AdaptiveVolumeMixer.ViewModels;
using Application = System.Windows.Application;

namespace AdaptiveVolumeMixer;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    private ServiceProvider? _serviceProvider;
    private NotifyIcon? _notifyIcon;
    private MainWindow? _mainWindow;
    private bool _showBalloonOnce = true;

    protected override void OnStartup(StartupEventArgs e)
    {
        // 读取配置中的语言设置
        string? configLanguage = null;
        var configPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.json");
        try
        {
            if (System.IO.File.Exists(configPath))
            {
                var config = System.Text.Json.JsonSerializer.Deserialize<Models.AppConfig>(
                    System.IO.File.ReadAllText(configPath),
                    new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                configLanguage = config?.Language;
            }
        }
        catch { }

        // 如果语言未配置，弹出选择窗口
        if (string.IsNullOrWhiteSpace(configLanguage))
        {
            var dialog = new LanguageSelectionDialog();
            if (dialog.ShowDialog() == true)
            {
                configLanguage = dialog.SelectedLanguage;

                // 如果勾选了"记住选择"，写入 config.json
                if (dialog.RememberChoice)
                {
                    try
                    {
                        var config = System.IO.File.Exists(configPath)
                            ? System.Text.Json.JsonSerializer.Deserialize<Models.AppConfig>(
                                System.IO.File.ReadAllText(configPath),
                                new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                            : Models.AppConfig.CreateDefault();

                        if (config != null)
                        {
                            config.Language = configLanguage;
                            System.IO.File.WriteAllText(configPath,
                                System.Text.Json.JsonSerializer.Serialize(config,
                                    new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
                        }
                    }
                    catch { }
                }
            }
        }

        // 初始化本地化（必须在 DI 容器构建之前）
        LocalizationManager.Instance.Initialize(configLanguage);

        var services = new ServiceCollection();
        ConfigureServices(services);
        _serviceProvider = services.BuildServiceProvider();

        _mainWindow = _serviceProvider.GetRequiredService<MainWindow>();

        // 初始化系统托盘图标
        InitNotifyIcon();

        _mainWindow.Show();
    }

    private void InitNotifyIcon()
    {
        _notifyIcon = new NotifyIcon
        {
            Icon = System.Drawing.Icon.ExtractAssociatedIcon(
                System.Windows.Forms.Application.ExecutablePath),
            Visible = true,
            Text = LocalizationManager.Instance.GetString("App.Title"),
            ContextMenuStrip = new ContextMenuStrip()
        };

        // 菜单项
        var showItem = new ToolStripMenuItem(LocalizationManager.Instance.GetString("Tray.ShowWindow"));
        showItem.Click += (_, _) => ShowMainWindow();

        var exitItem = new ToolStripMenuItem(LocalizationManager.Instance.GetString("Tray.Exit"));
        exitItem.Click += (_, _) => ExitApplication();

        _notifyIcon.ContextMenuStrip.Items.Add(showItem);
        _notifyIcon.ContextMenuStrip.Items.Add(new ToolStripSeparator());
        _notifyIcon.ContextMenuStrip.Items.Add(exitItem);

        // 双击托盘图标恢复窗口
        _notifyIcon.DoubleClick += (_, _) => ShowMainWindow();
    }

    /// <summary>
    /// 从托盘恢复主窗口
    /// </summary>
    private void ShowMainWindow()
    {
        if (_mainWindow == null) return;

        _mainWindow.Show();
        _mainWindow.WindowState = WindowState.Normal;
        _mainWindow.Activate();

        // 在 Win10/11 中，窗口可能需要 ShowInTaskbar 切换才能在任务栏闪现
        _mainWindow.ShowInTaskbar = false;
        _mainWindow.ShowInTaskbar = true;
    }

    /// <summary>
    /// 显示首次隐藏到托盘的气泡提示（内部方法，供 MainWindow_Closing 调用）
    /// </summary>
    internal void ShowMinimizeBalloonTip()
    {
        if (_showBalloonOnce && _notifyIcon != null)
        {
            _showBalloonOnce = false;
            _notifyIcon.ShowBalloonTip(
                3000,
                LocalizationManager.Instance.GetString("Tray.BalloonTitle"),
                LocalizationManager.Instance.GetString("Tray.BalloonText"),
                ToolTipIcon.Info);
        }
    }

    /// <summary>
    /// 显示"关闭行为已保存"的气泡提示
    /// </summary>
    internal void ShowCloseActionSavedBalloonTip()
    {
        if (_notifyIcon != null)
        {
            _notifyIcon.ShowBalloonTip(
                5000,
                LocalizationManager.Instance.GetString("ExitDialog.SavedTitle"),
                LocalizationManager.Instance.GetString("ExitDialog.SavedText"),
                ToolTipIcon.Info);
        }
    }

    /// <summary>
    /// 从托盘彻底退出应用
    /// </summary>
    private void ExitApplication()
    {
        if (_mainWindow != null)
        {
            _mainWindow.AllowClose = true;
        }
        Shutdown();
    }

    private static void ConfigureServices(ServiceCollection services)
    {
        services.AddSingleton(LocalizationManager.Instance);
        services.AddSingleton<AudioSessionService>();
        services.AddSingleton<ConfigManager>();
        services.AddSingleton<VolumeController>();
        services.AddSingleton<MainViewModel>();
        services.AddSingleton<MainWindow>();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        // 释放托盘图标
        if (_notifyIcon != null)
        {
            _notifyIcon.Visible = false;
            _notifyIcon.Dispose();
            _notifyIcon = null;
        }

        _serviceProvider?.Dispose();
        base.OnExit(e);
    }
}

