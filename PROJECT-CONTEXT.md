# Project Context

Quick reference guide for understanding this project and working with it efficiently.

## What is This Project?

**Keyboard Auto Switcher** is a Windows application that automatically switches between keyboard layouts (Dvorak ↔ AZERTY) when it detects a specific external USB keyboard (TypeMatrix).

### Core Problem Solved

Users with multiple keyboards often need different layouts:
- External TypeMatrix keyboard → US-Dvorak layout
- Built-in laptop keyboard → French AZERTY layout

Manually switching layouts is tedious. This app automates it based on USB device detection.

## Quick Start

### Run the App

```bash
dotnet run
```

The app will:
1. Appear in system tray (check notification area)
2. Monitor for TypeMatrix keyboard (USB VID_1E54, PID_2030)
3. Automatically switch layouts when keyboard is connected/disconnected
4. Respond to system events (wake from sleep, unlock)

### Run Tests

```bash
dotnet test tests/
```

### Format Code

```bash
dotnet format
```

## Current State (January 2026)

### What Works
✅ TypeMatrix keyboard detection via WMI
✅ Automatic layout switching (Dvorak ↔ AZERTY)
✅ System tray icon with dynamic indicators
✅ Respond to wake from sleep
✅ Respond to session unlock
✅ Auto-updates via GitHub Releases
✅ Launch at Windows startup
✅ Log viewer with theme support
✅ Comprehensive test coverage (>80%)

### Current Limitations
❌ Only supports TypeMatrix keyboard (hardcoded VID/PID)
❌ Only supports two layouts: Dvorak and AZERTY (hardcoded)
❌ No configuration UI
❌ No configuration file (all settings in code)

### Active Development

**Branch:** `keyboard-language-configuration`

This branch likely introduces:
- Configurable keyboard detection
- Configurable layout mappings
- Persistence for user settings

## Project Structure Overview

```
keyboard-auto-switcher/
│
├── 📄 Program.cs                      # Entry point
│
├── 📁 Services/                       # Business logic
│   ├── KeyboardSwitcherWorker.cs     # Main service
│   ├── USBDeviceDetector.cs          # WMI-based USB detection
│   ├── StartupManager.cs             # Windows startup
│   └── UpdateService.cs              # Velopack updates
│
├── 📁 UI/                            # User interface
│   ├── TrayApplicationContext.cs     # System tray
│   └── LogViewerForm.cs              # Log viewer
│
├── 📁 Resources/                     # Resources
│   └── IconGenerator.cs              # Dynamic icon generation
│
├── 📁 tests/                         # Unit tests (xUnit)
│
└── 📁 .github/workflows/             # CI/CD
    ├── tests.yml                     # Build, test, coverage
    └── release.yml                   # Package and publish
```

## Key Files to Know

### Configuration (Hardcoded)

**Keyboard Detection:**
- `Services/USBDeviceDetector.cs` line ~10
  ```csharp
  public static string KeyboardInstanceName = "USB\\VID_1E54&PID_2030\\";
  ```

**Keyboard Layouts:**
- `KeyboardLayoutConfig.cs`
  ```csharp
  UsDvorak:       0xF0020409 (en-US)
  FrenchStandard: 0x040C040C (fr-FR)
  ```

**Switching Logic:**
- `Services/KeyboardSwitcherWorker.cs` in `CheckAndSetKeyboardLayoutAsync()`

### Important Paths

**Logs:**
```
C:\ProgramData\KeyboardAutoSwitcher\logs\log-YYYYMMDD.txt
```

**Registry (Startup):**
```
HKCU\Software\Microsoft\Windows\CurrentVersion\Run\KeyboardAutoSwitcher
```

## Architecture at a Glance

