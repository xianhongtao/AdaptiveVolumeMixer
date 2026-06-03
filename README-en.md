# 🎚️ AdaptiveVolumeMixer

> 🇺🇸 English | [🇨🇳 中文](README.md)

A Windows WPF desktop application that automatically manages program volumes based on application priority. When a higher-priority program starts playing audio, it automatically lowers the volume of lower-priority programs to a configurable target — no more manually adjusting volumes between multiple apps!

## ✨ Features

- **Multi-level Priority System**: Dynamically add/delete levels through the UI; higher level numbers = higher priority (0 is highest)
- **Automatic Volume Suppression**: When a higher-level app plays audio, lower-level volumes are automatically reduced to their configured target volume; volumes are restored when monitoring stops or higher-level playback ceases
- **Drag & Drop Assignment**: Drag processes from the available list on the right to a level on the left
- **Real-time Status**: Each process displays playback status (🔊 Playing / 🔇 Suppressed / 🔈 Silent) and current volume
- **Per-Level Target Volume**: Adjust via slider, takes effect immediately
- **Editable Level Names**: Click a level title to rename it
- **🌐 Internationalization**: Supports Chinese and English interfaces; language selection dialog on first launch with "remember" option
- **❌ Exit Choice**: Dialog when closing the window: "Exit" or "Minimize to Tray", with "remember" option
- **Persistent Configuration**: Level rules and process assignments saved as JSON config file, auto-restored on restart
- **WPF + MVVM Architecture**: Uses CommunityToolkit.Mvvm source generators; clean separation of UI and logic

## 🎯 How It Works

```text
Level  0  (Highest Priority) ── Always plays at full volume
    │  Suppresses
Level -1  ─────────────────── Suppressed by Level 0, volume drops to configured target
    │  Suppresses
Level -2  ─────────────────── Suppressed by Levels 0 and -1 (if either is playing)
    │  ...
Level -N  ─────────────────── Suppressed by all higher levels
```

When any process in a level starts playing audio, all processes in lower levels have their volume immediately set to their level's configured `SuppressVolumeRatio` target (default 20%). When higher-level playback stops, lower-level volumes are automatically restored to their original levels.

## 🛠️ Tech Stack

