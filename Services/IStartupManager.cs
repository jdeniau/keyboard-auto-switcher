namespace KeyboardAutoSwitcher.Services
{
    /// <summary>
    /// Interface for managing application startup with Windows
    /// </summary>
    public interface IStartupManager
    {
        /// <summary>
        /// Gets whether the application starts with Windows
        /// </summary>
        bool IsStartupEnabled { get; }

        /// <summary>
        /// Enables starting the application with Windows
        /// </summary>
        /// <returns>True if successful, false otherwise</returns>
        bool EnableStartup();

        /// <summary>
        /// Disables starting the application with Windows
        /// </summary>
        /// <returns>True if successful, false otherwise</returns>
        bool DisableStartup();

        /// <summary>
        /// Toggles the startup setting
        /// </summary>
        /// <returns>True if successful, false otherwise</returns>
        bool ToggleStartup();
    }
}
