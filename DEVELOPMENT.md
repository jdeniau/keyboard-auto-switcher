# Development Guide

## Prerequisites

### Required Software
- **.NET 10 SDK** or later
- **Windows 10/11** (required for Windows Forms and WMI APIs)
- **Visual Studio 2022** or **Visual Studio Code** (recommended)
- **Git**

### Optional Tools
- **Velopack CLI** (`dotnet tool install -g vpk`) - For building installers
- **ReportGenerator** (`dotnet tool install -g dotnet-reportgenerator-globaltool`) - For coverage reports

## Getting Started

### Clone and Build

```bash
# Clone repository
git clone https://github.com/jdeniau/keyboard-auto-switcher.git
cd keyboard-auto-switcher

# Restore dependencies
dotnet restore

# Build project
dotnet build

# Run application (with telemetry disabled)
$env:DOTNET_CLI_TELEMETRY_OPTOUT=1; dotnet run
```

### Project Structure

```
keyboard-auto-switcher/
├── *.cs                          # Main application files
├── Services/                     # Business logic layer
├── UI/                          # User interface components
├── Resources/                   # Icons and resources
├── Logging/                     # Logging infrastructure
├── tests/                       # Unit tests
├── .github/                     # GitHub Actions workflows
│   ├── workflows/
│   │   ├── tests.yml           # CI: Build, format check, tests
│   │   └── release.yml         # CD: Package and publish
│   └── copilot-instructions.md # GitHub Copilot context
├── keyboard-auto-switcher.csproj
└── README.md
```

## Development Workflow

### Running Locally

```powershell
# Standard run
dotnet run

# With hot reload
dotnet watch run

# Debug mode (attach debugger in VS/VSCode)
dotnet run --configuration Debug
```

**Important:** The app will:
1. Create a tray icon (check system tray)
2. Write logs to `C:\ProgramData\KeyboardAutoSwitcher\logs\`
3. Try to detect TypeMatrix keyboard (VID_1E54&PID_2030)

### Code Style

This project follows standard C# conventions and uses `dotnet format` for code formatting.

```powershell
# Check formatting issues
dotnet format --verify-no-changes

# Auto-fix formatting
dotnet format
```

**CI Enforcement:** The `tests.yml` workflow runs `dotnet format --verify-no-changes` on every push/PR.

### Testing

#### Run All Tests

```powershell
dotnet test tests/
```

#### Run Specific Test Class

```powershell
dotnet test tests/ --filter "FullyQualifiedName~KeyboardLayoutTests"
```

#### Run with Coverage

```powershell
# Generate coverage data
dotnet test tests/ --collect:"XPlat Code Coverage" --results-directory:"tests/TestResults"

# Generate HTML report
reportgenerator -reports:"tests/TestResults/*/coverage.cobertura.xml" `
                -targetdir:"coverage-report" `
                -reporttypes:Html

# View report
start coverage-report/index.html
```

**Coverage Target:** The project aims for >80% code coverage. Current coverage is tracked via Codecov badge in README.

### Test Structure

All tests are in the `tests/` directory using **xUnit** framework:

```
tests/
├── KeyboardLayoutTests.cs          # Win32 API layout switching
├── StartupManagerTests.cs          # Windows startup integration
├── TrayApplicationContextTests.cs  # UI and tray icon
├── UpdateServiceTests.cs           # Velopack updates
├── USBDeviceDetectorTests.cs       # WMI USB detection
└── WorkerTests.cs                  # Main service logic
```

**Mocking:** Uses `Moq` library for interface mocking:
- `IUSBDeviceDetector` → Mock USB detection
- `IRegistryService` → Mock registry access
- `IUpdateManager` → Mock update checks

## Debugging Tips

### Debugging USB Detection

The WMI-based USB detector can be tricky to debug:

```csharp
// In USBDeviceDetector.cs, add more logging:
_logger.LogInformation("Checking for keyboard: {InstanceName}", KeyboardInstanceName);

