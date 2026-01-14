using KeyboardAutoSwitcher.Models;

namespace KeyboardAutoSwitcher.Services
{
    /// <summary>
    /// Interface for configuration management
    /// </summary>
    public interface IConfigurationService
    {
        /// <summary>
        /// Gets the current application configuration
        /// </summary>
        AppConfiguration Configuration { get; }

        /// <summary>
        /// Loads configuration from disk
        /// </summary>
        void Load();

        /// <summary>
        /// Saves the current configuration to disk
        /// </summary>
        void Save();

        /// <summary>
        /// Saves a new configuration and updates the current one
        /// </summary>
        void Save(AppConfiguration configuration);

        /// <summary>
        /// Event raised when configuration changes
        /// </summary>
        event EventHandler<ConfigurationChangedEventArgs>? ConfigurationChanged;
    }

    /// <summary>
    /// Event arguments for configuration change notifications
    /// </summary>
    public class ConfigurationChangedEventArgs(AppConfiguration newConfiguration) : EventArgs
    {
        public AppConfiguration NewConfiguration { get; } = newConfiguration;
    }
}
