using Serilog;
using Velopack;
using Velopack.Sources;

namespace KeyboardAutoSwitcher.Services;

/// <summary>
/// Manages application updates via Velopack and GitHub Releases
/// </summary>
public static class UpdateManager
{
    private const string GitHubRepoUrl = "https://github.com/jdeniau/keyboard-auto-switcher";

    private static Velopack.UpdateManager? _updateManager;

    /// <summary>
    /// Gets the current application version
    /// </summary>
    public static string CurrentVersion
    {
        get
        {
            try
            {
                var assembly = System.Reflection.Assembly.GetEntryAssembly();
                var version = assembly?.GetName().Version;
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
    public static async Task<UpdateInfo?> CheckForUpdatesAsync()
    {
        try
        {
            _updateManager ??= new Velopack.UpdateManager(new GithubSource(GitHubRepoUrl, null, false));

            var updateInfo = await _updateManager.CheckForUpdatesAsync();

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
    public static async Task<bool> DownloadAndApplyUpdateAsync(UpdateInfo updateInfo, Action<int>? progressCallback = null)
    {
        if (_updateManager == null)
        {
            Log.Error("UpdateManager not initialized");
            return false;
        }

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
    public static async Task<(bool Available, string? NewVersion)> CheckForUpdatesSilentAsync()
    {
        var updateInfo = await CheckForUpdatesAsync();

        if (updateInfo != null)
        {
            return (true, updateInfo.TargetFullRelease.Version.ToString());
        }

        return (false, null);
    }
}
