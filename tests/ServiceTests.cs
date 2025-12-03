using KeyboardAutoSwitcher.Services;
using Shouldly;
using Xunit;

namespace KeyboardAutoSwitcher.Tests;

/// <summary>
/// Unit tests for UpdateManager class
/// Note: Many methods require network/GitHub access, so we test
/// the testable parts without external dependencies
/// </summary>
public class UpdateManagerTests
{
    #region CurrentVersion Tests

    [Fact]
    public void CurrentVersion_ShouldReturnNonEmptyString()
    {
        // Act
        var version = UpdateManager.CurrentVersion;

        // Assert
        version.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public void CurrentVersion_ShouldReturnValidVersionFormat()
    {
        // Act
        var version = UpdateManager.CurrentVersion;

        // Assert - Should be in x.y.z or x.y.z.w format
        var parts = version.Split('.');
        parts.Length.ShouldBeGreaterThanOrEqualTo(2);
        parts.Length.ShouldBeLessThanOrEqualTo(4);
    }

    [Fact]
    public void CurrentVersion_AllPartsShouldBeNumeric()
    {
        // Act
        var version = UpdateManager.CurrentVersion;
        var parts = version.Split('.');

        // Assert - Each part should parse as integer
        foreach (var part in parts)
        {
            int.TryParse(part, out _).ShouldBeTrue($"Version part '{part}' should be numeric");
        }
    }

    [Fact]
    public void CurrentVersion_MultipleCalls_ShouldReturnSameValue()
    {
        // Act
        var version1 = UpdateManager.CurrentVersion;
        var version2 = UpdateManager.CurrentVersion;
        var version3 = UpdateManager.CurrentVersion;

        // Assert
        version1.ShouldBe(version2);
        version2.ShouldBe(version3);
    }

    #endregion

    #region CheckForUpdatesAsync Tests

    [Fact]
    public async Task CheckForUpdatesAsync_ShouldReturnNullableUpdateInfo()
    {
        // Act - This may return null if offline or on CI
        var result = await UpdateManager.CheckForUpdatesAsync();

        // Assert - Test that method completes without throwing
        // Result may be null (no updates available) or non-null (update available)
        if (result != null)
        {
            result.TargetFullRelease.ShouldNotBeNull();
        }
    }

    #endregion

    #region CheckForUpdatesSilentAsync Tests

    [Fact]
    public async Task CheckForUpdatesSilentAsync_ShouldReturnTuple()
    {
        // Act
        var (available, newVersion) = await UpdateManager.CheckForUpdatesSilentAsync();

        // Assert - Method should complete without throwing
        // and return consistent values
        if (available)
        {
            newVersion.ShouldNotBeNull();
        }
        else
        {
            newVersion.ShouldBeNull();
        }
    }

    [Fact]
    public async Task CheckForUpdatesSilentAsync_WhenNoUpdateAvailable_ShouldReturnFalse()
    {
        // Act
        var (available, newVersion) = await UpdateManager.CheckForUpdatesSilentAsync();

        // Assert - If no update, version should be null
        if (!available)
        {
            newVersion.ShouldBeNull();
        }
    }

    #endregion
}

/// <summary>
/// Unit tests for StartupManager class
/// Note: Registry operations are Windows-specific and may fail on other platforms
/// </summary>
public class StartupManagerTests
{
    #region IsStartupEnabled Tests

    [Fact]
    public void IsStartupEnabled_ShouldNotThrow()
    {
        // Act - Property access should not throw
        _ = StartupManager.IsStartupEnabled;
        
        // Assert - Method completed without exception
    }

    [Fact]
    public void IsStartupEnabled_MultipleCalls_ShouldBeConsistent()
    {
        // Act
        var result1 = StartupManager.IsStartupEnabled;
        var result2 = StartupManager.IsStartupEnabled;

        // Assert
        result1.ShouldBe(result2);
    }

    #endregion

    #region ToggleStartup Tests

    [Fact]
    public void ToggleStartup_ShouldCompleteWithoutThrowing()
    {
        // Arrange
        bool initialState = StartupManager.IsStartupEnabled;

        // Act
        bool result = StartupManager.ToggleStartup();

        // Cleanup - restore original state
        if (result)
        {
            StartupManager.ToggleStartup();
        }

        // Assert - Method completed, result indicates success or failure
    }

    #endregion

    #region EnableStartup Tests

    [Fact]
    public void EnableStartup_ShouldCompleteWithoutThrowing()
    {
        // Act
        bool result = StartupManager.EnableStartup();

        // Cleanup
        if (result)
        {
            StartupManager.DisableStartup();
        }

        // Assert - Method completed, result indicates success or failure
    }

    #endregion

    #region DisableStartup Tests

    [Fact]
    public void DisableStartup_ShouldCompleteWithoutThrowing()
    {
        // Act
        _ = StartupManager.DisableStartup();

        // Assert - Method completed without exception
    }

    [Fact]
    public void DisableStartup_WhenAlreadyDisabled_ShouldReturnTrue()
    {
        // Arrange - Ensure startup is disabled
        StartupManager.DisableStartup();

        // Act
        bool result = StartupManager.DisableStartup();

        // Assert - Should succeed even when already disabled
        result.ShouldBeTrue();
    }

    #endregion
}
