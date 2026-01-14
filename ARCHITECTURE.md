# Project Architecture

## Overview

**Keyboard Auto Switcher** is a .NET 10 Windows Forms application that automatically switches keyboard layouts based on the detection of an external USB keyboard (currently TypeMatrix).

### How It Works

```
Application Startup
         │
         ├─→ Velopack Initialization (auto-update)
         ├─→ Serilog Configuration (logging)
         ├─→ DI Configuration (services)
         │
         ├─→ GUI Mode?
         │   ├─→ Yes: TrayApplicationContext + UpdateService
         │   └─→ No: Service Mode (headless)
         │
         └─→ KeyboardSwitcherWorker Starts
                   │
                   ├─→ Initial USB Check
                   ├─→ Start USB Monitoring (WMI)
                   ├─→ Subscribe to System Events
                   │   ├─→ PowerModeChanged (sleep/wake)
                   │   └─→ SessionSwitch (lock/unlock)
                   │
                   └─→ Automatic Layout Switch
                       ├─→ TypeMatrix Detected → Dvorak (en-US)
                       └─→ No TypeMatrix → AZERTY (fr-FR)
```

## Project Structure

```
keyboard-auto-switcher/
│
├── Program.cs                    # Entry point, DI configuration
│
├── Services/                     # Business logic
│   ├── KeyboardSwitcherWorker.cs    # Main service (hosted service)
│   ├── StartupManager.cs            # Windows startup management
│   ├── IRegistryService.cs          # Registry access interface
│   ├── WindowsRegistryService.cs    # Registry access implementation
│   ├── IUSBDeviceDetector.cs        # USB detection interface
│   ├── USBDeviceDetector.cs         # USB detection via WMI
│   ├── IStartupManager.cs           # Startup management interface
│   ├── IUpdateManager.cs            # Update interface
│   └── UpdateService.cs             # Updates via Velopack
│
├── UI/                           # User interface
│   ├── TrayApplicationContext.cs    # System tray icon context
│   ├── LogViewerForm.cs             # Log viewer
│   └── ThemeHelper.cs               # Windows theme detection
│
├── Resources/                    # Resources
│   └── IconGenerator.cs             # Dynamic icon generation
│
├── Logging/                      # Logging infrastructure
│   ├── LoggingConstants.cs          # Constants and templates
│   └── SerilogVelopackLogger.cs     # Velopack → Serilog adapter
│
├── KeyboardLayout.cs             # Win32 API for layouts
├── KeyboardLayoutConfig.cs       # Layout configuration (Dvorak, AZERTY)
│
└── tests/                        # Unit tests
    ├── KeyboardLayoutTests.cs
    ├── StartupManagerTests.cs
    ├── TrayApplicationContextTests.cs
    ├── UpdateServiceTests.cs
    ├── USBDeviceDetectorTests.cs
    └── WorkerTests.cs
```

## Core Components

### 1. KeyboardSwitcherWorker (Main Service)

**Responsibilities:**
- Monitor TypeMatrix keyboard connection/disconnection
- Respond to system events (sleep, unlock)
- Automatically switch keyboard layout
- Notify UI of layout changes

**Event-Driven Architecture:**

```
USBDeviceDetector.DeviceChanged
          │
          ├─→ KeyboardSwitcherWorker.OnDeviceChanged()
          │        │
          │        └─→ CheckAndSetKeyboardLayoutAsync()
          │                   │
          │                   ├─→ Detect connected keyboard
          │                   ├─→ Select layout (Dvorak/AZERTY)
          │                   ├─→ KeyboardLayout.SetLayout()
          │                   └─→ LayoutChanged Event 🔔
          │
SystemEvents.PowerModeChanged
          │
          └─→ OnPowerModeChanged() → CheckAndSetKeyboardLayoutAsync()

SystemEvents.SessionSwitch
          │
          └─→ OnSessionSwitch() → CheckAndSetKeyboardLayoutAsync()
```

**Strategic Delays:**
- **Wake from sleep**: 2 seconds (allows USB re-enumeration)
- **Session unlock**: 500ms (allows system stabilization)
- **Polling mode**: 10 seconds (fallback if WMI fails)

### 2. USBDeviceDetector (USB Detection)

**Technology: Windows Management Instrumentation (WMI)**

```csharp
// TypeMatrix Identification
VID: 1E54 (Vendor ID)
PID: 2030 (Product ID)
Pattern: "USB\\VID_1E54&PID_2030\\"
```

**Detection Modes:**

1. **Event-Based Mode (optimal)**:
   ```csharp
   ManagementEventWatcher
   WQL: "SELECT * FROM __InstanceOperationEvent
         WITHIN 2 WHERE TargetInstance ISA 'Win32_USBHub'"
   ```
   - Real-time events
   - No CPU polling

2. **Direct Query**:
   ```csharp
   ManagementObjectSearcher("SELECT PNPDeviceID FROM Win32_USBHub")
   ```
   - 5-second timeout
   - Retry after 500ms on failure
   - UI freeze protection