```
┌─────────────────────────────────────────────┐
│          System Events                      │
│  (USB, Wake, Unlock, Display)               │
└──────────────┬──────────────────────────────┘
               │
               ▼
┌─────────────────────────────────────────────┐
│     KeyboardSwitcherWorker                  │
│  (Hosted Service - Main Business Logic)    │
│                                             │
│  ┌─────────────┐      ┌─────────────────┐  │
│  │ USB Detect  │      │  Layout Switch  │  │
│  │    (WMI)    │─────▶│   (Win32 API)   │  │
│  └─────────────┘      └─────────────────┘  │
└──────────────┬──────────────────────────────┘
               │
               ▼
┌─────────────────────────────────────────────┐
│     TrayApplicationContext                  │
│   (System Tray Icon + Menu)                 │
│                                             │
│   • Dynamic icon (Dvorak=Green, AZERTY=Blue)│
│   • Balloon notifications                   │
│   • Menu actions (logs, startup, updates)   │
└─────────────────────────────────────────────┘
```

## Common Tasks

### Add Support for Another Keyboard

1. Find VID/PID: Device Manager → Properties → Hardware IDs
   ```
   USB\VID_XXXX&PID_YYYY\...
   ```

2. Update `Services/USBDeviceDetector.cs`:
   ```csharp
   public static string KeyboardInstanceName = "USB\\VID_XXXX&PID_YYYY\\";
   ```

3. Test: Run app, plug in keyboard, check logs

### Add a New Keyboard Layout

1. Install layout in Windows Settings

2. Find Layout ID:
   ```powershell
   Get-WinUserLanguageList | Select LanguageTag, InputMethodTips
   ```

3. Add to `KeyboardLayoutConfig.cs`:
   ```csharp
   public static readonly KeyboardLayoutConfig GermanQwertz = new(
       cultureName: "de-DE",
       layoutId: 0x04070407,
       displayName: "German (Germany)"
   );
   ```

4. Update `Services/KeyboardSwitcherWorker.cs` logic

5. Update `Resources/IconGenerator.cs` for icon color/text

### Respond to New System Event

Add to `Services/KeyboardSwitcherWorker.cs` in `ExecuteAsync()`:

```csharp
SystemEvents.DisplaySettingsChanged += (s, e) => {
    _logger.LogInformation("Display settings changed");
    Task.Run(() => CheckAndSetKeyboardLayoutAsync());
};
```

### Debug Issues

**Enable detailed logging:**
```csharp
// In Program.cs
.MinimumLevel.Debug()  // Change from Information
```

**Check logs:**
```
C:\ProgramData\KeyboardAutoSwitcher\logs\log-YYYYMMDD.txt
```

**Common issues:**
- WMI access denied → Run as Administrator
- Layout not switching → Check layout is installed
- USB not detected → Verify VID/PID in Device Manager

## Testing Strategy

### Unit Tests

Located in `tests/` directory using xUnit and Moq:

- **KeyboardLayoutTests.cs** - Win32 API mocking
- **USBDeviceDetectorTests.cs** - WMI query mocking
- **WorkerTests.cs** - Main service logic
- **TrayApplicationContextTests.cs** - UI logic
- **StartupManagerTests.cs** - Registry operations
- **UpdateServiceTests.cs** - Update flow

### Coverage

Run with coverage report:
```bash
dotnet test tests/ --collect:"XPlat Code Coverage"
reportgenerator -reports:"tests/TestResults/*/coverage.cobertura.xml" \
                -targetdir:"coverage-report" \
                -reporttypes:Html
```

**Target:** >80% coverage (enforced by CI)

### CI/CD

**Every push/PR:**
- Build check
- Code format check (`dotnet format --verify-no-changes`)
- Run all tests
- Upload coverage to Codecov

**On GitHub Release:**
- Build self-contained executable
- Create Velopack installer
- Upload `Setup.exe`, `.nupkg`, `RELEASES` to release

## Dependencies

### Runtime
- .NET 10.0 (Windows)
- Windows Forms

### NuGet Packages
- **System.Management** - WMI queries
- **Serilog** - Structured logging
- **Velopack** - Auto-updates
- **Microsoft.Extensions.Hosting** - DI and hosted services

### Development
- **xUnit** - Test framework
- **Moq** - Mocking library

## Documentation

- **README.md** - Installation and usage
- **ARCHITECTURE.md** - Detailed architecture and patterns
- **DEVELOPMENT.md** - Development guide and how-tos
- **TECHNICAL-DETAILS.md** - Deep technical implementation details
- **PROJECT-CONTEXT.md** - This file (quick reference)

