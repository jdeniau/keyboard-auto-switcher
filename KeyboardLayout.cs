using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32;

namespace KeyboardAutoSwitcher
{
    /// <summary>
    /// Represents an installed keyboard layout on the system
    /// </summary>
    public class InstalledKeyboardLayout
    {
        public int LayoutId { get; init; }
        public string DisplayName { get; init; } = string.Empty;
        public string LanguageTag { get; init; } = string.Empty;

        public override string ToString()
        {
            return DisplayName;
        }
    }

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

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern int GetKeyboardLayoutNameW(StringBuilder pwszKLID);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr LoadKeyboardLayoutW(string pwszKLID, uint Flags);

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
            int layoutCount = GetKeyboardLayoutList(0, []);
            nint[] layoutHandles = new IntPtr[layoutCount];
            _ = GetKeyboardLayoutList(layoutCount, layoutHandles);
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

            _ = ActivateKeyboardLayout(targetLayout, KLF_ACTIVATE);

            // Notify all windows about the layout change
            IntPtr foregroundWindow = GetForegroundWindow();
            if (foregroundWindow != IntPtr.Zero)
            {
                _ = PostMessage(foregroundWindow, WM_INPUTLANGCHANGEREQUEST, IntPtr.Zero, targetLayout);
            }
            _ = PostMessage(HWND_BROADCAST, WM_INPUTLANGCHANGEREQUEST, IntPtr.Zero, targetLayout);
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
        public static IntPtr[]? GetCachedLayoutHandles()
        {
            return _cachedLayoutHandles;
        }

        /// <summary>
        /// Gets all installed keyboard layouts on the system with their display names
        /// </summary>
        public static List<InstalledKeyboardLayout> GetInstalledLayouts()
        {
            List<InstalledKeyboardLayout> layouts = [];
            IntPtr[] handles = GetAvailableLayouts();

            foreach (IntPtr hkl in handles)
            {
                int layoutId = (int)hkl;
                string displayName = GetLayoutDisplayName(layoutId);
                string languageTag = GetLanguageTag(layoutId);

                layouts.Add(new InstalledKeyboardLayout
                {
                    LayoutId = layoutId,
                    DisplayName = displayName,
                    LanguageTag = languageTag
                });
            }

            return layouts;
        }

        /// <summary>
        /// Gets the display name for a keyboard layout
        /// </summary>
        private static string GetLayoutDisplayName(int layoutId)
        {
            try
            {
                // Try to get the specific layout name from registry
                string? layoutName = GetLayoutNameFromRegistry(layoutId);

                if (!string.IsNullOrEmpty(layoutName))
                {
                    return layoutName;
                }

                // Fallback to culture name with layout ID for unknown variants
                int langId = layoutId & 0xFFFF;
                CultureInfo culture = CultureInfo.GetCultureInfo(langId);
                int layoutVariant = (layoutId >> 16) & 0xFFFF;

                return layoutVariant != 0 && layoutVariant != langId
                    ? $"{culture.DisplayName} (0x{layoutId:X8})"
                    : culture.DisplayName;
            }
            catch
            {
                return $"Unknown Layout (0x{layoutId:X8})";
            }
        }

        /// <summary>
        /// Tries to get the layout name from Windows registry
        /// </summary>
        private static string? GetLayoutNameFromRegistry(int layoutId)
        {
            try
            {
                using RegistryKey? layoutsKey = Registry.LocalMachine.OpenSubKey(
                    @"SYSTEM\CurrentControlSet\Control\Keyboard Layouts");

                if (layoutsKey == null)
                {
                    return null;
                }

                int langId = layoutId & 0xFFFF;
                int deviceId = (layoutId >> 16) & 0xFFFF;

                // Check if there's a substitute for this layout
                // Substitutes map preload IDs to actual layout IDs
                string? substituteLayoutId = GetSubstituteLayout(langId);

                // Build a list of registry keys to try, in order of preference
                List<string> keysToTry = [];

                // If we have a substitute, try it first
                if (!string.IsNullOrEmpty(substituteLayoutId))
                {
                    keysToTry.Add(substituteLayoutId);
                }

                // For extended layouts (high bits set), we need to find the matching layout
                // Windows uses the pattern where HKL high bits (like F002) map to registry keys (like 0001)
                if (deviceId != 0 && deviceId != langId)
                {
                    // Try common extended layout patterns for this language
                    // Extended layouts have format 000XLLLL where X is the variant number
                    for (int variant = 1; variant <= 15; variant++)
                    {
                        keysToTry.Add($"{variant:X4}{langId:X4}");
                    }
                }

                // Standard layout (0000LLLL)
                keysToTry.Add($"0000{langId:X4}");

                // Try each key until we find one
                foreach (string keyName in keysToTry)
                {
                    using RegistryKey? subKey = layoutsKey.OpenSubKey(keyName);
                    if (subKey != null)
                    {
                        string? name = subKey.GetValue("Layout Text") as string;
                        if (!string.IsNullOrEmpty(name))
                        {
                            return name;
                        }
                    }
                }

                return null;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Gets the substitute layout ID for a given language ID from user preferences
        /// </summary>
        private static string? GetSubstituteLayout(int langId)
        {
            try
            {
                using RegistryKey? substitutesKey = Registry.CurrentUser.OpenSubKey(
                    @"Keyboard Layout\Substitutes");

                if (substitutesKey == null)
                {
                    return null;
                }

                // Look for a substitute for this language's standard layout
                string standardLayoutId = $"0000{langId:X4}";
                return substitutesKey.GetValue(standardLayoutId) as string;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Gets the language tag (e.g., "en-US") for a layout
        /// </summary>
        private static string GetLanguageTag(int layoutId)
        {
            try
            {
                int langId = layoutId & 0xFFFF;
                CultureInfo culture = CultureInfo.GetCultureInfo(langId);
                return culture.Name;
            }
            catch
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// Activates a keyboard layout by its layout ID
        /// </summary>
        public static void ActivateLayoutById(int layoutId)
        {
            // Ensure cache is initialized
            if (_cachedLayoutHandles == null || _cachedLayoutHandles.Length == 0)
            {
                RefreshLayoutCache();
            }

            IntPtr targetLayout = FindLayoutHandleById(_cachedLayoutHandles!, layoutId);
            if (targetLayout == IntPtr.Zero)
            {
                throw new InvalidOperationException($"Layout not installed: 0x{layoutId:X8}");
            }

            _ = ActivateKeyboardLayout(targetLayout, KLF_ACTIVATE);

            // Notify all windows about the layout change
            IntPtr foregroundWindow = GetForegroundWindow();
            if (foregroundWindow != IntPtr.Zero)
            {
                _ = PostMessage(foregroundWindow, WM_INPUTLANGCHANGEREQUEST, IntPtr.Zero, targetLayout);
            }
            _ = PostMessage(HWND_BROADCAST, WM_INPUTLANGCHANGEREQUEST, IntPtr.Zero, targetLayout);
        }

        /// <summary>
        /// Finds the handle for a specific layout ID
        /// </summary>
        private static IntPtr FindLayoutHandleById(IntPtr[] layoutHandles, int targetLayoutId)
        {
            // Try exact match first
            foreach (IntPtr hkl in layoutHandles)
            {
                if ((int)hkl == targetLayoutId)
                {
                    return hkl;
                }
            }

            // Fallback to language ID match
            int targetLangId = targetLayoutId & 0xFFFF;
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
        /// Gets the display name for a layout ID
        /// </summary>
        public static string GetDisplayNameForLayoutId(int layoutId)
        {
            return GetLayoutDisplayName(layoutId);
        }
    }
}