### 3. KeyboardLayout (Win32 API)

**Win32 APIs Used:**

```csharp
[DllImport("user32.dll")]
GetKeyboardLayout(uint idThread)          // Current layout
ActivateKeyboardLayout(IntPtr hkl, ...)   // Switch layout
GetKeyboardLayoutList(...)                // List layouts
PostMessage(..., WM_INPUTLANGCHANGEREQUEST, ...) // Notify windows
```

**Switch Process:**

```
1. RefreshLayoutCache()
   └─→ GetKeyboardLayoutList() → Cache installed layouts

2. GetCurrentLayout()
   └─→ GetKeyboardLayout(foregroundWindow.threadId)

3. SetLayout(targetLayout)
   ├─→ Search layout by exact ID or Language ID
   ├─→ ActivateKeyboardLayout(layout, KLF_ACTIVATE)
   └─→ PostMessage(WM_INPUTLANGCHANGEREQUEST) to all windows
```

**Configured Layouts:**

| Layout | Culture | Layout ID | Hex ID |
|--------|---------|-----------|---------|
| **Dvorak** | en-US | -268304375 | 0xF0020409 |
| **AZERTY** | fr-FR | 67896332 | 0x040C040C |

### 4. TrayApplicationContext (System Interface)

**Dynamic Icon Generation:**

```csharp
IconGenerator.GenerateLayoutIcon(layout)
├─→ 32x32 Bitmap
├─→ Rounded rectangle (4px radius)
├─→ Color by layout:
│   ├─→ Dvorak: Green #4CAF50 + "DV"
│   ├─→ AZERTY: Blue #2196F3 + "AZ"
│   └─→ Default: Gray #9E9E9E + "KB"
└─→ Centered text (Segoe UI 9pt font)
```

**Context Menu:**

1. **Current State** (disabled, informational)
   - "Clavier actuel: Dvorak"

2. **External Keyboard Status**
   - "Clavier externe: ✓ Connecté" / "✗ Déconnecté"

3. **Launch at Startup**
   - Toggle via StartupManager

4. **Update**
   - "Version actuelle: 1.1.0"
   - "🔄 Mise à jour disponible: vX.Y.Z" (if available)

5. **Logs**
   - "Voir les logs" → LogViewerForm
   - "Ouvrir le dossier des logs" → Explorer

6. **Quit**

**Adaptive Theme:**
- Registry read: `HKCU\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize\AppsUseLightTheme`
- Listens to `SystemEvents.UserPreferenceChanged`
- LogViewerForm with adapted syntax highlighting

### 5. UpdateService (Velopack)

**Source: GitHub Releases**

```csharp
GithubSource("https://github.com/jdeniau/keyboard-auto-switcher")
```

**Update Flow:**

```
App Startup
     │
     ├─→ CheckForUpdatesSilentAsync() (background)
     │        │
     │        ├─→ GitHub API call
     │        ├─→ Version comparison
     │        └─→ UpdateAvailable event
     │                   │
     │                   └─→ TrayApplicationContext updates menu
     │
User clicks "Mise à jour disponible"
     │
     ├─→ CheckForUpdatesAsync()
     │        │
     │        ├─→ DownloadUpdatesAsync(asset, progressCallback)
     │        │        │
     │        │        └─→ Progress displayed in menu
     │        │
     │        ├─→ ApplyUpdatesAndRestart()
     │        └─→ Application restarts
     │
Restart
     │
     └─→ VelopackApp.Build().Run()
              └─→ Finalize installation
```

**Release Files:**
- `KeyboardAutoSwitcher-win-x64-Setup.exe` - Installer
- `KeyboardAutoSwitcher-{version}-win-x64-full.nupkg` - Full package
- `RELEASES` - Version manifest

## Patterns and Principles

### Dependency Injection

```csharp
services.AddSingleton<IRegistryService, WindowsRegistryService>();
services.AddSingleton<IUSBDeviceDetector, USBDeviceDetector>();
services.AddSingleton<IStartupManager, StartupManager>();
services.AddHostedService<KeyboardSwitcherWorker>();
```

**Benefits:**
- Testability (mocks)
- Loose coupling
- Inversion of control

### Event-Driven Architecture

```csharp
// Publishers
public event EventHandler<DeviceChangedEventArgs>? DeviceChanged;
public event EventHandler<LayoutChangedEventArgs>? LayoutChanged;
public event EventHandler<UpdateEventArgs>? UpdateAvailable;

// Subscribers
_usbDetector.DeviceChanged += OnDeviceChanged;
_worker.LayoutChanged += OnLayoutChanged;
_updateService.UpdateAvailable += OnUpdateAvailable;
```

**Benefits:**
- Real-time reactivity
- Component decoupling
- No intensive polling

### Thread-Safe Design

