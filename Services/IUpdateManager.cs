using Velopack;

namespace KeyboardAutoSwitcher.Services
{
    /// <summary>
    /// Interface for managing application updates
    /// </summary>
    public interface IUpdateManager
    {
        /// <summary>
        /// Gets the current application version
        /// </summary>
        string CurrentVersion { get; }

        /// <summary>
        /// Checks for available updates
        /// </summary>
        /// <returns>Update info if available, null otherwise</returns>
        Task<UpdateInfo?> CheckForUpdatesAsync();

        /// <summary>
        /// Downloads and applies the update, then restarts the application
        /// </summary>
        Task<bool> DownloadAndApplyUpdateAsync(UpdateInfo updateInfo, Action<int>? progressCallback = null);

        /// <summary>
        /// Checks for updates silently and returns true if an update is available
        /// </summary>
        Task<(bool Available, string? NewVersion)> CheckForUpdatesSilentAsync();
    }
}