| Technology | Version | Description |
| ---------- | ------- | ----------- |
| .NET | `net10.0-windows` | Target framework |
| WPF | — | Windows desktop UI framework |
| [NAudio](https://github.com/naudio/NAudio) | 2.3.0 | Windows Core Audio API wrapper |
| [CommunityToolkit.Mvvm](https://github.com/CommunityToolkit/dotnet) | 8.4.0 | MVVM source generators (`[ObservableProperty]`, `[RelayCommand]`) |
| Microsoft.Extensions.DependencyInjection | 10.0.0 | DI container |

## 🚀 Quick Start

### Prerequisites

- Windows 10/11
- [.NET 10.0 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)

### Build & Run

```bash
git clone https://github.com/xianhongtao/AdaptiveVolumeMixer.git
cd AdaptiveVolumeMixer

# Build
dotnet build

# Run
dotnet run --project AdaptiveVolumeMixer
```

Or open `AdaptiveVolumeMixer.sln` in Visual Studio and press F5.

## ⚙️ Configuration

Configuration file `config.json` is auto-generated in the executable directory. If no config exists on first launch, a default config with 6 levels is created.

### Complete Configuration Reference

```json
{
  "Levels": [
    {
      "Level": 0,
      "DisplayName": "Level 0 (Highest Priority)",
      "ProcessNames": ["notepad.exe"],
      "SuppressVolumeRatio": 0.2
    }
  ],
  "PollingIntervalMs": 1000,
  "Language": null,
  "CloseAction": null
}
```

| Field | Type | Description |
| ----- | ---- | ----------- |
| `Levels` | `LevelConfig[]` | Level configuration list, sorted by `Level` descending |
| `Levels[].Level` | `int` | Level number; higher = higher priority (0 is highest). Auto-renumbered when levels are added/removed |
| `Levels[].DisplayName` | `string` | Display name shown in UI, editable directly |
| `Levels[].ProcessNames` | `string[]` | Process names for this level (with `.exe` extension, supports partial match, case-insensitive) |
| `Levels[].SuppressVolumeRatio` | `float` | Target volume when suppressed by higher levels, range 0.0 ~ 1.0 (default 0.2 = 20%) |
| `PollingIntervalMs` | `int` | Audio playback polling interval in milliseconds (default 1000ms) |
| `Language` | `string` / `null` | UI language: `null`=ask on every startup, `"zh"`=Chinese, `"en"`=English |
| `CloseAction` | `string` / `null` | Close behavior: `null`=ask every time, `"exit"`=quit directly, `"tray"`=minimize to tray |

> 💡 Language and close behavior can also be saved automatically via the "Remember choice" checkbox in their respective dialogs.

## 📁 Project Structure

```text
AdaptiveVolumeMixer/
├── Models/
│   ├── AppConfig.cs              # Global config model (levels + polling + language + close action)
│   ├── LevelConfig.cs            # Single level config (number, name, process list, target volume)
│   └── AudioProcess.cs           # Audio process model (PID, name, volume, playing/suppressed status)
├── Services/
│   ├── AudioSessionService.cs    # NAudio wrapper for enumerating/controlling Windows audio sessions
│   ├── ConfigManager.cs          # JSON config file I/O (System.Text.Json)
│   ├── VolumeController.cs       # Core volume suppression logic (polling → match levels → apply/restore)
│   └── LocalizationManager.cs    # Internationalization manager (singleton, detects/selects language on startup)
├── ViewModels/
│   ├── MainViewModel.cs          # Main window ViewModel (start/stop monitoring, add/delete levels, process assignment, config save)
│   ├── LevelViewModel.cs         # Level ViewModel (editable name, target volume slider, process count)
│   └── LevelItemViewModel.cs     # Process item ViewModel within a level (playback status, current volume)
├── Controls/
│   ├── LevelItemControl.xaml     # Level item user control (title bar + process list + empty state)
│   └── LevelItemControl.xaml.cs  # Level item interaction logic (process removal, drag & drop)
├── Converters/
│   ├── BoolToVisibilityConverter.cs         # bool → Visibility (true=Visible)
│   ├── InverseBoolToVisibilityConverter.cs  # bool → Visibility (true=Collapsed)
│   ├── LevelIndexToColorBrushConverter.cs   # Level index → color brush (10-color palette)
│   ├── NullToBoolConverter.cs               # Non-null → bool
│   └── LocExtension.cs                      # XAML i18n MarkupExtension ({l:Loc Key})
├── Resources/
│   ├── Strings.resx              # Chinese resources (default fallback language)
│   └── Strings.en.resx           # English resources
├── LanguageSelectionDialog.xaml  # Language selection dialog
├── ExitDialog.xaml               # Exit confirmation dialog
├── MainWindow.xaml               # Main window XAML layout (toolbar + left/right panels + status bar)
├── MainWindow.xaml.cs            # Main window interaction logic (close/minimize to tray, drag source detection)
├── App.xaml                      # App-level resources (converters, color brushes, shared styles)
└── App.xaml.cs                   # App entry point (DI container initialization, system tray, service registration)
```

### Core Class Relationships

```text
App.xaml.cs (DI Container)
  ├── AudioSessionService  ──→  NAudio Core Audio API
  ├── ConfigManager        ──→  config.json
  ├── VolumeController     ──→  AudioSessionService + ConfigManager
  │     └── MonitorLoop()     Background polling → RefreshProcesses() → ApplyVolumeRules()
  ├── MainViewModel        ──→  VolumeController + ConfigManager + AudioSessionService
  │     ├── Levels (ObservableCollection<LevelViewModel>)
  │     │     └── Processes (ObservableCollection<LevelItemViewModel>)
  │     └── AvailableProcesses (ObservableCollection<AudioProcess>)
  ├── LocalizationManager  ──→  Resources/Strings.resx / Strings.en.resx
  └── MainWindow           ──→  MainViewModel (DataContext)
        ├── Left: Level list (ItemsControl → LevelItemControl)
        │     └── LevelItemControl (drop target + process list + delete button)
        └── Right: Available processes list (drag source) + instructions
```

## 🖥️ User Guide

| Action | How To |
| ------ | ------ |
| Start/Stop monitoring | Click "Start Monitoring" / "Stop Monitoring" buttons in toolbar |
| Assign process to level | Drag a process from the right-side list to a level on the left |
| Remove process from level | Click the ✕ button on the process item |
| Adjust target volume | Drag the slider in each level's title bar (0% ~ 100%) |
| Edit level name | Click the level title text directly |
| Add new level | Click "Add Level" button in toolbar |
| Delete level | Click ✕ button on level title bar (disabled while monitoring, minimum 1 level) |
| Refresh process list | Click "Refresh Processes" button in toolbar |
| Save configuration | Click "Save Config" button (some operations auto-save) |
| Select language | Dialog on first launch — choose Chinese or English |
| Close window | Dialog asks "Exit" or "Minimize to Tray" |
| Restore from tray | Double-click tray icon or right-click → "Show Window" |
| Full exit | Right-click tray icon → "Exit" |

## 🏷️ Version History

See [Releases](https://github.com/xianhongtao/AdaptiveVolumeMixer/releases).

## 📝 License

MIT License

<!--markdownlint-disable MD033-->
> 🫠 **Disclaimer**: This project was entirely generated by AI (a.k.a. Vibe Coding).
>
> <img src="66140cbe-fe23-4a77-8a38-02bfe697c945.png" alt="vibe coding" width="300" />
<!--markdownlint-enable MD033-->
