# Copilot Instructions for keyboard-auto-switcher

## Project Overview

A Windows-native GUI application with system tray that automatically switches keyboard layouts (Dvorak ↔ AZERTY) when a TypeMatrix USB keyboard is connected/disconnected. Runs in user session without admin privileges. Features auto-updates via Velopack and GitHub Releases.

## Architecture

### Core Components

**KeyboardLayout.cs** - Win32 API wrapper for keyboard operations

- P/Invoke declarations for `user32.dll` functions (`GetKeyboardLayout`, `ActivateKeyboardLayout`, `GetKeyboardLayoutList`)
- Caches available layouts for performance
- Converts between IntPtr handles and KeyboardLayoutConfig objects
- Note: Layout IDs are 32-bit values where lower 16 bits are language ID, upper bits encode variant (e.g., `0xF0020409` for Dvorak)

**KeyboardLayoutConfig.cs** - Configuration data model

- Immutable config: `CultureName`, `LayoutId`, `DisplayName`
- Provides lookup methods: `GetByCultureName()`, `GetByLayoutId()`, `GetByLanguageId()`
- **Critical Detail**: Language ID fallback is intentionally disabled for en-US to avoid matching QWERTY instead of Dvorak
- Predefined layouts: `UsDvorak`, `FrenchStandard`

**IUSBDeviceDetector.cs** - Interface for USB device detection

- Abstraction layer for USB monitoring (allows mocking in tests)
- `IsTargetKeyboardConnected()`, `StartMonitoring()`, `StopMonitoring()`
- `DeviceChanged` event with `USBDeviceEventArgs`

**USBDeviceDetector.cs** - USB device monitoring implementation

