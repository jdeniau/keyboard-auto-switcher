using KeyboardAutoSwitcher.Services;
using Shouldly;
using Xunit;

namespace KeyboardAutoSwitcher.Tests;

/// <summary>
/// Unit tests for StartupManager (Services/StartupManager.cs)
/// Note: Registry operations are Windows-specific
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

        try
        {
            // Act
            bool result = StartupManager.ToggleStartup();
        }
        finally
        {
            // Cleanup - restore original state
            if (StartupManager.IsStartupEnabled != initialState)
            {
                StartupManager.ToggleStartup();
            }
        }

        // Assert - Method completed, result indicates success or failure
    }

    #endregion

    #region EnableStartup Tests

    [Fact]
    public void EnableStartup_ShouldCompleteWithoutThrowing()
    {
        try
        {
            // Act
            bool result = StartupManager.EnableStartup();
        }
        finally
        {
            // Cleanup - restore to disabled state for test isolation
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
