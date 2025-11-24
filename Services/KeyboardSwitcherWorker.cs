using System.Management;
using System.Runtime.InteropServices;
using KeyboardAutoSwitcher;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;

namespace KeyboardAutoSwitcher.Services;

/// <summary>
/// Background worker that monitors keyboard connection and switches layouts automatically
/// Uses event-based USB monitoring and power events instead of polling for better performance
/// </summary>
public class KeyboardSwitcherWorker : BackgroundService
{
    // Win32 API imports
    [DllImport("user32.dll")] private static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(IntPtr hWnd, IntPtr ProcessId);
    [DllImport("user32.dll")] private static extern IntPtr GetKeyboardLayout(uint idThread);
    [DllImport("user32.dll")] private static extern IntPtr ActivateKeyboardLayout(IntPtr hkl, uint flags);
    [DllImport("user32.dll")] private static extern int GetKeyboardLayoutList(int nBuff, [Out] IntPtr[] lpList);
    [DllImport("user32.dll")] private static extern bool PostMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    // Constants
    private const uint KLF_ACTIVATE = 0x00000001;
    private const uint WM_INPUTLANGCHANGEREQUEST = 0x0050;
    private const int HWND_BROADCAST = 0xFFFF;

    private readonly ILogger<KeyboardSwitcherWorker> _logger;
    private ManagementEventWatcher? _usbWatcher;
    private IntPtr[]? _cachedLayoutHandles; // Cache to avoid repeated allocations

    public KeyboardSwitcherWorker(ILogger<KeyboardSwitcherWorker> logger)
    {
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Keyboard Auto Switcher worker starting (event-based monitoring)");

        // Initialize layout cache
        RefreshLayoutCache();

        // Check initial state
        CheckAndSwitchLayout();

        // Set up power event monitoring (for resume from sleep)
        SystemEvents.PowerModeChanged += OnPowerModeChanged;
        _logger.LogInformation("Power mode monitoring started");

        // Set up session switch monitoring (for lock/unlock)
        SystemEvents.SessionSwitch += OnSessionSwitch;
        _logger.LogInformation("Session monitoring started");

        // Set up USB event monitoring
        try
        {
            _usbWatcher = USBDeviceInfo.CreateUSBWatcher(OnUSBDeviceEvent);
            _usbWatcher.Start();
            _logger.LogInformation("USB event monitoring started");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start USB event monitoring, falling back to polling");
            SystemEvents.PowerModeChanged -= OnPowerModeChanged;
            SystemEvents.SessionSwitch -= OnSessionSwitch;
            await FallbackPollingMode(stoppingToken);
            return;
        }

        // Wait for cancellation
        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (TaskCanceledException)
        {
            // graceful shutdown
        }
        finally
        {
            SystemEvents.PowerModeChanged -= OnPowerModeChanged;
            SystemEvents.SessionSwitch -= OnSessionSwitch;
            _usbWatcher?.Stop();
            _usbWatcher?.Dispose();
            _logger.LogInformation("Keyboard Auto Switcher worker stopping");
        }
    }

    private void OnPowerModeChanged(object sender, PowerModeChangedEventArgs e)
    {
        if (e.Mode == PowerModes.Resume)
        {
            _logger.LogInformation("System resumed from sleep/hibernation");
            // Small delay to let USB devices re-enumerate
            Task.Delay(2000).ContinueWith(_ => CheckAndSwitchLayout());
        }
    }

    private void OnSessionSwitch(object sender, SessionSwitchEventArgs e)
    {
        switch (e.Reason)
        {
            case SessionSwitchReason.SessionUnlock:
                _logger.LogInformation("Session unlocked");
                // Small delay to ensure session is fully restored
                Task.Delay(500).ContinueWith(_ => CheckAndSwitchLayout());
                break;
            case SessionSwitchReason.SessionLock:
                _logger.LogDebug("Session locked");
                break;
            case SessionSwitchReason.RemoteConnect:
                _logger.LogInformation("Remote session connected");
                Task.Delay(500).ContinueWith(_ => CheckAndSwitchLayout());
                break;
            case SessionSwitchReason.ConsoleConnect:
                _logger.LogInformation("Console session connected");
                Task.Delay(500).ContinueWith(_ => CheckAndSwitchLayout());
                break;
        }
    }

    private void OnUSBDeviceEvent(object sender, EventArrivedEventArgs e)
    {
        try
        {
            _logger.LogDebug("USB device event detected");
            CheckAndSwitchLayout();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling USB event");
        }
        finally
        {
            e.NewEvent?.Dispose(); // Prevent memory leak
        }
    }

