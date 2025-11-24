using System.Runtime.InteropServices;

namespace KeyboardAutoSwitcher;

/// <summary>
/// Manages keyboard layout operations through Win32 API
/// Handles listing, getting current layout, and activating layouts
/// </summary>
internal static class KeyboardLayout
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

    private static IntPtr[]? _cachedLayoutHandles;

    /// <summary>
    /// Gets all available keyboard layouts installed on the system
    /// </summary>
    public static IntPtr[] GetAvailableLayouts()
    {
        int layoutCount = GetKeyboardLayoutList(0, Array.Empty<IntPtr>());
        var layoutHandles = new IntPtr[layoutCount];
        GetKeyboardLayoutList(layoutCount, layoutHandles);
        return layoutHandles;
    }

    /// <summary>
    /// Refreshes the cache of available keyboard layouts
    /// </summary>
    public static void RefreshLayoutCache()
    {
        _cachedLayoutHandles = GetAvailableLayouts();
    }

    /// <summary>
    /// Gets the currently active keyboard layout for the foreground window
    /// </summary>
    public static KeyboardLayoutConfig? GetCurrentLayout()
    {
        IntPtr hwnd = GetForegroundWindow();
        uint threadId = GetWindowThreadProcessId(hwnd, IntPtr.Zero);
        IntPtr hkl = GetKeyboardLayout(threadId);
        int layoutId = (int)hkl;

        return KeyboardLayouts.GetByLayoutId(layoutId)
            ?? KeyboardLayouts.GetByLanguageId(layoutId & 0xFFFF);
    }

    /// <summary>
    /// Gets the layout ID of the currently active keyboard layout
    /// </summary>
    public static int GetCurrentLayoutId()
    {
        IntPtr hwnd = GetForegroundWindow();
        uint threadId = GetWindowThreadProcessId(hwnd, IntPtr.Zero);
        IntPtr hkl = GetKeyboardLayout(threadId);
        return (int)hkl;
    }

    /// <summary>
    /// Activates the specified keyboard layout
    /// </summary>
    public static void ActivateLayout(KeyboardLayoutConfig targetLayoutConfig)
    {
        // Ensure cache is initialized
        if (_cachedLayoutHandles == null || _cachedLayoutHandles.Length == 0)
        {
            RefreshLayoutCache();
        }

        IntPtr targetLayout = FindLayoutHandle(_cachedLayoutHandles!, targetLayoutConfig);
        if (targetLayout == IntPtr.Zero)
        {
            throw new InvalidOperationException($"Layout not installed: {targetLayoutConfig.DisplayName}");
        }

        ActivateKeyboardLayout(targetLayout, KLF_ACTIVATE);

        // Notify all windows about the layout change
        IntPtr foregroundWindow = GetForegroundWindow();
        if (foregroundWindow != IntPtr.Zero)
        {
            PostMessage(foregroundWindow, WM_INPUTLANGCHANGEREQUEST, IntPtr.Zero, targetLayout);
        }
        PostMessage((IntPtr)HWND_BROADCAST, WM_INPUTLANGCHANGEREQUEST, IntPtr.Zero, targetLayout);
    }

    /// <summary>
    /// Finds the handle for a specific keyboard layout
    /// </summary>
    private static IntPtr FindLayoutHandle(IntPtr[] layoutHandles, KeyboardLayoutConfig targetLayoutConfig)
    {
        // Try exact match first
        foreach (IntPtr hkl in layoutHandles)
        {
            if ((int)hkl == targetLayoutConfig.LayoutId)
            {
                return hkl;
            }
        }

        // Fallback to language ID match
        int targetLangId = targetLayoutConfig.GetLanguageId();
        foreach (IntPtr hkl in layoutHandles)
        {
            if (((int)hkl & 0xFFFF) == targetLangId)
            {
                return hkl;
            }
        }

        return IntPtr.Zero;
    }

    /// <summary>
    /// Gets cached layout handles for debugging purposes
    /// </summary>
    public static IntPtr[]? GetCachedLayoutHandles() => _cachedLayoutHandles;
}