// List all connected USB devices:
foreach (ManagementObject usbDevice in searcher.Get())
{
    _logger.LogDebug("Found USB: {PnpId}", usbDevice["PNPDeviceID"]);
}
```

**Common Issues:**
- **WMI Timeout:** Increase timeout in `EnumerationOptions`
- **Access Denied:** Run as Administrator
- **Wrong Device Class:** Verify using Device Manager → Properties → Hardware IDs

### Debugging Keyboard Layout Switch

```csharp
// In KeyboardLayout.cs, log all installed layouts:
var layouts = GetInstalledLayouts();
foreach (var layout in layouts)
{
    _logger.LogDebug("Available Layout: {Id:X8} - {Culture}",
                     layout.LayoutId, layout.CultureName);
}
```

**Tools:**
- **PowerShell:** `Get-WinUserLanguageList` lists installed layouts
- **Registry:** `HKCU\Keyboard Layout\Preload` shows layout order

### Debugging Updates

```csharp
// In UpdateService.cs:
_logger.LogInformation("Current Version: {Version}", _currentVersion);
_logger.LogInformation("Checking: {RepoUrl}", GitHubRepoUrl);
```

**Test Update Locally:**
1. Build version 1.0.0
2. Install it
3. Change version to 1.0.1 in `.csproj`
4. Build and create package with `vpk pack`
5. Upload to GitHub Releases
6. Run app → should detect update

## Building for Release

### Self-Contained Build

```powershell
dotnet publish -c Release -r win-x64 --self-contained true
```

Output: `bin\Release\net10.0-windows\win-x64\publish\`

This creates a **self-contained** executable that doesn't require .NET runtime installed.

### Creating Installer

```powershell
# 1. Publish
dotnet publish -c Release -r win-x64 --self-contained true

# 2. Package with Velopack
vpk pack --packId KeyboardAutoSwitcher `
         --packVersion 1.1.0 `
         --packDir .\bin\Release\net10.0-windows\win-x64\publish\ `
         --mainExe KeyboardAutoSwitcher.exe

# 3. Output in Releases/ folder:
#    - KeyboardAutoSwitcher-win-x64-Setup.exe
#    - KeyboardAutoSwitcher-1.1.0-win-x64-full.nupkg
#    - RELEASES
```

### Release Process (Automated)

The project uses GitHub Actions for automated releases:

1. **Update version** in `keyboard-auto-switcher.csproj`:
   ```xml
   <Version>1.2.0</Version>
   ```

2. **Commit and tag**:
   ```bash
   git add keyboard-auto-switcher.csproj
   git commit -m "Bump version to 1.2.0"
   git tag v1.2.0
   git push origin main
   git push origin v1.2.0
   ```

3. **Create GitHub Release**:
   - Go to GitHub → Releases → "Draft a new release"
   - Select tag `v1.2.0`
   - Title: `v1.2.0`
   - Describe changes
   - Publish release

4. **GitHub Actions automatically**:
   - Builds the project
   - Runs tests
   - Creates installer with `vpk pack`
   - Uploads `Setup.exe`, `.nupkg`, and `RELEASES` to the release

## Common Development Tasks

### Adding a New Keyboard Type

Currently hardcoded in `Services/USBDeviceDetector.cs`:

```csharp
public static string KeyboardInstanceName = "USB\\VID_1E54&PID_2030\\";
```

**To add new keyboard:**
1. Find VID/PID using Device Manager → Properties → Hardware IDs
2. Update the constant or add configuration system
3. Test detection: `IsTargetKeyboardConnected()`
4. Add unit tests in `tests/USBDeviceDetectorTests.cs`

**Future:** The `keyboard-language-configuration` branch likely adds configurable keyboards.

### Adding a New Keyboard Layout

Currently defined in `KeyboardLayoutConfig.cs`:

```csharp
public static readonly KeyboardLayoutConfig NewLayout = new(
    cultureName: "de-DE",           // German
    layoutId: 0x04070407,           // Layout ID from registry
    displayName: "German (Germany)"
);
```

**Steps:**
1. Install the layout on Windows
2. Find Layout ID:
   ```powershell
   Get-WinUserLanguageList | Select LanguageTag, InputMethodTips
   # Or check: HKCU\Keyboard Layout\Preload
   ```
3. Add to `KeyboardLayoutConfig.cs`
4. Update icon generator in `Resources/IconGenerator.cs`
5. Update worker logic in `Services/KeyboardSwitcherWorker.cs`

### Adding a New System Event

The worker subscribes to Windows events in `StartAsync()`:

```csharp
protected override async Task ExecuteAsync(CancellationToken stoppingToken)
{
    // Add new event:
    SystemEvents.DisplaySettingsChanged += OnDisplaySettingsChanged;

    // ... existing code
}

private void OnDisplaySettingsChanged(object? sender, EventArgs e)
{
    _logger.LogInformation("Display settings changed");
    Task.Run(() => CheckAndSetKeyboardLayoutAsync());
}
```

**Available Events:**
- `SystemEvents.PowerModeChanged` ✓ (already used)
- `SystemEvents.SessionSwitch` ✓ (already used)
- `SystemEvents.DisplaySettingsChanged` (monitor connect/disconnect)
- `SystemEvents.UserPreferenceChanged` (theme, etc.)

