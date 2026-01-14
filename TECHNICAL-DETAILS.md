# Technical Details

## USB Device Detection

### WMI Query Mechanics

The application uses Windows Management Instrumentation (WMI) to monitor USB devices without polling.

#### Device Identification

USB devices are identified by their PNP (Plug and Play) Device ID format:

```
USB\VID_1E54&PID_2030\SerialNumber
│   │        │        │
│   │        │        └─ Device serial (unique per device)
│   │        └─ Product ID (device model)
│   └─ Vendor ID (manufacturer)
└─ Bus type
```

**TypeMatrix Keyboard:**
- Vendor ID: `1E54` (TypeMatrix)
- Product ID: `2030` (2030 USB)
- Detection pattern: `"USB\\VID_1E54&PID_2030\\"`

#### WMI Event Subscription

The app uses WMI event queries instead of polling:

```csharp
ManagementEventWatcher watcher = new(
    new WqlEventQuery(
        "SELECT * FROM __InstanceOperationEvent " +
        "WITHIN 2 " +  // Poll WMI every 2 seconds
        "WHERE TargetInstance ISA 'Win32_USBHub'"
    )
);
```

**Event Types:**
- `__InstanceCreationEvent` - USB device connected
- `__InstanceDeletionEvent` - USB device disconnected
- `__InstanceModificationEvent` - USB device properties changed

**Why Win32_USBHub?**
- More reliable than `Win32_PnPEntity` for USB devices
- Triggers consistently on plug/unplug
- Less noisy than `Win32_USBControllerDevice`

### Query Performance

**Timeout Protection:**
```csharp
var options = new EnumerationOptions
{
    Timeout = TimeSpan.FromSeconds(5),
    ReturnImmediately = false
};
```

**Retry Logic:**
- First attempt with 5s timeout
- On `ManagementException`: wait 500ms, retry once
- On second failure: fall back to 10s polling

**Why Retry?**
WMI can occasionally fail with "Generic failure" on system load. A single retry with delay resolves 95% of transient failures.

### Fallback Polling Mode

If WMI event subscription fails (security policies, service issues):

```csharp
_pollingTimer = new Timer(10000); // 10 seconds
_pollingTimer.Elapsed += (s, e) => {
    bool connected = IsTargetKeyboardConnected();
    // Check if state changed, fire event
};
```

**Performance Impact:**
- WMI Event Mode: ~0% CPU, events triggered instantly
- Polling Mode: ~0.1% CPU, up to 10s detection delay

## Keyboard Layout Switching

### Layout Identification

Windows identifies keyboard layouts using a 32-bit Layout ID:

```
Layout ID (32-bit): 0xLLLLKKKK
                      │    │
                      │    └─ Keyboard Layout ID (KLID) - 16 bits
                      └─ Language ID (LANGID) - 16 bits
```

**Examples:**
- `0xF0020409` = US-Dvorak
  - Language: `0x0409` (en-US)
  - Layout: `0xF002` (Dvorak variant)
- `0x040C040C` = French AZERTY
  - Language: `0x040C` (fr-FR)
  - Layout: `0x040C` (standard)

### Win32 API Flow

#### 1. Enumerate Installed Layouts

```csharp
int count = GetKeyboardLayoutList(0, null);
IntPtr[] hkls = new IntPtr[count];
GetKeyboardLayoutList(hkls.Length, hkls);

// hkls now contains handles to all installed layouts
```

#### 2. Get Current Layout

```csharp
// Get foreground window
IntPtr hwnd = GetForegroundWindow();

// Get window's thread
uint threadId = GetWindowThreadProcessId(hwnd, IntPtr.Zero);

// Get thread's keyboard layout
IntPtr hkl = GetKeyboardLayout(threadId);

// hkl is the layout handle (same as Layout ID)
```

**Why thread-specific?**
Each thread in Windows can have a different keyboard layout. We target the foreground window's thread to switch the actively-used layout.

