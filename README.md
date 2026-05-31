# 🎚️ AdaptiveVolumeMixer

一个 Windows WPF 桌面应用，根据应用优先级自动管理各程序的音量。当高优先级程序开始播放音频时，自动将低优先级程序的音量降低到可配置的比例——再也不用在多个程序间手动调音量了。

## ✨ 功能

- **多层级优先级系统**：通过界面动态添加/删除层级，层级数值越大优先级越高（0 为最高优先级）
- **自动音量压制**：上级程序播放音频时，下级程序音量自动降至该层级配置的压制比例
- **拖拽分配**：从右侧可用进程列表直接拖拽到左侧层级即可完成分配
- **实时状态显示**：每个进程显示播放状态（🔊播放中 / 🔇被压制 / 🔈静默）和当前音量
- **每层级独立压制比例**：通过滑块调整，实时生效
- **可编辑层级名称**：点击层级标题即可重命名
- **持久化配置**：层级规则和进程分配保存为 JSON 配置文件，重启后自动恢复
- **WPF + MVVM 架构**：使用 CommunityToolkit.Mvvm 源生成器，界面与逻辑解耦

## 🎯 工作原理

```text
层级  0  (最高优先级) ── 播放时不被任何人压制
    │  压制
层级 -1  ─────────────── 被层级 0 压制，音量降至本层级配置的压制比例
    │  压制
层级 -2  ─────────────── 被层级 0、-1 压制（任一上级播放即压制）
    │  ...
层级 -N  ─────────────── 被所有上级层级压制
```

当某层级中任意进程开始播放音频，所有低于该层级的进程音量立即降低至各自层级配置的 `SuppressVolumeRatio` 比例（默认 20%）。上级播放停止后，下级音量自动恢复至原始音量。

## 🛠️ 技术栈

