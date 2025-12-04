using Microsoft.Extensions.Logging;

namespace KeyboardAutoSwitcher.Services;

/// <summary>
/// Manages application startup with Windows
/// </summary>
public class StartupManager : IStartupManager
{
    private const string RegistryKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string AppName = "KeyboardAutoSwitcher";

    private readonly IRegistryService _registryService;
    private readonly ILogger<StartupManager>? _logger;

    /// <summary>
    /// Creates a new StartupManager with the specified registry service
    /// </summary>
    /// <param name="registryService">The registry service for Windows Registry operations</param>
    /// <param name="logger">Optional logger for diagnostics</param>
    public StartupManager(IRegistryService registryService, ILogger<StartupManager>? logger = null)
    {
        _registryService = registryService ?? throw new ArgumentNullException(nameof(registryService));
        _logger = logger;
    }

    /// <inheritdoc />
    public bool IsStartupEnabled
    {
        get
        {
            try
            {
                var value = _registryService.GetValue(RegistryKeyPath, AppName);

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
                _logger?.LogWarning(ex, "Failed to check startup status");
                return false;
            }
        }
    }

    /// <inheritdoc />
    public bool EnableStartup()
    {
        try
        {
            var executablePath = GetExecutablePath();
            var result = _registryService.SetValue(RegistryKeyPath, AppName, $"\"{executablePath}\"");

            if (result)
            {
                _logger?.LogInformation("Startup enabled: {Path}", executablePath);
            }
            else
            {
                _logger?.LogError("Failed to open registry key for startup");
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to enable startup");
            return false;
        }
    }

    /// <inheritdoc />
    public bool DisableStartup()
    {
        try
        {
            var result = _registryService.DeleteValue(RegistryKeyPath, AppName);

            if (result)
            {
                _logger?.LogInformation("Startup disabled");
            }
            else
            {
                _logger?.LogError("Failed to open registry key for startup");
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to disable startup");
            return false;
        }
    }

    /// <inheritdoc />
    public bool ToggleStartup()
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
