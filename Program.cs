using System.Runtime.InteropServices;
using KeyboardAutoSwitcher;

/// <summary>
/// Application context that monitors keyboard connection and switches layouts automatically
/// </summary>
class KeyboardSwitcherApp : ApplicationContext
{
    // Win32 API imports
    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, IntPtr ProcessId);

    [DllImport("user32.dll")]
    private static extern IntPtr GetKeyboardLayout(uint idThread);

    [DllImport("user32.dll")]
    private static extern IntPtr ActivateKeyboardLayout(IntPtr hkl, uint flags);

    [DllImport("user32.dll")]
    private static extern int GetKeyboardLayoutList(int nBuff, [Out] IntPtr[] lpList);

    [DllImport("user32.dll")]
    private static extern bool PostMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    // Constants
    private const uint KLF_ACTIVATE = 0x00000001;
    private const uint WM_INPUTLANGCHANGEREQUEST = 0x0050;
    private const int HWND_BROADCAST = 0xFFFF;
    private const int POLLING_INTERVAL_MS = 5000;

    private KeyboardSwitcherApp()
    {
        MonitorKeyboardAndSwitchLayout();
    }

    /// <summary>
    /// Main monitoring loop - checks keyboard status and switches layout accordingly
    /// </summary>
    private void MonitorKeyboardAndSwitchLayout()
    {
        while (true)
        {
            KeyboardLayoutConfig? currentLayout = GetCurrentKeyboardLayout();
            bool isExternalKeyboardConnected = IsKeyboardConnected();

            KeyboardLayoutConfig targetLayout = isExternalKeyboardConnected
                ? KeyboardLayouts.UsDvorak
                : KeyboardLayouts.FrenchStandard;

            Console.WriteLine(isExternalKeyboardConnected
                ? "✓ External keyboard detected"
                : "✗ No external keyboard");

            if (currentLayout == null || currentLayout.LayoutId != targetLayout.LayoutId)
            {
                Console.WriteLine($"→ Switching to {targetLayout.DisplayName}...");
                SetKeyboardLayout(targetLayout);
            }
            else
            {
                Console.WriteLine($"✓ Already using {currentLayout.DisplayName}");
            }

            Thread.Sleep(POLLING_INTERVAL_MS);
        }
    }

    /// <summary>
    /// Get the current keyboard layout configuration
    /// </summary>
    private KeyboardLayoutConfig? GetCurrentKeyboardLayout()
    {
        // Get the foreground window handle
        IntPtr hwnd = GetForegroundWindow();

        // Get the thread ID of the foreground window
        uint threadId = GetWindowThreadProcessId(hwnd, IntPtr.Zero);

        // Get the keyboard layout for that thread
        IntPtr hkl = GetKeyboardLayout(threadId);
        int layoutId = (int)hkl;

        // Try exact match first, then fallback to language ID
        KeyboardLayoutConfig? layout = KeyboardLayouts.GetByLayoutId(layoutId)
            ?? KeyboardLayouts.GetByLanguageId(layoutId & 0xFFFF);

        Console.WriteLine(
            $"Current layout: 0x{layoutId:X8} => " +
            (layout != null ? layout.DisplayName : $"Unknown (lang: {(layoutId & 0xFFFF):X4})")
        );

        return layout;
    }

    /// <summary>
    /// Set the current keyboard layout
    /// </summary>
    private void SetKeyboardLayout(KeyboardLayoutConfig targetLayoutConfig)
    {
        // Get all installed layouts and find the target one
        int layoutCount = GetKeyboardLayoutList(0, Array.Empty<IntPtr>());
        IntPtr[] layoutHandles = new IntPtr[layoutCount];
        GetKeyboardLayoutList(layoutCount, layoutHandles);

        Console.WriteLine($"Looking for layout: {targetLayoutConfig.DisplayName} (0x{targetLayoutConfig.LayoutId:X8})");
        Console.WriteLine("Available layouts:");
        foreach (IntPtr hkl in layoutHandles)
        {
            KeyboardLayoutConfig? knownLayout = KeyboardLayouts.GetByLayoutId((int)hkl);
            string layoutName = knownLayout != null ? knownLayout.DisplayName : "Unknown";
            Console.WriteLine($"  0x{(int)hkl:X8} - {layoutName}");
        }

        IntPtr targetLayout = FindLayoutHandle(layoutHandles, targetLayoutConfig);

        if (targetLayout == IntPtr.Zero)
        {
            Console.Error.WriteLine($"❌ Layout not installed: {targetLayoutConfig.DisplayName}");
            return;
        }

        Console.WriteLine($"Activating layout: 0x{(int)targetLayout:X8}");

        // First, activate the layout for current thread
        ActivateKeyboardLayout(targetLayout, KLF_ACTIVATE);

        // Then, broadcast the change to all windows
        IntPtr foregroundWindow = GetForegroundWindow();
        if (foregroundWindow != IntPtr.Zero)
        {
            PostMessage(foregroundWindow, WM_INPUTLANGCHANGEREQUEST, IntPtr.Zero, targetLayout);
        }

        PostMessage((IntPtr)HWND_BROADCAST, WM_INPUTLANGCHANGEREQUEST, IntPtr.Zero, targetLayout);

        Console.WriteLine($"✓ Switched to: {targetLayoutConfig.DisplayName}");
    }

    /// <summary>
    /// Find the handle for a specific layout configuration
    /// </summary>
    private IntPtr FindLayoutHandle(IntPtr[] layoutHandles, KeyboardLayoutConfig targetLayoutConfig)
    {
        // Try exact match first
        foreach (IntPtr hkl in layoutHandles)
        {
            if ((int)hkl == targetLayoutConfig.LayoutId)
            {
                return hkl;
            }
        }

        // Fallback: match by language ID only (lower 16 bits)
        int targetLangId = targetLayoutConfig.GetLanguageId();
        Console.WriteLine($"Exact layout not found, searching by language ID: 0x{targetLangId:X4}");

        foreach (IntPtr hkl in layoutHandles)
        {
            if (((int)hkl & 0xFFFF) == targetLangId)
            {
                Console.WriteLine($"Found layout by language ID: 0x{(int)hkl:X8}");
                return hkl;
            }
        }

        return IntPtr.Zero;
    }

    /// <summary>
    /// Check if an external USB keyboard is connected
    /// </summary>
    private bool IsKeyboardConnected()
    {
        var usbDevices = USBDeviceInfo.GetUSBDevices();
        return usbDevices.Any(d => d.PnpDeviceID.StartsWith(USBDeviceInfo.KeyboardInstanceName));
    }

    [STAThread]
    static void Main(string[] args)
    {
        KeyboardSwitcherApp context = new KeyboardSwitcherApp();
        Application.Run(context);
    }
}

