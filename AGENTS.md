# AdaptiveVolumeMixer — Agent Instructions

> A Windows WPF desktop app that auto-manages per-app volume via priority levels.
> See [README.md](README.md) (中文) or [README-en.md](README-en.md) (English) for full product overview.

## Build & Run

```bash
dotnet build          # Build the solution
dotnet run --project AdaptiveVolumeMixer   # Run from CLI
```

Target: `net10.0-windows`, WPF + WinForms. No test project exists.

## Tech Stack

| Component | Detail |
|---|---|
| .NET | `net10.0-windows` |
| UI | WPF (`UseWPF`) + WinForms (`UseWindowsForms` for NotifyIcon) |
| Audio | NAudio 2.3.0 (CoreAudioApi) |
| MVVM | CommunityToolkit.Mvvm 8.4.0 (source generators) |
| DI | Microsoft.Extensions.DependencyInjection 10.0 |

## Architecture

```
Models/          POCO data (AppConfig, LevelConfig, AudioProcess) — no logic
ViewModels/      MVVM using CommunityToolkit.Mvvm source generators
Services/        Singleton services injected via DI (App.xaml.cs ConfigureServices)
Controls/        UserControl (LevelItemControl — drag-drop target)
Converters/      IValueConverter + LocExtension (markup extension for i18n)
Resources/       .resx files: Strings.resx (zh-CN default), Strings.en.resx (en)
```

All services are **singletons** registered in `App.xaml.cs` → `ConfigureServices()`. The composition root resolves `MainWindow` from the container.

## Key Conventions

### CommunityToolkit.Mvvm Source Generators (NOT manual INotifyPropertyChanged)

- `[ObservableProperty]` on a `private` field → generates a `public` property with `OnXxxChanged` partial method.
- `[RelayCommand]` on a `private` method → generates a `public` `XxxCommand`.
- `[NotifyPropertyChangedFor]` / `[NotifyCanExecuteChangedFor]` for cascading notifications.
- ViewModels are `partial` classes.

**Never** manually implement `INotifyPropertyChanged` or create `RelayCommand` instances by hand. Always use the source generators.

### Localization

Uses a custom `LocExtension : MarkupExtension`:

```xml
xmlns:l="clr-namespace:AdaptiveVolumeMixer.Converters"
Text="{l:Loc Window.Title}"
```

This is **one-shot** — evaluated at XAML load time. Runtime language switching requires an app restart.

`LocalizationManager` is initialized **before** the DI container (in `App.OnStartup`), because `AppConfig.CreateDefault()` calls it for default level names. Keep this dependency order in mind.

### Configuration

`config.json` in the executable directory. Managed by `ConfigManager` — plain `File.ReadAllText` + `System.Text.Json`, no `IConfiguration`/`IOptions`. Saved immediately on every change.

## Critical Pitfalls

### 1. WinForms/WPF Type Name Conflicts

`UseWindowsForms` causes collisions: `Application`, `Button`, `DataObject`, `DragDrop`, `DragDropEffects`, `DragEventArgs`, `ListBox`, `MouseEventArgs`, `Point`, `Color`, `SolidColorBrush`.

Solution: explicit `using` aliases in affected files:

- `MainWindow.xaml.cs`: WPF aliases for `Application`, `Button`, `DataObject`, `DragDrop`, `DragDropEffects`, `DragEventArgs`, `ListBox`, `MouseEventArgs`, `Point`, and related types.
- `LevelItemControl.xaml.cs`: similar aliases.
- `LevelIndexToColorBrushConverter.cs`: aliases for `Color` and `SolidColorBrush`.

**When adding any new file**, check for ambiguous type references. See [system-tray.md](/.memories/repo/system-tray.md) for full details.

### 2. Same-Name Processes

Processes are tracked by **PID**, not just process name. Use `VolumeController.TrackedProcesses` (PID-keyed dictionary) and `RemoveProcessById(int)` for precise removal. See [same-name-process-fix.md](/.memories/repo/same-name-process-fix.md).

### 3. SuppressVolumeRatio is Absolute

`SuppressVolumeRatio` is an **absolute target** (0.0–1.0), not a multiplier. Do not compute `original × ratio` — apply the ratio value directly as the target volume.

### 4. Thread Safety

`VolumeController` uses `lock (_lock)` for all shared state. UI updates from background threads go through `App.Current.Dispatcher.Invoke()`.

## File Map

| Directory | Key Files | Purpose |
|---|---|---|
| Root | `App.xaml.cs` | DI setup, tray icon, lifecycle |
| `Models/` | `AppConfig.cs`, `LevelConfig.cs`, `AudioProcess.cs` | Data POCOs |
| `ViewModels/` | `MainViewModel.cs` | App state, commands, orchestration |
| | `LevelViewModel.cs` | Per-level UI state |
| | `LevelItemViewModel.cs` | Per-process UI state |
| `Services/` | `AudioSessionService.cs` | NAudio Core Audio API wrapper |
| | `VolumeController.cs` | Monitor loop + volume rules engine |
| | `ConfigManager.cs` | JSON config read/write |
| | `LocalizationManager.cs` | Singleton i18n manager |
| `Controls/` | `LevelItemControl.xaml/.cs` | Level UI container + drag-drop target |
| `Converters/` | `LocExtension.cs` | XAML i18n markup extension |
| | Various `*Converter.cs` | IValueConverter implementations |
| `Resources/` | `Strings.resx`, `Strings.en.resx` | Localized strings |