### Modifying the Tray Icon

Icon generation in `Resources/IconGenerator.cs`:

```csharp
public static Icon GenerateLayoutIcon(KeyboardLayoutConfig layout)
{
    using var bitmap = new Bitmap(32, 32);
    using var graphics = Graphics.FromImage(bitmap);

    // Change colors, text, shapes here
    Color bgColor = layout.CultureName switch
    {
        "en-US" => ColorTranslator.FromHtml("#4CAF50"), // Green
        "fr-FR" => ColorTranslator.FromHtml("#2196F3"), // Blue
        "de-DE" => ColorTranslator.FromHtml("#FF9800"), // Orange (example)
        _ => ColorTranslator.FromHtml("#9E9E9E")        // Gray
    };

    // ... drawing code
}
```

## Troubleshooting

### "Access to WMI is denied"

**Solution:** Run Visual Studio or terminal as Administrator.

### "Layout not switching"

**Checks:**
1. Is the layout installed? `Get-WinUserLanguageList`
2. Is the Layout ID correct? Check `HKCU\Keyboard Layout\Preload`
3. Are there logs? `C:\ProgramData\KeyboardAutoSwitcher\logs\`
4. Run with debugger and check `KeyboardLayout.SetLayout()`

### "USB device not detected"

**Checks:**
1. Verify VID/PID in Device Manager
2. Check logs: "Device change detected"
3. Is WMI service running? `Get-Service Winmgmt`
4. Try fallback polling mode (automatic after WMI failure)

### "App not starting with Windows"

**Checks:**
1. Registry key exists?
   ```powershell
   Get-ItemProperty "HKCU:\Software\Microsoft\Windows\CurrentVersion\Run" |
       Select-Object KeyboardAutoSwitcher
   ```
2. Path in registry is correct and quoted
3. Check Windows Event Viewer for startup errors

### "Update not working"

**Checks:**
1. GitHub Releases contains `RELEASES` file
2. Versions are correctly formatted (semantic versioning)
3. Check logs for `UpdateService` entries
4. Test GitHub API access: `curl https://api.github.com/repos/jdeniau/keyboard-auto-switcher/releases`

## Code Architecture Patterns

### Dependency Injection Pattern

All services use constructor injection:

```csharp
public class MyNewService : IMyNewService
{
    private readonly ILogger<MyNewService> _logger;
    private readonly IUSBDeviceDetector _usbDetector;

    public MyNewService(
        ILogger<MyNewService> logger,
        IUSBDeviceDetector usbDetector)
    {
        _logger = logger;
        _usbDetector = usbDetector;
    }
}

// Register in Program.cs:
services.AddSingleton<IMyNewService, MyNewService>();
```

### Event Pattern

Use events for loose coupling between components:

```csharp
// Publisher
public event EventHandler<MyEventArgs>? MyEvent;

protected virtual void OnMyEvent(MyEventArgs e)
{
    MyEvent?.Invoke(this, e);
}

// Subscriber
_myService.MyEvent += OnMyEvent;

private void OnMyEvent(object? sender, MyEventArgs e)
{
    // Handle event
}
```

### Async/Await Pattern

Use async for I/O operations:

```csharp
public async Task<bool> CheckSomethingAsync()
{
    await Task.Delay(1000); // Async delay
    return await SomeApiCallAsync();
}
```

**Avoid:**
- `Task.Wait()` or `.Result` (can cause deadlocks)
- Mixing sync and async code unnecessarily

## Resources

### Win32 APIs Documentation
- [Keyboard Input](https://learn.microsoft.com/en-us/windows/win32/inputdev/keyboard-input)
- [GetKeyboardLayout](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-getkeyboardlayout)
- [ActivateKeyboardLayout](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-activatekeyboardlayout)

### WMI Documentation
- [Win32_USBHub class](https://learn.microsoft.com/en-us/windows/win32/cimwin32prov/win32-usbhub)
- [System.Management namespace](https://learn.microsoft.com/en-us/dotnet/api/system.management)

### .NET Documentation
- [.NET 10 Release Notes](https://github.com/dotnet/core/tree/main/release-notes/10.0)
- [Windows Forms](https://learn.microsoft.com/en-us/dotnet/desktop/winforms/)
- [Generic Host](https://learn.microsoft.com/en-us/dotnet/core/extensions/generic-host)

### Third-Party Libraries
- [Velopack Documentation](https://velopack.io/)
- [Serilog Wiki](https://github.com/serilog/serilog/wiki)
- [xUnit Documentation](https://xunit.net/)