- Implements `IUSBDeviceDetector` interface
- Detects TypeMatrix via hardcoded VID/PID: `USB\VID_1E54&PID_2030\`
- Uses WMI queries (`Win32_USBHub`, `__InstanceOperationEvent`)
- Provides event-driven detection via `DeviceChanged` event

### Services

**Services/KeyboardSwitcherWorker.cs** - Main business logic (BackgroundService)

- **Event-driven architecture**: Responds to USB events, power mode changes, session locks/unlocks
- Fallback to polling if WMI event monitoring fails
- Delay logic: 2000ms after resume, 500ms after unlock (allows system to fully restore)
- Core decision: `CheckAndSwitchLayout()` - activates Dvorak if TypeMatrix connected, AZERTY otherwise
- Emits `LayoutChanged` and `KeyboardStatusChanged` events for UI updates

**Services/StartupManager.cs** - Windows startup integration

- Manages registry key `HKCU\Software\Microsoft\Windows\CurrentVersion\Run`
- `IsStartupEnabled`, `EnableStartup()`, `DisableStartup()` methods
- No admin privileges required (user registry)

**Services/UpdateManager.cs** - Auto-update via Velopack

- Uses GitHub Releases as update source
- `CheckForUpdatesAsync()`, `DownloadAndApplyUpdateAsync()`
- Silent background update checks
- Auto-restart after update application

### UI Components

**UI/TrayApplicationContext.cs** - System tray application

- `NotifyIcon` with context menu
- Dynamic icons based on current layout (DV=green, AZ=blue)
- Menu items: status, keyboard connection, startup toggle, version/update, logs, quit
- Subscribes to `LayoutChanged` and `KeyboardStatusChanged` events
- Balloon notifications for layout changes and available updates

**UI/LogViewerForm.cs** - Log viewer window

- Real-time log viewing with syntax highlighting
- Auto-scroll, search, theme support (dark/light)
- File watcher for live updates
- Toolbar with refresh, clear, open folder actions

**UI/ThemeHelper.cs** - Windows theme detection

- Detects dark/light mode from registry
- `ThemeChanged` event for dynamic theme switching
- `ThemeColors` class with predefined color schemes

**Resources/IconGenerator.cs** - Programmatic icon generation

- Creates system tray icons with text labels
- `CreateDvorakIcon()` (green "DV"), `CreateAzertyIcon()` (blue "AZ"), `CreateDefaultIcon()` (gray "KB")

### Program Entry Point

**Program.cs** - Host configuration

- `[STAThread]` for Windows Forms compatibility
- Velopack initialization (`VelopackApp.Build().Run()`) must be first
- Dual mode: GUI application (default) or Windows Service (`!Environment.UserInteractive`)
- Serilog logging: rolling daily files in `C:\ProgramData\KeyboardAutoSwitcher\logs\`
- Dependency injection: `IUSBDeviceDetector` → `USBDeviceDetector`
- Retains 7 days of logs

## Key Patterns & Conventions

**Event-based over polling**: USB/power/session events trigger checks rather than continuous polling (improves battery life)

**Graceful fallback**: WMI event monitoring can fail on some systems → falls back to polling mode automatically

**Layout ID matching**: First try exact `LayoutId` match, then fallback to language ID (lower 16 bits) except for en-US

**Delay-on-recovery**: System events trigger async delays before checking layout (allows enumeration/restoration)

**Cleanup in finally**: All managed resources (`ManagementEventWatcher`, event handlers) cleaned up on shutdown

**Interface-based USB detection**: `IUSBDeviceDetector` allows mocking in unit tests

**Theme-aware UI**: Automatic dark/light mode detection and switching

## Build & Deployment

**Debug run**: `$env:DOTNET_CLI_TELEMETRY_OPTOUT=1; dotnet run`

**Release (self-contained)**:

```powershell
dotnet publish -c Release -r win-x64 --self-contained true
# Output: bin/Release/net10.0-windows/win-x64/publish/KeyboardAutoSwitcher.exe
```

**Startup integration** (no admin):

- Use the "Lancer au démarrage de Windows" menu option in system tray
- Or manually: registry key `HKCU\Software\Microsoft\Windows\CurrentVersion\Run`
- Runs in user session context (required for language switching)

**Auto-updates**: Managed via Velopack with GitHub Releases as source

**Logging**: Check `C:\ProgramData\KeyboardAutoSwitcher\logs\log-*.txt` for troubleshooting (or use built-in log viewer)

## Dependencies

- **.NET 10.0-windows** target (Windows Forms + Windows-specific APIs)
- **System.Management** 10.0.0 (WMI queries)
- **Microsoft.Extensions.Hosting.WindowsServices** 10.0.0 (BackgroundService, DI, Windows Service)
- **Serilog.Extensions.Hosting** 9.0.0 (structured logging)
- **Serilog.Sinks.Console** 4.1.0
- **Serilog.Sinks.File** 5.0.0
- **Velopack** 0.0.1298 (auto-updates)
- **Microsoft.Win32.SystemEvents** (power/session events)

## Common Tasks

**Adding a new keyboard layout**: Add to `KeyboardLayouts` class with culture name, layout ID, display name. Update `GetByCultureName()` and `GetByLayoutId()` lookup methods.

**Changing detection logic**: Modify `CheckAndSwitchLayout()` condition in `Services/KeyboardSwitcherWorker.cs` (currently: Dvorak if TypeMatrix, AZERTY otherwise).

**Adjusting delays**: Tune `Task.Delay()` calls in event handlers - 500ms for session events, 2000ms for resume.

**Detecting different keyboard**: Update `USBDeviceDetector.KeyboardInstanceName` with target VID/PID (format: `USB\VID_XXXX&PID_XXXX\`).

**Customizing tray icons**: Modify `Resources/IconGenerator.cs` - colors and text labels for each layout state.

**Adding menu items**: Update `UI/TrayApplicationContext.cs` constructor where `contextMenu.Items` are added.

## Testing

- Unit tests in `tests/` folder using xUnit + Shouldly + Moq
- Test file structure mirrors source files:
  - `tests/KeyboardLayoutConfigTests.cs` → `KeyboardLayoutConfig.cs`
  - `tests/KeyboardLayoutTests.cs` → `KeyboardLayout.cs`
  - `tests/USBDeviceDetectorTests.cs` → `USBDeviceDetector.cs`
  - `tests/Services/` → `Services/`
  - `tests/UI/` → `UI/`
  - `tests/Resources/` → `Resources/`
- `IUSBDeviceDetector` and `IUpdateManager` interfaces allow mocking in tests
- Run tests: `dotnet test`
- Memory/CPU tests: `test-memory.ps1`, `test-cpu.ps1` (see `MEMORY_TESTS.md`)
- Logs provide diagnostic detail for event triggering