| 技术 | 版本 | 说明 |
| ------ | ------ | ------ |
| .NET | `net10.0-windows` | 目标框架 |
| WPF | — | Windows 桌面 UI 框架 |
| [NAudio](https://github.com/naudio/NAudio) | 2.3.0 | Windows Core Audio API 封装 |
| [CommunityToolkit.Mvvm](https://github.com/CommunityToolkit/dotnet) | 8.4.0 | MVVM 源生成器（`[ObservableProperty]`、`[RelayCommand]`） |
| Microsoft.Extensions.DependencyInjection | 10.0.0 | 依赖注入容器 |

## 🚀 快速开始

### 前置要求

- Windows 10/11
- [.NET 10.0 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- **以管理员身份运行**（访问 Windows Core Audio API 需要管理员权限）

### 构建与运行

```bash
git clone https://github.com/xianhongtao/AdaptiveVolumeMixer.git
cd AdaptiveVolumeMixer

# 构建
dotnet build

# 运行（以管理员身份）
dotnet run --project AdaptiveVolumeMixer
```

或在 Visual Studio 中打开 `AdaptiveVolumeMixer.sln`，右键"以管理员身份运行"后 F5 启动。

## ⚙️ 配置

配置文件自动生成于可执行文件同目录下的 `config.json`。首次运行无配置文件时，自动创建包含 6 个默认层级的配置。

### 默认配置示例

```json
{
  "Levels": [
    {
      "Level": 0,
      "DisplayName": "层级 0 (最高优先级)",
      "ProcessNames": [],
      "SuppressVolumeRatio": 0.2
    },
    {
      "Level": -1,
      "DisplayName": "层级 -1",
      "ProcessNames": [],
      "SuppressVolumeRatio": 0.2
    }
    // ... 共 6 个默认层级（0 到 -5），可通过界面动态增减
  ],
  "PollingIntervalMs": 1000
}
```

### 配置字段说明

| 字段 | 类型 | 说明 |
| ------ | ------ | ------ |
| `Levels` | `LevelConfig[]` | 层级配置列表，按 `Level` 降序排列 |
| `Levels[].Level` | `int` | 层级编号，数值越大优先级越高（0 为最高优先级）。层级数量通过界面动态增减后，`Level` 值自动按列表位置重新编号（首位为 0，依次 -1, -2, ...） |
| `Levels[].DisplayName` | `string` | 界面中显示的层级名称，可通过界面直接编辑 |
| `Levels[].ProcessNames` | `string[]` | 该层级匹配的进程名列表（含 `.exe` 后缀，支持部分匹配，不区分大小写） |
| `Levels[].SuppressVolumeRatio` | `float` | 被上级压制时的目标音量比例，取值范围 0.0 ~ 1.0（默认 0.2 = 20%） |
| `PollingIntervalMs` | `int` | 音频播放状态轮询间隔（毫秒），默认 1000ms |

## 📁 项目结构

```text
AdaptiveVolumeMixer/
├── Models/
│   ├── AppConfig.cs              # 全局配置模型（层级列表 + 轮询间隔）
│   ├── LevelConfig.cs            # 单个层级配置（编号、名称、进程列表、压制比例）
│   └── AudioProcess.cs           # 音频进程模型（PID、名称、音量、播放/压制状态）
├── Services/
│   ├── AudioSessionService.cs    # NAudio 封装，枚举和控制 Windows 音频会话
│   ├── ConfigManager.cs          # JSON 配置文件读写（System.Text.Json）
│   └── VolumeController.cs       # 音量压制核心逻辑（后台轮询 → 匹配层级 → 应用压制）
├── ViewModels/
│   ├── MainViewModel.cs          # 主界面 ViewModel（监控启停、层级增删、进程分配、配置保存）
│   ├── LevelViewModel.cs         # 层级 ViewModel（可编辑名称、压制比例滑块、进程计数）
│   └── LevelItemViewModel.cs     # 层级内进程项 ViewModel（播放状态、当前音量）
├── Converters/
│   ├── BoolToVisibilityConverter.cs         # bool → Visibility（true=Visible）
│   ├── InverseBoolToVisibilityConverter.cs  # bool → Visibility（true=Collapsed）
│   ├── LevelIndexToColorBrushConverter.cs   # 层级索引 → 颜色画刷（10 色调色板）
│   └── NullToBoolConverter.cs               # 非空值 → bool
├── MainWindow.xaml               # 主窗口 XAML 布局
├── MainWindow.xaml.cs            # 主窗口交互逻辑（拖拽启动检测、Drop 事件处理）
├── App.xaml                      # 应用资源（全局转换器、状态颜色画笔）
└── App.xaml.cs                   # 应用入口（DI 容器初始化、服务注册）
```

### 核心类关系

```text
App.xaml.cs (DI Container)
  ├── AudioSessionService  ──→  NAudio Core Audio API
  ├── ConfigManager        ──→  config.json
  ├── VolumeController     ──→  AudioSessionService + ConfigManager
  │     └── MonitorLoop()     后台轮询 → RefreshProcesses() → ApplyVolumeRules()
  ├── MainViewModel        ──→  VolumeController + ConfigManager + AudioSessionService
  │     ├── Levels (ObservableCollection<LevelViewModel>)
  │     │     └── Processes (ObservableCollection<LevelItemViewModel>)
  │     └── AvailableProcesses (ObservableCollection<AudioProcess>)
  └── MainWindow           ──→  MainViewModel (DataContext)
        ├── 左侧：层级列表 + 进程项（拖拽放置目标）
        └── 右侧：可用进程列表（拖拽源） + 使用说明
```

## 🖥️ 界面操作指南

| 操作 | 方式 |
| ------ | ------ |
| 启动/停止自动音量监控 | 点击顶部工具栏「启动监控」/「停止监控」按钮 |
| 将进程分配到层级 | 从右侧「可用音频进程」列表拖拽进程到左侧目标层级区域 |
| 从层级移除进程 | 点击进程项右侧的 ✕ 按钮 |
| 调整压制比例 | 拖动每个层级标题栏中的滑块（0% ~ 100%） |
| 编辑层级名称 | 直接点击层级标题文本进行编辑 |
| 添加新层级 | 点击顶部工具栏「添加层级」按钮 |
| 删除层级 | 点击层级标题栏右侧的 ✕ 按钮（至少保留 1 个层级） |
| 刷新进程列表 | 点击顶部工具栏「刷新进程」按钮 |
| 保存配置 | 点击顶部工具栏「保存配置」按钮（部分操作会自动保存） |

## 📝 许可

MIT License
<!--markdownlint-disable MD033-->
> 🫠 **免责声明**：本项目完全由 AI（aka Vibe Coding）生成。
>
> <img src="66140cbe-fe23-4a77-8a38-02bfe697c945.png" alt="vibe coding" width="300" />
<!--markdownlint-enable MD033-->