    private void CheckAndSwitchLayout()
    {
        try
        {
            KeyboardLayoutConfig? currentLayout = GetCurrentKeyboardLayout();
            bool isExternalKeyboardConnected = USBDeviceInfo.IsTargetKeyboardConnected();

            KeyboardLayoutConfig targetLayout = isExternalKeyboardConnected
                ? KeyboardLayouts.UsDvorak
                : KeyboardLayouts.FrenchStandard;

            _logger.LogInformation(isExternalKeyboardConnected
                ? "External keyboard detected"
                : "No external keyboard");

            if (currentLayout == null || currentLayout.LayoutId != targetLayout.LayoutId)
            {
                _logger.LogInformation("Switching to {Layout}...", targetLayout.DisplayName);
                SetKeyboardLayout(targetLayout);
            }
            else
            {
                _logger.LogDebug("Already using {Layout}", currentLayout.DisplayName);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while checking/switching keyboard layout");
        }
    }

    /// <summary>
    /// Fallback to polling mode if event monitoring fails
    /// </summary>
    private async Task FallbackPollingMode(CancellationToken stoppingToken)
    {
        _logger.LogWarning("Running in polling mode (less efficient)");
        const int POLLING_INTERVAL_MS = 10000; // 10 seconds for fallback mode

        while (!stoppingToken.IsCancellationRequested)
        {
            CheckAndSwitchLayout();

            try
            {
                await Task.Delay(POLLING_INTERVAL_MS, stoppingToken);
            }
            catch (TaskCanceledException)
            {
                break;
            }
        }
    }

    private void RefreshLayoutCache()
    {
        int layoutCount = GetKeyboardLayoutList(0, Array.Empty<IntPtr>());
        _cachedLayoutHandles = new IntPtr[layoutCount];
        GetKeyboardLayoutList(layoutCount, _cachedLayoutHandles);
        _logger.LogDebug("Layout cache refreshed: {Count} layouts available", layoutCount);
    }

    private KeyboardLayoutConfig? GetCurrentKeyboardLayout()
    {
        IntPtr hwnd = GetForegroundWindow();
        uint threadId = GetWindowThreadProcessId(hwnd, IntPtr.Zero);
        IntPtr hkl = GetKeyboardLayout(threadId);
        int layoutId = (int)hkl;

        KeyboardLayoutConfig? layout = KeyboardLayouts.GetByLayoutId(layoutId)
            ?? KeyboardLayouts.GetByLanguageId(layoutId & 0xFFFF);

        _logger.LogInformation("Current layout: 0x{LayoutId:X8} => {Layout}", layoutId,
            layout != null ? layout.DisplayName : $"Unknown (lang: {(layoutId & 0xFFFF):X4})");

        return layout;
    }

    private void SetKeyboardLayout(KeyboardLayoutConfig targetLayoutConfig)
    {
        // Refresh cache if needed (e.g., if user installs new keyboard layouts)
        if (_cachedLayoutHandles == null || _cachedLayoutHandles.Length == 0)
        {
            RefreshLayoutCache();
        }

        _logger.LogDebug("Looking for layout: {Layout} (0x{LayoutId:X8})", targetLayoutConfig.DisplayName, targetLayoutConfig.LayoutId);

        if (_logger.IsEnabled(LogLevel.Debug))
        {
            foreach (IntPtr hkl in _cachedLayoutHandles!)
            {
                KeyboardLayoutConfig? knownLayout = KeyboardLayouts.GetByLayoutId((int)hkl);
                string layoutName = knownLayout != null ? knownLayout.DisplayName : "Unknown";
                _logger.LogDebug("  0x{HKL:X8} - {LayoutName}", (int)hkl, layoutName);
            }
        }

        IntPtr targetLayout = FindLayoutHandle(_cachedLayoutHandles!, targetLayoutConfig);
        if (targetLayout == IntPtr.Zero)
        {
            _logger.LogWarning("Layout not installed: {Layout}", targetLayoutConfig.DisplayName);
            return;
        }

        _logger.LogInformation("Activating layout: 0x{HKL:X8}", (int)targetLayout);

        ActivateKeyboardLayout(targetLayout, KLF_ACTIVATE);

        IntPtr foregroundWindow = GetForegroundWindow();
        if (foregroundWindow != IntPtr.Zero)
        {
            PostMessage(foregroundWindow, WM_INPUTLANGCHANGEREQUEST, IntPtr.Zero, targetLayout);
        }
        PostMessage((IntPtr)HWND_BROADCAST, WM_INPUTLANGCHANGEREQUEST, IntPtr.Zero, targetLayout);

        _logger.LogInformation("Switched to: {Layout}", targetLayoutConfig.DisplayName);
    }

    private IntPtr FindLayoutHandle(IntPtr[] layoutHandles, KeyboardLayoutConfig targetLayoutConfig)
    {
        foreach (IntPtr hkl in layoutHandles)
        {
            if ((int)hkl == targetLayoutConfig.LayoutId)
            {
                return hkl;
            }
        }

        int targetLangId = targetLayoutConfig.GetLanguageId();
        _logger.LogInformation("Exact layout not found, searching by language ID: 0x{LangId:X4}", targetLangId);

        foreach (IntPtr hkl in layoutHandles)
        {
            if (((int)hkl & 0xFFFF) == targetLangId)
            {
                _logger.LogInformation("Found layout by language ID: 0x{HKL:X8}", (int)hkl);
                return hkl;
            }
        }

        return IntPtr.Zero;
    }

}
