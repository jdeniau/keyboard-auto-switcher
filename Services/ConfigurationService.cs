using System.Text.Json;
using KeyboardAutoSwitcher.Models;
using Microsoft.Extensions.Logging;

namespace KeyboardAutoSwitcher.Services
{
    /// <summary>
    /// Service for managing application configuration persistence
    /// </summary>
    public class ConfigurationService : IConfigurationService
    {
        private readonly ILogger<ConfigurationService> _logger;
        private readonly string _configPath;
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        public event EventHandler<ConfigurationChangedEventArgs>? ConfigurationChanged;

        public AppConfiguration Configuration { get; private set; }

        public ConfigurationService(ILogger<ConfigurationService> logger)
        {
            _logger = logger;
            _configPath = GetConfigPath();
            Configuration = AppConfiguration.CreateDefault();

            // Ensure config directory exists
            string? configDir = Path.GetDirectoryName(_configPath);
            if (!string.IsNullOrEmpty(configDir))
            {
                _ = Directory.CreateDirectory(configDir);
            }

            // Load existing configuration if available
            Load();
        }

        private static string GetConfigPath()
        {
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                "KeyboardAutoSwitcher",
                "config.json"
            );
        }

        public void Load()
        {
            try
            {
                if (File.Exists(_configPath))
                {
                    string json = File.ReadAllText(_configPath);
                    AppConfiguration? loaded = JsonSerializer.Deserialize<AppConfiguration>(json, JsonOptions);

                    if (loaded != null)
                    {
                        Configuration = loaded;
                        _logger.LogInformation("Configuration loaded from {Path}", _configPath);
                        return;
                    }
                }

                _logger.LogInformation("No configuration file found, using defaults");
                Configuration = AppConfiguration.CreateDefault();
                Save(); // Create the config file with defaults
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load configuration, using defaults");
                Configuration = AppConfiguration.CreateDefault();
            }
        }

        public void Save()
        {
            try
            {
                string json = JsonSerializer.Serialize(Configuration, JsonOptions);
                File.WriteAllText(_configPath, json);
                _logger.LogInformation("Configuration saved to {Path}", _configPath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save configuration");
                throw;
            }
        }

        public void Save(AppConfiguration configuration)
        {
            Configuration = configuration;
            Save();
            ConfigurationChanged?.Invoke(this, new ConfigurationChangedEventArgs(configuration));
        }
    }
}
