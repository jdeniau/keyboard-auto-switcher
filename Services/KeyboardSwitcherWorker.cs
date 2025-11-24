using System.Runtime.InteropServices;
using KeyboardAutoSwitcher;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace KeyboardAutoSwitcher.Services;

/// <summary>
/// Background worker that monitors keyboard connection and switches layouts automatically
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
    private const int POLLING_INTERVAL_MS = 5000;

    private readonly ILogger<KeyboardSwitcherWorker> _logger;

    public KeyboardSwitcherWorker(ILogger<KeyboardSwitcherWorker> logger)
    {
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Keyboard Auto Switcher worker starting");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                KeyboardLayoutConfig? currentLayout = GetCurrentKeyboardLayout();
                bool isExternalKeyboardConnected = IsKeyboardConnected();

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
                    _logger.LogInformation("Already using {Layout}", currentLayout.DisplayName);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while monitoring/switching keyboard layout");
            }

            try
            {
                await Task.Delay(POLLING_INTERVAL_MS, stoppingToken);
            }
            catch (TaskCanceledException)
            {
                // graceful shutdown
            }
        }

        _logger.LogInformation("Keyboard Auto Switcher worker stopping");
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
        int layoutCount = GetKeyboardLayoutList(0, Array.Empty<IntPtr>());
        IntPtr[] layoutHandles = new IntPtr[layoutCount];
        GetKeyboardLayoutList(layoutCount, layoutHandles);

        _logger.LogInformation("Looking for layout: {Layout} (0x{LayoutId:X8})", targetLayoutConfig.DisplayName, targetLayoutConfig.LayoutId);
        foreach (IntPtr hkl in layoutHandles)
        {
            KeyboardLayoutConfig? knownLayout = KeyboardLayouts.GetByLayoutId((int)hkl);
            string layoutName = knownLayout != null ? knownLayout.DisplayName : "Unknown";
            _logger.LogInformation("  0x{HKL:X8} - {LayoutName}", (int)hkl, layoutName);
        }

        IntPtr targetLayout = FindLayoutHandle(layoutHandles, targetLayoutConfig);
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

    private bool IsKeyboardConnected()
    {
        var usbDevices = USBDeviceInfo.GetUSBDevices();
        _logger.LogInformation("Found {Count} USB devices", usbDevices.Count);

        foreach (var device in usbDevices)
        {
            _logger.LogDebug("USB Device: {PnpDeviceID} - {Description}", device.PnpDeviceID, device.Description);
            if (device.PnpDeviceID.StartsWith(USBDeviceInfo.KeyboardInstanceName))
            {
                _logger.LogInformation("Target keyboard detected: {PnpDeviceID}", device.PnpDeviceID);
                return true;
            }
        }

        _logger.LogInformation("Target keyboard not found (looking for: {KeyboardId})", USBDeviceInfo.KeyboardInstanceName);
        return false;
    }
}