#### 3. Activate New Layout

```csharp
// Activate layout (0x00000100 = KLF_ACTIVATE)
ActivateKeyboardLayout(targetHkl, 0x00000100);

// Notify all windows of language change
PostMessage(HWND_BROADCAST, WM_INPUTLANGCHANGEREQUEST,
            IntPtr.Zero, targetHkl);
```

**Flags:**
- `KLF_ACTIVATE (0x00000100)`: Activate layout for current thread
- `KLF_SETFORPROCESS (0x00000100)`: Set for entire process (less reliable)

**Broadcasting:**
`WM_INPUTLANGCHANGEREQUEST` notifies all windows to refresh their language bars and input method indicators.

### Layout Matching Strategy

The app uses a two-tier matching system:

```csharp
// 1. Try exact Layout ID match
var exactMatch = layouts.FirstOrDefault(l =>
    l.LayoutId == targetConfig.LayoutId);

if (exactMatch != null)
    return exactMatch.Handle;

// 2. Fallback: match lower 16 bits (Language ID)
var languageMatch = layouts.FirstOrDefault(l =>
    (l.LayoutId & 0xFFFF) == (targetConfig.LayoutId & 0xFFFF));

return languageMatch?.Handle;
```

**Why two-tier?**
- Some systems have multiple variants of same language (e.g., US-QWERTY, US-Dvorak, US-Colemak)
- Exact match ensures correct variant
- Language match provides fallback if exact variant not installed

### Caching Strategy

Layouts are cached on startup and refreshed only when needed:

```csharp
private static List<LayoutInfo>? _cachedLayouts;

public static void RefreshLayoutCache()
{
    _cachedLayouts = GetInstalledLayouts().ToList();
}
```

**Cache Invalidation:**
- On application start
- After layout installation (not currently detected)
- Manual refresh (not exposed)

**Why cache?**
`GetKeyboardLayoutList()` is relatively expensive (~10ms). Caching reduces latency when switching layouts frequently.

## System Event Handling

### Power Mode Changes

```csharp
SystemEvents.PowerModeChanged += (s, e) => {
    if (e.Mode == PowerModes.Resume)
    {
        // Wait 2 seconds for USB re-enumeration
        await Task.Delay(2000);
        await CheckAndSetKeyboardLayoutAsync();
    }
};
```

**Why 2 seconds delay?**
- USB hub controllers reinitialize after sleep
- Device enumeration takes 500-1500ms
- 2s ensures devices are recognized before checking

### Session Switch Events

```csharp
SystemEvents.SessionSwitch += (s, e) => {
    switch (e.Reason)
    {
        case SessionSwitchReason.SessionUnlock:
        case SessionSwitchReason.RemoteConnect:
        case SessionSwitchReason.ConsoleConnect:
            // Wait 500ms for session stabilization
            await Task.Delay(500);
            await CheckAndSetKeyboardLayoutAsync();
            break;
    }
};
```

**Handled Reasons:**
- `SessionUnlock`: User unlocks workstation (Ctrl+Alt+Del → unlock)
- `RemoteConnect`: Remote Desktop connection established
- `ConsoleConnect`: Switch from remote to local console

**Not Handled:**
- `SessionLock`: Layout doesn't matter when locked
- `SessionLogon`/`SessionLogoff`: App may not be running

**Why 500ms delay?**
- Windows resets some session state on unlock
- Input system needs time to reinitialize
- 500ms ensures stable state before layout change

### Event Thread Safety

All system events fire on background threads. UI updates require marshaling:

```csharp
if (_trayIcon.InvokeRequired)
{
    _trayIcon.Invoke(() => UpdateIconAndTooltip(layout));
}
else
{
    UpdateIconAndTooltip(layout);
}
```

**Why?**
Windows Forms controls are not thread-safe. Accessing from non-UI thread throws `InvalidOperationException`.

## Auto-Update System