## Future Enhancements

Based on `keyboard-language-configuration` branch:

1. **Configuration System**
   - JSON config file or registry-based settings
   - Support multiple keyboard VID/PIDs
   - Support multiple layouts per keyboard
   - User-configurable delays

2. **Configuration UI**
   - Right-click menu → "Settings"
   - Add/remove keyboards
   - Configure layout mappings
   - Test detection

3. **Additional Detections**
   - Bluetooth keyboard connect/disconnect
   - Docking station events
   - Multiple monitor configuration changes
   - Virtual desktop switches

4. **Enhancements**
   - Per-application layout preferences
   - Keyboard shortcuts to force layout switch
   - Layout history (quick switch to previous)
   - Notification preferences

## Useful Commands

```bash
# Development
dotnet run                                    # Run app
dotnet watch run                              # Run with hot reload
dotnet format                                 # Format code

# Testing
dotnet test tests/                            # Run all tests
dotnet test --filter "FullyQualifiedName~USB" # Run specific tests

# Building
dotnet publish -c Release -r win-x64 --self-contained true

# Packaging
vpk pack --packId KeyboardAutoSwitcher \
         --packVersion 1.1.0 \
         --packDir .\bin\Release\net10.0-windows\win-x64\publish\ \
         --mainExe KeyboardAutoSwitcher.exe

# Release
git tag v1.2.0
git push origin v1.2.0
# Then create GitHub Release → CI builds and uploads installer
```

## Getting Help

### Log Files
First check: `C:\ProgramData\KeyboardAutoSwitcher\logs\`

### Device Manager
Verify keyboard VID/PID:
1. Open Device Manager
2. Find keyboard under "USB Input Device" or "Keyboards"
3. Properties → Details → Hardware IDs
4. Look for `USB\VID_XXXX&PID_YYYY`

### Registry
Check startup entry:
```powershell
Get-ItemProperty "HKCU:\Software\Microsoft\Windows\CurrentVersion\Run"
```

### Installed Layouts
List keyboard layouts:
```powershell
Get-WinUserLanguageList | Select LanguageTag, InputMethodTips
```

## Tips for AI Assistants

When working on this project:

1. **Always check logs first** - They're in `C:\ProgramData\KeyboardAutoSwitcher\logs\`
2. **VID/PID is in** `Services/USBDeviceDetector.cs` line ~10
3. **Layouts are in** `KeyboardLayoutConfig.cs`
4. **Main logic is in** `Services/KeyboardSwitcherWorker.cs`
5. **Tests are comprehensive** - Check `tests/` for examples
6. **Code must be formatted** - Run `dotnet format` before committing
7. **Branch context** - `keyboard-language-configuration` is for adding configuration support

### Likely Next Steps

If implementing configuration:
- Create `Configuration/KeyboardConfig.cs` model
- Add JSON serialization (System.Text.Json)
- Store in `%APPDATA%\KeyboardAutoSwitcher\config.json`
- Add migration from hardcoded values
- Create settings UI form
- Update tests for configuration scenarios

### Code Patterns to Follow

- Use dependency injection for all services
- Use events for component communication
- Use async/await for I/O operations
- Log at appropriate levels (Info/Warning/Error)
- Write unit tests for new functionality
- Follow existing naming conventions

## Quick Wins

Easy improvements to make:

1. **Add keyboard shortcuts** - Global hotkeys to force layout switch
2. **Notification settings** - Let user disable balloon notifications
3. **Layout detection feedback** - Show which layout was detected on startup
4. **Tray icon tooltip** - Show last detection time
5. **Log level configuration** - Runtime log level adjustment

## Contact & Links

- **Repository:** https://github.com/jdeniau/keyboard-auto-switcher
- **Issues:** https://github.com/jdeniau/keyboard-auto-switcher/issues
- **Releases:** https://github.com/jdeniau/keyboard-auto-switcher/releases
- **CI/CD:** https://github.com/jdeniau/keyboard-auto-switcher/actions
- **Coverage:** https://codecov.io/gh/jdeniau/keyboard-auto-switcher
