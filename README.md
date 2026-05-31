# 🎚️ AdaptiveVolumeMixer

一个 Windows 桌面应用，根据应用优先级自动管理各程序的音量。当高优先级程序开始播放音频时，自动降低低优先级程序的音量——再也不用在多个程序间手动调整音量了。

## ✨ 功能

- **多层级优先级系统**：可自定义的优先级层级，层级越高越优先
- **自动音量压制**：上级程序播放音频时，下级程序音量自动降至可配置的比例
- **拖拽管理**：直接将音频进程拖入对应层级即可完成分配
- **实时状态显示**：显示每个进程的播放状态、当前音量和压制状态
- **持久化配置**：层级规则保存为 JSON 配置文件，重启后自动恢复
- **WPF MVVM 架构**：使用 CommunityToolkit.Mvvm，界面与逻辑解耦

## 🎯 工作原理

```text
层级 0 (最高优先级) ── 播放时不会被任何人压制
    │  压制
层级 -1 ────────────── 被层级 0 压制，音量降至 20%
    │  压制
层级 -2 ────────────── 被层级 0、-1 压制
    │  ...
```

当一个层级中的任意进程开始播放音频，所有低于它的层级中的进程音量将被降低至配置的比例（默认 20%）。播放停止后，音量自动恢复。

## 🛠️ 技术栈

| 技术 | 说明 |
| ------ | ------ |
| .NET 10.0 | 目标框架 `net10.0-windows` |
| WPF | Windows 桌面 UI 框架 |
| [NAudio](https://github.com/naudio/NAudio) 2.3.0 | Windows Core Audio API 封装 |
| [CommunityToolkit.Mvvm](https://github.com/CommunityToolkit/dotnet) 8.4.0 | MVVM 工具包（ObservableObject、RelayCommand 等） |
| Microsoft.Extensions.DependencyInjection | 依赖注入 |

## 🚀 快速开始

### 前置要求

- Windows 10/11
- [.NET 10.0 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- **以管理员身份运行**（访问 Windows 音频会话 API 需要管理员权限）

### 构建与运行

```bash
# 克隆仓库
git clone https://github.com/yourusername/adaptiveVolumeMixer.git
cd adaptiveVolumeMixer

# 构建
dotnet build

# 运行（建议以管理员身份）
dotnet run --project AdaptiveVolumeMixer
```

或在 Visual Studio 中打开 `adaptiveVolumeMixer.sln`，直接 F5 运行。

## ⚙️ 配置

配置文件位于 `bin/Debug/net10.0-windows/config.json`（构建后自动复制），首次运行会自动生成默认配置：

```json
{
  "Levels": [
    {
      "Level": 0,
      "DisplayName": "层级 0 (最高优先级)",
      "ProcessNames": ["music.exe", "video.exe"],
      "SuppressVolumeRatio": 0.2
    },
    {
      "Level": -1,
      "DisplayName": "层级 -1",
      "ProcessNames": [],
      "SuppressVolumeRatio": 0.2
    }
    // 层级数量可通过界面动态添加/删除，Level 值按列表位置自动编号
  ],
  "PollingIntervalMs": 1000
}
```

| 字段 | 说明 |
| ------ | ------ |
| `Level` | 层级编号，数值越大优先级越高，0 为最高优先级（支持动态层级） |
| `DisplayName` | 界面显示的层级名称 |
| `ProcessNames` | 该层级匹配的进程名（支持部分匹配） |
| `SuppressVolumeRatio` | 被压制时的目标音量比例（0.0 ~ 1.0） |
| `PollingIntervalMs` | 音频状态轮询间隔（毫秒） |

## 📁 项目结构

```text
AdaptiveVolumeMixer/
├── Models/
│   ├── AppConfig.cs          # 全局配置模型
│   ├── LevelConfig.cs        # 层级配置模型
│   └── AudioProcess.cs       # 音频进程模型
├── Services/
│   ├── AudioSessionManager.cs # NAudio 音频会话管理（类名 AudioSessionService）
│   ├── ConfigManager.cs      # JSON 配置文件读写
│   └── VolumeController.cs   # 音量压制核心逻辑
├── ViewModels/
│   ├── MainViewModel.cs      # 主界面 ViewModel
│   ├── LevelViewModel.cs     # 层级 ViewModel
│   └── LevelItemViewModel.cs # 层级内进程项 ViewModel
├── Converters/               # WPF 值转换器
├── MainWindow.xaml           # 主窗口界面
├── MainWindow.xaml.cs        # 主窗口交互逻辑（拖拽等）
└── App.xaml                  # 应用入口
```

## 📝 许可

MIT License

---
<!--markdownlint-disable MD033-->
> 🫠 **免责声明**：本项目完全由 AI（aka Vibe Coding）生成。
>
> <img src="66140cbe-fe23-4a77-8a38-02bfe697c945.png" alt="vibe coding" width="300" />
<!--markdownlint-enable MD033-->