### Velopack Architecture

Velopack uses a "squirrel-like" update mechanism:

```
KeyboardAutoSwitcher.exe
    │
    ├─→ On startup: VelopackApp.Build().Run()
    │   └─→ Checks if launched by Update.exe
    │       └─→ Finalizes pending updates
    │
    └─→ In runtime: UpdateManager.CheckForUpdatesAsync()
        └─→ Queries GitHub Releases API
            ├─→ Finds newer version
            ├─→ Downloads .nupkg
            ├─→ Extracts to temp
            ├─→ Calls Update.exe
            └─→ Restarts app
```

### Update Detection

```csharp
var updateInfo = await _updateManager.CheckForUpdatesAsync();

if (updateInfo != null)
{
    Version currentVersion = Assembly.GetEntryAssembly()
        .GetName().Version;

    Version newVersion = updateInfo.TargetFullRelease.Version;

    if (newVersion > currentVersion)
    {
        // Update available
    }
}
```

**Version Comparison:**
Uses semantic versioning: `major.minor.patch.build`

**GitHub API Rate Limits:**
- Unauthenticated: 60 requests/hour per IP
- App checks once on startup (silent)
- User-initiated checks don't count toward startup limit

### Update Download

```csharp
await _updateManager.DownloadUpdatesAsync(
    updateInfo,
    progress: p => {
        int percent = (int)(p * 100);
        UpdateMenuText($"Téléchargement: {percent}%");
    }
);
```

**Download Location:**
- Temporary folder: `%LOCALAPPDATA%\Temp\VelopackTemp_*`
- Automatically cleaned on success/failure

### Update Application

```csharp
_updateManager.ApplyUpdatesAndRestart(updateInfo);
```

**Process:**
1. Extract `.nupkg` to installation directory
2. Run `Update.exe --processStart KeyboardAutoSwitcher.exe`
3. `Update.exe` waits for main process to exit
4. Replaces files
5. Restarts `KeyboardAutoSwitcher.exe`
6. `Update.exe` exits

**Rollback:**
Velopack keeps previous version in `packages\` directory for rollback on failure.

## Logging System

### Serilog Configuration

```csharp
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("System", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File(
        path: logPath,
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 7,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level:u3}] {Message:lj}{NewLine}{Exception}"
    )
    .CreateLogger();
```

**Log Levels:**
- `Information`: Normal operations (startup, layout changes, USB events)
- `Warning`: Recoverable issues (WMI timeouts, retry attempts)
- `Error`: Failures (WMI access denied, update failures)

**Framework Filtering:**
- Microsoft/System libraries: Warning+ only (reduces noise)

### Log Rotation

**Daily Rotation:**
- Files named: `log-20260114.txt`
- New file at midnight
- Automatically deletes logs >7 days old

**Why Daily?**
- Manageable file size (~100KB/day typical usage)
- Easy to find logs by date
- Automatic cleanup prevents disk fill

### Log Viewer

The UI includes a custom log viewer (`LogViewerForm.cs`):

**Features:**
- Syntax highlighting by log level
  - Information: Default color
  - Warning: Orange
  - Error: Red
- Auto-refresh every second
- Theme-aware colors (light/dark mode)
- Regex-based parsing of timestamp, level, message

**Performance:**
- Reads last 1000 lines only
- Uses `File.Open` with `FileShare.ReadWrite` to avoid locking

## Icon Generation

### Dynamic Icon Creation

Icons are generated programmatically based on keyboard layout:

```csharp
var bitmap = new Bitmap(32, 32, PixelFormat.Format32bppArgb);
var graphics = Graphics.FromImage(bitmap);

graphics.SmoothingMode = SmoothingMode.AntiAlias;
graphics.TextRenderingHint = TextRenderingHint.AntiAlias;

// Draw rounded rectangle background
var path = CreateRoundedRectangle(2, 2, 28, 28, 4);
graphics.FillPath(new SolidBrush(bgColor), path);

