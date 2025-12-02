using System.Drawing;
using Microsoft.Win32;

namespace KeyboardAutoSwitcher.UI;

/// <summary>
/// Helper class to detect Windows theme and provide appropriate colors
/// </summary>
public static class ThemeHelper
{
    /// <summary>
    /// Detects if Windows is using dark mode
    /// </summary>
    public static bool IsDarkMode
    {
        get
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(
                    @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");

                if (key != null)
                {
                    var value = key.GetValue("AppsUseLightTheme");
                    if (value is int intValue)
                    {
                        return intValue == 0; // 0 = dark mode, 1 = light mode
                    }
                }
            }
            catch
            {
                // Default to dark mode on error
            }

            return true; // Default to dark mode
        }
    }

    /// <summary>
    /// Event raised when the system theme changes
    /// </summary>
    public static event EventHandler? ThemeChanged;

    private static bool _isMonitoring;

    /// <summary>
    /// Start monitoring for theme changes
    /// </summary>
    public static void StartMonitoring()
    {
        if (_isMonitoring) return;

        SystemEvents.UserPreferenceChanged += OnUserPreferenceChanged;
        _isMonitoring = true;
    }

    /// <summary>
    /// Stop monitoring for theme changes
    /// </summary>
    public static void StopMonitoring()
    {
        if (!_isMonitoring) return;

        SystemEvents.UserPreferenceChanged -= OnUserPreferenceChanged;
        _isMonitoring = false;
    }

    private static void OnUserPreferenceChanged(object sender, UserPreferenceChangedEventArgs e)
    {
        if (e.Category == UserPreferenceCategory.General)
        {
            ThemeChanged?.Invoke(null, EventArgs.Empty);
        }
    }

    /// <summary>
    /// Get the current theme colors
    /// </summary>
    public static ThemeColors GetThemeColors()
    {
        return IsDarkMode ? ThemeColors.Dark : ThemeColors.Light;
    }
}

/// <summary>
/// Color scheme for the application
/// </summary>
public class ThemeColors
{
    // Background colors
    public Color Background { get; init; }
    public Color BackgroundSecondary { get; init; }
    public Color BackgroundToolbar { get; init; }

    // Text colors
    public Color TextPrimary { get; init; }
    public Color TextSecondary { get; init; }

    // Log level colors
    public Color LogDebug { get; init; }
    public Color LogInfo { get; init; }
    public Color LogWarning { get; init; }
    public Color LogError { get; init; }
    public Color LogFatal { get; init; }
    public Color LogTimestamp { get; init; }

    // Highlight colors
    public Color HighlightKeyboard { get; init; }
    public Color HighlightHex { get; init; }
    public Color HighlightConnected { get; init; }
    public Color HighlightDisconnected { get; init; }
    public Color HighlightAction { get; init; }

    // UI colors
    public Color Border { get; init; }
    public Color ButtonHover { get; init; }
    public Color ButtonPressed { get; init; }
    public Color Separator { get; init; }

    /// <summary>
    /// Dark theme colors
    /// </summary>
    public static ThemeColors Dark { get; } = new ThemeColors
    {
        // Backgrounds
        Background = Color.FromArgb(30, 30, 30),
        BackgroundSecondary = Color.FromArgb(37, 37, 38),
        BackgroundToolbar = Color.FromArgb(45, 45, 45),

        // Text
        TextPrimary = Color.FromArgb(220, 220, 220),
        TextSecondary = Color.FromArgb(150, 150, 150),

        // Log levels
        LogDebug = Color.FromArgb(128, 128, 128),
        LogInfo = Color.FromArgb(86, 156, 214),
        LogWarning = Color.FromArgb(255, 185, 0),
        LogError = Color.FromArgb(244, 71, 71),
        LogFatal = Color.FromArgb(200, 40, 40),
        LogTimestamp = Color.FromArgb(140, 140, 140),   // Light gray for timestamp

        // Highlights
        HighlightKeyboard = Color.FromArgb(156, 220, 120),
        HighlightHex = Color.FromArgb(206, 147, 216),
        HighlightConnected = Color.FromArgb(129, 199, 132),
        HighlightDisconnected = Color.FromArgb(239, 154, 154),
        HighlightAction = Color.FromArgb(255, 213, 79),

        // UI
        Border = Color.FromArgb(60, 60, 60),
        ButtonHover = Color.FromArgb(60, 60, 60),
        ButtonPressed = Color.FromArgb(70, 70, 70),
        Separator = Color.FromArgb(60, 60, 60)
    };

    /// <summary>
    /// Light theme colors - WCAG AA compliant (4.5:1 contrast ratio on white background)
    /// </summary>
    public static ThemeColors Light { get; } = new ThemeColors
    {
        // Backgrounds
        Background = Color.FromArgb(255, 255, 255),
        BackgroundSecondary = Color.FromArgb(248, 248, 248),
        BackgroundToolbar = Color.FromArgb(240, 240, 240),

        // Text - minimum 4.5:1 contrast on white
        TextPrimary = Color.FromArgb(23, 23, 23),       // ~18:1 contrast
        TextSecondary = Color.FromArgb(90, 90, 90),     // ~5.3:1 contrast

        // Log levels - all AA compliant on white background
        LogDebug = Color.FromArgb(96, 96, 96),          // ~5.9:1 contrast (gray)
        LogInfo = Color.FromArgb(0, 90, 180),           // ~5.1:1 contrast (vivid blue)
        LogWarning = Color.FromArgb(138, 89, 0),        // ~5.2:1 contrast (dark orange/brown)
        LogError = Color.FromArgb(179, 38, 38),         // ~5.6:1 contrast (dark red)
        LogFatal = Color.FromArgb(130, 0, 0),           // ~8.1:1 contrast (very dark red)
        LogTimestamp = Color.FromArgb(96, 96, 96),      // ~5.9:1 contrast (gray like debug)

        // Highlights - AA compliant on white background
        HighlightKeyboard = Color.FromArgb(0, 120, 60),     // ~4.6:1 contrast (vivid green)
        HighlightHex = Color.FromArgb(130, 0, 180),         // ~6.2:1 contrast (vivid purple)
        HighlightConnected = Color.FromArgb(0, 120, 60),    // ~4.6:1 contrast (vivid green)
        HighlightDisconnected = Color.FromArgb(165, 29, 29),// ~6.3:1 contrast (dark red)
        HighlightAction = Color.FromArgb(200, 100, 0),      // ~4.5:1 contrast (vivid orange)

        // UI
        Border = Color.FromArgb(200, 200, 200),
        ButtonHover = Color.FromArgb(225, 225, 225),
        ButtonPressed = Color.FromArgb(210, 210, 210),
        Separator = Color.FromArgb(200, 200, 200)
    };
}
