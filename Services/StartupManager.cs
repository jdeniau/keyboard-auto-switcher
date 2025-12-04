using System.Reflection;
using Microsoft.Extensions.Logging;

namespace KeyboardAutoSwitcher.Services
{
    /// <summary>
    /// Manages application startup with Windows
    /// </summary>
    /// <remarks>
    /// Creates a new StartupManager with the specified registry service
    /// </remarks>
    /// <param name="registryService">The registry service for Windows Registry operations</param>
    /// <param name="logger">Optional logger for diagnostics</param>
    public class StartupManager(IRegistryService registryService, ILogger<StartupManager>? logger = null) : IStartupManager
    {
        private const string RegistryKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private const string AppName = "KeyboardAutoSwitcher";

        private readonly IRegistryService _registryService = registryService ?? throw new ArgumentNullException(nameof(registryService));
        private readonly ILogger<StartupManager>? _logger = logger;

        /// <inheritdoc />
        public bool IsStartupEnabled
        {
            get
            {
                try
                {
                    object? value = _registryService.GetValue(RegistryKeyPath, AppName);

                    if (value is string path)
                    {
                        // Verify the path still matches current executable
                        string currentPath = GetExecutablePath();
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
                string executablePath = GetExecutablePath();
                bool result = _registryService.SetValue(RegistryKeyPath, AppName, $"\"{executablePath}\"");

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
                bool result = _registryService.DeleteValue(RegistryKeyPath, AppName);

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
            return IsStartupEnabled ? DisableStartup() : EnableStartup();
        }

        private static string GetExecutablePath()
        {
            // Get the path of the current executable
            string? processPath = Environment.ProcessPath;

            if (!string.IsNullOrEmpty(processPath))
            {
                return processPath;
            }

            // Fallback to the entry assembly location
            Assembly? assembly = Assembly.GetEntryAssembly();
            return assembly?.Location ?? throw new InvalidOperationException("Cannot determine executable path");
        }
    }
}