// Draw text centered
string text = GetLayoutAbbreviation(layout);
var font = new Font("Segoe UI", 9, FontStyle.Bold);
var textSize = graphics.MeasureString(text, font);
float x = (32 - textSize.Width) / 2;
float y = (32 - textSize.Height) / 2;
graphics.DrawString(text, font, Brushes.White, x, y);
```

**Color Scheme:**
- **Dvorak (en-US)**: Green `#4CAF50` + "DV"
- **AZERTY (fr-FR)**: Blue `#2196F3` + "AZ"
- **Unknown**: Gray `#9E9E9E` + "KB"

**Why Dynamic?**
- Easy to add new layouts without icon files
- Consistent visual style
- Smaller application size (no icon resources)
- Runtime adaptation (could change colors based on theme)

### Icon Conversion

```csharp
IntPtr hIcon = bitmap.GetHicon();
Icon icon = Icon.FromHandle(hIcon);
```

**Memory Management:**
- `GetHicon()` creates unmanaged icon handle
- Must call `DestroyIcon()` to free (handled by `Icon.Dispose()`)
- Bitmap disposed after conversion

## Registry Integration

### Startup Management

The app can register itself to run at Windows startup:

```csharp
// Registry path
const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
const string AppName = "KeyboardAutoSwitcher";

// Enable startup
registry.SetValue(RunKey, AppName,
    $"\"{exePath}\"",  // Quoted path (handles spaces)
    RegistryValueKind.String);

// Disable startup
registry.DeleteValue(RunKey, AppName);
```

**Why HKCU (Current User)?**
- No admin rights required
- Per-user setting (correct for user-specific layouts)
- Runs in user context (can access user's UI session)

### Theme Detection

```csharp
const string ThemePath = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
const string LightThemeValue = "AppsUseLightTheme";

int value = (int)Registry.GetValue(
    @"HKEY_CURRENT_USER\" + ThemePath,
    LightThemeValue,
    1  // Default to light theme
);

bool isLightTheme = value != 0;
```

**Values:**
- `0` = Dark mode
- `1` = Light mode
- Missing key = Light mode (Windows default)

**Live Updates:**
```csharp
SystemEvents.UserPreferenceChanged += (s, e) => {
    if (e.Category == UserPreferenceCategory.General)
    {
        // Theme may have changed, re-detect
        UpdateTheme();
    }
};
```

## Performance Characteristics

### CPU Usage
- **Idle (event mode)**: <0.1%
- **Idle (polling mode)**: ~0.1%
- **During layout switch**: ~1% for <100ms
- **During update check**: ~2% for <1s

### Memory Usage
- **Baseline**: ~20 MB
- **With log viewer open**: ~25 MB
- **During update download**: +10 MB (temporary)

### Startup Time
- **Cold start**: ~200ms
- **With update check**: +500ms (async, non-blocking)

### Network Usage
- **Update check**: ~5 KB (GitHub API JSON)
- **Update download**: ~15-20 MB per version

## Security Considerations

### Permissions Required
- **WMI Access**: Read-only access to USB device info
- **Registry Write**: `HKCU\Software\Microsoft\Windows\CurrentVersion\Run`
- **Keyboard Layout**: System-level keyboard control (no special permission)
- **Network**: HTTPS to GitHub API (outbound only)

### No Admin Required
The application runs entirely in user context with standard permissions.

### Update Verification
Velopack verifies package signatures (when configured). Currently, the project uses GitHub Releases without signature verification (standard for open-source tools).

**Future Enhancement:** Add code signing for executables.

### Log Sensitivity
Logs may contain:
- USB device IDs (VID/PID/Serial)
- User's keyboard layout preferences
- System events (wake/unlock times)

**Recommendation:** Logs stored in `ProgramData` are accessible to all users on the machine. Consider per-user storage for privacy-sensitive deployments.
