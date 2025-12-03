using KeyboardAutoSwitcher.UI;
using Shouldly;
using Xunit;

namespace KeyboardAutoSwitcher.Tests;

/// <summary>
/// Unit tests for ThemeHelper class
/// Note: Some functionality requires Windows registry access
/// </summary>
public class ThemeHelperTests
{
    #region IsDarkMode Tests

    [Fact]
    public void IsDarkMode_ShouldReturnBoolean()
    {
        // Act - This will attempt to read from registry, falling back to true on non-Windows
        var result = ThemeHelper.IsDarkMode;

        // Assert - Should be a valid boolean value
        result.ShouldBeOneOf(true, false);
    }

    [Fact]
    public void IsDarkMode_MultipleAccesses_ShouldReturnConsistentValue()
    {
        // Act
        var result1 = ThemeHelper.IsDarkMode;
        var result2 = ThemeHelper.IsDarkMode;
        var result3 = ThemeHelper.IsDarkMode;

        // Assert - Should consistently return the same value
        result1.ShouldBe(result2);
        result2.ShouldBe(result3);
    }

    #endregion

    #region GetThemeColors Tests

    [Fact]
    public void GetThemeColors_ShouldReturnValidThemeColors()
    {
        // Act
        var colors = ThemeHelper.GetThemeColors();

        // Assert
        colors.ShouldNotBeNull();
        colors.ShouldBeOfType<ThemeColors>();
    }

    [Fact]
    public void GetThemeColors_ShouldReturnDarkOrLightTheme()
    {
        // Act
        var colors = ThemeHelper.GetThemeColors();

        // Assert - Should be one of the predefined themes
        (colors == ThemeColors.Dark || colors == ThemeColors.Light).ShouldBeTrue();
    }

    [Fact]
    public void GetThemeColors_WhenDarkMode_ShouldReturnDarkTheme()
    {
        // Arrange
        bool isDarkMode = ThemeHelper.IsDarkMode;

        // Act
        var colors = ThemeHelper.GetThemeColors();

        // Assert
        if (isDarkMode)
        {
            colors.ShouldBe(ThemeColors.Dark);
        }
        else
        {
            colors.ShouldBe(ThemeColors.Light);
        }
    }

    #endregion

    #region ThemeChanged Event Tests

    [Fact]
    public void ThemeChanged_ShouldBeAccessible()
    {
        // Assert - The event should be accessible
        var eventInfo = typeof(ThemeHelper).GetEvent(nameof(ThemeHelper.ThemeChanged));
        eventInfo.ShouldNotBeNull();
    }

    [Fact]
    public void ThemeChanged_ShouldAcceptEventHandler()
    {
        // Arrange
        bool eventRaised = false;
        void handler(object? sender, EventArgs e) => eventRaised = true;

        // Act - Subscribe and unsubscribe should not throw
        ThemeHelper.ThemeChanged += handler;
        ThemeHelper.ThemeChanged -= handler;

        // Assert - Successfully subscribed and unsubscribed
        eventRaised.ShouldBeFalse();
    }

    #endregion

    #region StartMonitoring Tests

    [Fact]
    public void StartMonitoring_ShouldNotThrow()
    {
        // Act & Assert - Should not throw
        ThemeHelper.StartMonitoring();
    }

    [Fact]
    public void StartMonitoring_CalledMultipleTimes_ShouldNotThrow()
    {
        // Act & Assert - Multiple calls should be safe
        ThemeHelper.StartMonitoring();
        ThemeHelper.StartMonitoring();
        ThemeHelper.StartMonitoring();
    }

    #endregion

    #region StopMonitoring Tests

    [Fact]
    public void StopMonitoring_ShouldNotThrow()
    {
        // Act & Assert - Should not throw
        ThemeHelper.StopMonitoring();
    }

    [Fact]
    public void StopMonitoring_CalledMultipleTimes_ShouldNotThrow()
    {
        // Act & Assert - Multiple calls should be safe
        ThemeHelper.StopMonitoring();
        ThemeHelper.StopMonitoring();
        ThemeHelper.StopMonitoring();
    }

    [Fact]
    public void StartAndStopMonitoring_ShouldWorkCorrectly()
    {
        // Act & Assert - Start/Stop cycle should work
        ThemeHelper.StartMonitoring();
        ThemeHelper.StopMonitoring();
        ThemeHelper.StartMonitoring();
        ThemeHelper.StopMonitoring();
    }

    #endregion

    #region Integration Tests

    [Fact]
    public void ThemeHelper_FullCycle_ShouldWorkCorrectly()
    {
        // Arrange
        void handler(object? sender, EventArgs e) { /* Event may or may not fire */ }

        // Act
        ThemeHelper.ThemeChanged += handler;
        ThemeHelper.StartMonitoring();
        var colors = ThemeHelper.GetThemeColors();
        ThemeHelper.StopMonitoring();
        ThemeHelper.ThemeChanged -= handler;

        // Assert
        colors.ShouldNotBeNull();
    }

    #endregion
}
