using Microsoft.Win32;
using Serilog;

namespace KeyboardAutoSwitcher.Services;

/// <summary>
/// Manages application startup with Windows
/// </summary>
public static class StartupManager
{
    private const string RegistryKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string AppName = "KeyboardAutoSwitcher";

    /// <summary>
    /// Gets or sets whether the application starts with Windows
    /// </summary>
    public static bool IsStartupEnabled
    {
        get
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(RegistryKeyPath, false);
                var value = key?.GetValue(AppName);

                if (value is string path)
                {
                    // Verify the path still matches current executable
                    var currentPath = GetExecutablePath();
                    return path.Equals($"\"{currentPath}\"", StringComparison.OrdinalIgnoreCase) ||
                           path.Equals(currentPath, StringComparison.OrdinalIgnoreCase);
                }

                return false;
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Failed to check startup status");
                return false;
            }
        }
    }

    /// <summary>
    /// Enables starting the application with Windows
    /// </summary>
    public static bool EnableStartup()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RegistryKeyPath, true);
            if (key == null)
            {
                Log.Error("Failed to open registry key for startup");
                return false;
            }

            var executablePath = GetExecutablePath();
            key.SetValue(AppName, $"\"{executablePath}\"");

            Log.Information("Startup enabled: {Path}", executablePath);
            return true;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to enable startup");
            return false;
        }
    }

    /// <summary>
    /// Disables starting the application with Windows
    /// </summary>
    public static bool DisableStartup()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RegistryKeyPath, true);
            if (key == null)
            {
                Log.Error("Failed to open registry key for startup");
                return false;
            }

            // Check if value exists before trying to delete
            if (key.GetValue(AppName) != null)
            {
                key.DeleteValue(AppName, false);
                Log.Information("Startup disabled");
            }

            return true;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to disable startup");
            return false;
        }
    }

    /// <summary>
    /// Toggles the startup setting
    /// </summary>
    public static bool ToggleStartup()
    {
        if (IsStartupEnabled)
        {
            return DisableStartup();
        }
        else
        {
            return EnableStartup();
        }
    }

    private static string GetExecutablePath()
    {
        // Get the path of the current executable
        var processPath = Environment.ProcessPath;

        if (!string.IsNullOrEmpty(processPath))
        {
            return processPath;
        }

        // Fallback to the entry assembly location
        var assembly = System.Reflection.Assembly.GetEntryAssembly();
        return assembly?.Location ?? throw new InvalidOperationException("Cannot determine executable path");
    }
}
