# Copilot Instructions for keyboard-auto-switcher

## Overview

Windows system tray app that auto-switches keyboard layouts (Dvorak ↔ AZERTY) when a TypeMatrix USB keyboard is connected/disconnected. Runs without admin privileges. Uses Velopack for auto-updates via GitHub Releases.

## Architecture

```
Program.cs              → Entry point, DI setup, Velopack init (MUST be first line)
├── Services/
│   ├── KeyboardSwitcherWorker.cs  → Main logic: USB/power/session events → layout switch
│   ├── StartupManager.cs          → Windows startup via HKCU registry
│   └── UpdateService.cs           → Velopack auto-updates
├── UI/
│   ├── TrayApplicationContext.cs  → System tray icon + context menu
│   ├── LogViewerForm.cs           → Real-time log viewer
│   └── ThemeHelper.cs             → Dark/light mode detection
├── KeyboardLayout.cs              → Win32 P/Invoke (user32.dll)
├── KeyboardLayoutConfig.cs        → Layout definitions (UsDvorak, FrenchStandard)
└── USBDeviceDetector.cs           → WMI-based USB monitoring
```

## Critical Patterns

**Event-driven, not polling**: `KeyboardSwitcherWorker` listens to USB events, power mode changes (`SystemEvents.PowerModeChanged`), and session switches (`SystemEvents.SessionSwitch`). Polling is fallback only.

**Delay-on-recovery**: After resume (2000ms) or unlock (500ms), delay before checking layout to allow USB re-enumeration.

**Interface-based DI**: All external dependencies (`IUSBDeviceDetector`, `IRegistryService`, `IUpdateManager`, `IStartupManager`) have interfaces for testability.

**Layout ID quirk**: Layout IDs are 32-bit (e.g., `0xF0020409` for Dvorak). Language ID fallback intentionally disabled for en-US to avoid matching QWERTY.

## Key Commands

```powershell
# Run in debug
$env:DOTNET_CLI_TELEMETRY_OPTOUT=1; dotnet run

# Run tests
dotnet test tests

# Format check (CI enforces this)
dotnet format keyboard-auto-switcher.sln --verify-no-changes

# Publish release
dotnet publish -c Release -r win-x64 --self-contained true
```

## Testing

-   **Stack**: xUnit + Shouldly (assertions) + Moq (mocking)
-   **Structure**: Mirror source → `Services/StartupManager.cs` → `tests/Services/StartupManagerTests.cs`
-   **Coverage**: `dotnet test tests --collect:"XPlat Code Coverage"` then `reportgenerator`

## Common Modifications

| Task                    | File(s)                              | Notes                                                    |
| ----------------------- | ------------------------------------ | -------------------------------------------------------- |
| Add new keyboard layout | `KeyboardLayoutConfig.cs`            | Add to `KeyboardLayouts`, update lookup methods          |
| Change target keyboard  | `USBDeviceDetector.cs`               | Update `KeyboardInstanceName` VID/PID                    |
| Modify switch logic     | `Services/KeyboardSwitcherWorker.cs` | Edit `CheckAndSwitchLayout()`                            |
| Change event delays     | `Services/KeyboardSwitcherWorker.cs` | `Task.Delay()` in `OnPowerModeChanged`/`OnSessionSwitch` |
| Customize tray icons    | `Resources/IconGenerator.cs`         | Colors: green=Dvorak, blue=AZERTY                        |
| Add tray menu items     | `UI/TrayApplicationContext.cs`       | Modify `contextMenu.Items`                               |

## Tech Stack

-   **.NET 10.0-windows** (Windows Forms)
-   **System.Management** (WMI queries for USB detection)
-   **Microsoft.Extensions.Hosting** (BackgroundService, DI)
-   **Serilog** (logging to `C:\ProgramData\KeyboardAutoSwitcher\logs\`)
-   **Velopack** (auto-updates)

## CI/CD

-   GitHub Actions: `.github/workflows/tests.yml` (build + format + test + coverage)
-   Release: `.github/workflows/release.yml` (triggered by `vX.Y.Z` tags)
-   Version in `keyboard-auto-switcher.csproj` `<Version>` element
