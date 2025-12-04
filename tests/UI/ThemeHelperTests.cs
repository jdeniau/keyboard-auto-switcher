using KeyboardAutoSwitcher.UI;
using Shouldly;

namespace KeyboardAutoSwitcher.Tests.UI
{
    /// <summary>
    /// Unit tests for ThemeHelper class
    /// Note: Some functionality requires Windows registry access
    /// </summary>
    [Collection("ThemeHelper")]
    public class ThemeHelperTests : IDisposable
    {
        public void Dispose()
        {
            // Ensure monitoring is stopped after each test to prevent side effects
            ThemeHelper.StopMonitoring();
            GC.SuppressFinalize(this);
        }

        #region IsDarkMode Tests

        [Fact]
        public void IsDarkMode_ShouldNotThrow()
        {
            // Act - This will attempt to read from registry, falling back to true on non-Windows
            _ = ThemeHelper.IsDarkMode;

            // Assert - Property access completed without exception
        }

        [Fact]
        public void IsDarkMode_MultipleAccesses_ShouldReturnConsistentValue()
        {
            // Act
            bool result1 = ThemeHelper.IsDarkMode;
            bool result2 = ThemeHelper.IsDarkMode;
            bool result3 = ThemeHelper.IsDarkMode;

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
            ThemeColors colors = ThemeHelper.GetThemeColors();

            // Assert
            _ = colors.ShouldNotBeNull();
        }

        [Fact]
        public void GetThemeColors_ShouldReturnDarkOrLightTheme()
        {
            // Act
            ThemeColors colors = ThemeHelper.GetThemeColors();

            // Assert - Should be one of the predefined themes
            (colors == ThemeColors.Dark || colors == ThemeColors.Light).ShouldBeTrue();
        }

        [Fact]
        public void GetThemeColors_WhenDarkMode_ShouldReturnDarkTheme()
        {
            // Arrange
            bool isDarkMode = ThemeHelper.IsDarkMode;

            // Act
            ThemeColors colors = ThemeHelper.GetThemeColors();

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
        public void ThemeChanged_ShouldAcceptEventHandler()
        {
            // Arrange
            bool eventRaised = false;
            void handler(object? sender, EventArgs e)
            {
                eventRaised = true;
            }

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
            static void handler(object? sender, EventArgs e) { /* Event may or may not fire */ }

            // Act
            ThemeHelper.ThemeChanged += handler;
            ThemeHelper.StartMonitoring();
            ThemeColors colors = ThemeHelper.GetThemeColors();
            ThemeHelper.StopMonitoring();
            ThemeHelper.ThemeChanged -= handler;

            // Assert
            _ = colors.ShouldNotBeNull();
        }

        #endregion
    }
}

