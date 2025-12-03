using System.Reflection;
using Serilog;
using Velopack;
using Velopack.Locators;
using Velopack.Sources;

namespace KeyboardAutoSwitcher.Services
{
    /// <summary>
    /// Manages application updates via Velopack and GitHub Releases
    /// </summary>
    /// <remarks>
    /// Creates a new UpdateService with a custom Velopack locator (for testing).
    /// </remarks>
    /// <param name="locator">Custom Velopack locator for testing, or null for default</param>
    public class UpdateService(IVelopackLocator? locator) : IUpdateManager
    {
        private const string GitHubRepoUrl = "https://github.com/jdeniau/keyboard-auto-switcher";

        private readonly UpdateManager _updateManager = new(
                new GithubSource(GitHubRepoUrl, null, false),
                options: null,
                locator: locator);

        /// <summary>
        /// Creates a new UpdateService with default Velopack configuration.
        /// Use this constructor for production.
        /// </summary>
        public UpdateService() : this(locator: null)
        {
        }

        /// <summary>
        /// Gets the current application version
        /// </summary>
        public string CurrentVersion
        {
            get
            {
                try
                {
                    Assembly? assembly = Assembly.GetEntryAssembly();
                    Version? version = assembly?.GetName().Version;
                    return version?.ToString(3) ?? "1.0.0";
                }
                catch
                {
                    return "1.0.0";
                }
            }
        }

        /// <summary>
        /// Checks for available updates
        /// </summary>
        /// <returns>Update info if available, null otherwise</returns>
        public async Task<UpdateInfo?> CheckForUpdatesAsync()
        {
            try
            {
                UpdateInfo? updateInfo = await _updateManager.CheckForUpdatesAsync();

                if (updateInfo != null)
                {
                    Log.Information("Update available: {Version}", updateInfo.TargetFullRelease.Version);
                }
                else
                {
                    Log.Debug("No updates available");
                }

                return updateInfo;
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Failed to check for updates");
                return null;
            }
        }

        /// <summary>
        /// Downloads and applies the update, then restarts the application
        /// </summary>
        public async Task<bool> DownloadAndApplyUpdateAsync(UpdateInfo updateInfo, Action<int>? progressCallback = null)
        {
            try
            {
                Log.Information("Downloading update {Version}...", updateInfo.TargetFullRelease.Version);

                await _updateManager.DownloadUpdatesAsync(updateInfo, progressCallback);

                Log.Information("Update downloaded, applying and restarting...");

                _updateManager.ApplyUpdatesAndRestart(updateInfo);

                return true;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to download and apply update");
                return false;
            }
        }

        /// <summary>
        /// Checks for updates silently and returns true if an update is available
        /// </summary>
        public async Task<(bool Available, string? NewVersion)> CheckForUpdatesSilentAsync()
        {
            UpdateInfo? updateInfo = await CheckForUpdatesAsync();

            if (updateInfo != null)
            {
                return (true, updateInfo.TargetFullRelease.Version.ToString());
            }

            return (false, null);
        }
    }
}