```csharp
// UI updates via Invoke
if (_trayIcon.InvokeRequired)
{
    _trayIcon.Invoke(() => UpdateIcon(layout));
}
else
{
    UpdateIcon(layout);
}

// Async/await for I/O
await _updateManager.CheckForUpdatesAsync();
```

### Robust Error Handling

```csharp
// Retry logic (USB detection)
try
{
    return QueryUSBDevices();
}
catch (ManagementException ex)
{
    _logger.LogWarning(ex, "First attempt failed, retrying...");
    await Task.Delay(500);
    return QueryUSBDevices();
}

// WMI Timeouts
var options = new EnumerationOptions
{
    Timeout = TimeSpan.FromSeconds(5)
};

// Fallback polling
if (!_monitoringStarted)
{
    _logger.LogWarning("WMI monitoring failed, fallback to 10s polling");
    StartPollingFallback();
}
```

## Application Lifecycle

### Startup

```
1. VelopackApp.Build().Run()
   └─→ Finalize pending installations/updates

2. Serilog.Log.Logger = new LoggerConfiguration()
   └─→ C:\ProgramData\KeyboardAutoSwitcher\logs\

3. Host.CreateDefaultBuilder()
   ├─→ Configure services (DI)
   └─→ GUI vs Service mode

4. Application.Run(TrayApplicationContext)
   └─→ Show tray icon, start worker

5. KeyboardSwitcherWorker.StartAsync()
   ├─→ Initial layout check
   ├─→ Start USB monitoring
   └─→ Subscribe to system events
```

### Runtime

```
┌─────────────────────────────────────────────────┐
│                                                 │
│  Event Triggered                                │
│  (USB change / Wake / Unlock)                   │
│            │                                    │
│            ▼                                    │
│  CheckAndSetKeyboardLayoutAsync()               │
│            │                                    │
│            ├─→ Detect connected keyboard       │
│            ├─→ Select appropriate layout       │
│            ├─→ Switch via Win32 API            │
│            └─→ Notify UI (LayoutChanged)       │
│                        │                        │
│                        ▼                        │
│            TrayApplicationContext.OnLayoutChanged()
│                        │                        │
│                        ├─→ Update icon          │
│                        ├─→ Update tooltip       │
│                        └─→ Balloon notification │
│                                                 │
└─────────────────────────────────────────────────┘
```

### Shutdown

```
User: Click "Quitter" in tray menu
      │
      ├─→ Application.Exit()
      │
      ├─→ KeyboardSwitcherWorker.StopAsync()
      │        │
      │        ├─→ _usbDetector.StopMonitoring()
      │        ├─→ SystemEvents.PowerModeChanged -= ...
      │        ├─→ SystemEvents.SessionSwitch -= ...
      │        └─→ _pollingTimer?.Dispose()
      │
      └─→ TrayApplicationContext.Dispose()
               │
               ├─→ _trayIcon.Visible = false
               └─→ _trayIcon.Dispose()
```

## Dependencies and Technologies

### Framework and Runtime
- **.NET 10.0** (net10.0-windows)
- **Windows Forms** (UI)
- **Microsoft.Extensions.Hosting** (DI, hosted services)

### NuGet Packages
- **System.Management 10.0.0** - WMI (USB detection)
- **Serilog 9.0.0** - Structured logging
  - Serilog.Extensions.Hosting
  - Serilog.Sinks.Console
  - Serilog.Sinks.File (daily rotation, 7 days retention)
- **Velopack 0.0.1298** - Auto-updates and packaging

### Win32 APIs
- **user32.dll** - Keyboard layout management
- **Windows Registry** - Auto-startup, theme

### Infrastructure
- **GitHub Actions** - CI/CD
  - tests.yml: Tests + code coverage
  - release.yml: Build + packaging + publication
- **Codecov** - Coverage reports

## Current Configuration

### Detected Keyboards
```csharp
// Hardcoded in USBDeviceDetector.cs
KeyboardInstanceName = "USB\\VID_1E54&PID_2030\\";
// TypeMatrix only
```

### Configured Layouts
```csharp
// Hardcoded in KeyboardLayoutConfig.cs
UsDvorak:       layoutId=0xF0020409, culture="en-US"
FrenchStandard: layoutId=0x040C040C, culture="fr-FR"
```

### System Paths
```
Logs:      C:\ProgramData\KeyboardAutoSwitcher\logs\log-YYYYMMDD.txt
Registry:  HKCU\Software\Microsoft\Windows\CurrentVersion\Run\KeyboardAutoSwitcher
Theme:     HKCU\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize\AppsUseLightTheme
```

## Planned Future Evolutions

Based on the current `keyboard-language-configuration` branch and project description:

1. **Keyboard Configuration** - Support keyboards other than TypeMatrix
2. **Layout Configuration** - Customize associated layouts
3. **Additional Detections** - New system events to monitor

These evolutions will likely require:
- A persistent configuration system (JSON file or registry)
- A configuration UI
- Refactoring of hardcoded constants
