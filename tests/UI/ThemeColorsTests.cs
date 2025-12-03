using System.Drawing;
using KeyboardAutoSwitcher.UI;
using Shouldly;
using Xunit;

namespace KeyboardAutoSwitcher.Tests;

/// <summary>
/// Unit tests for ThemeColors class
/// </summary>
public class ThemeColorsTests
{
    #region Dark Theme Tests

    [Fact]
    public void DarkTheme_ShouldHaveValidBackgroundColors()
    {
        // Arrange
        var dark = ThemeColors.Dark;

        // Assert - Background colors should be dark (low RGB values)
        dark.Background.R.ShouldBeLessThan((byte)100);
        dark.Background.G.ShouldBeLessThan((byte)100);
        dark.Background.B.ShouldBeLessThan((byte)100);

        dark.BackgroundSecondary.R.ShouldBeLessThan((byte)100);
        dark.BackgroundToolbar.R.ShouldBeLessThan((byte)100);
    }

    [Fact]
    public void DarkTheme_ShouldHaveValidTextColors()
    {
        // Arrange
        var dark = ThemeColors.Dark;

        // Assert - Text colors should be light (high RGB values)
        dark.TextPrimary.R.ShouldBeGreaterThan((byte)150);
        dark.TextPrimary.G.ShouldBeGreaterThan((byte)150);
        dark.TextPrimary.B.ShouldBeGreaterThan((byte)150);
    }

    [Fact]
    public void DarkTheme_ShouldHaveAllLogLevelColors()
    {
        // Arrange
        var dark = ThemeColors.Dark;

        // Assert
        dark.LogDebug.ShouldNotBe(Color.Empty);
        dark.LogInfo.ShouldNotBe(Color.Empty);
        dark.LogWarning.ShouldNotBe(Color.Empty);
        dark.LogError.ShouldNotBe(Color.Empty);
        dark.LogFatal.ShouldNotBe(Color.Empty);
        dark.LogTimestamp.ShouldNotBe(Color.Empty);
    }

    [Fact]
    public void DarkTheme_ShouldHaveAllHighlightColors()
    {
        // Arrange
        var dark = ThemeColors.Dark;

        // Assert
        dark.HighlightKeyboard.ShouldNotBe(Color.Empty);
        dark.HighlightHex.ShouldNotBe(Color.Empty);
        dark.HighlightConnected.ShouldNotBe(Color.Empty);
        dark.HighlightDisconnected.ShouldNotBe(Color.Empty);
        dark.HighlightAction.ShouldNotBe(Color.Empty);
    }

    [Fact]
    public void DarkTheme_ShouldHaveAllUIColors()
    {
        // Arrange
        var dark = ThemeColors.Dark;

        // Assert
        dark.Border.ShouldNotBe(Color.Empty);
        dark.ButtonHover.ShouldNotBe(Color.Empty);
        dark.ButtonPressed.ShouldNotBe(Color.Empty);
        dark.Separator.ShouldNotBe(Color.Empty);
    }

    #endregion

    #region Light Theme Tests

    [Fact]
    public void LightTheme_ShouldHaveValidBackgroundColors()
    {
        // Arrange
        var light = ThemeColors.Light;

        // Assert - Background colors should be light (high RGB values)
        light.Background.R.ShouldBeGreaterThan((byte)200);
        light.Background.G.ShouldBeGreaterThan((byte)200);
        light.Background.B.ShouldBeGreaterThan((byte)200);
    }

    [Fact]
    public void LightTheme_ShouldHaveValidTextColors()
    {
        // Arrange
        var light = ThemeColors.Light;

        // Assert - Text colors should be dark (low RGB values)
        light.TextPrimary.R.ShouldBeLessThan((byte)100);
        light.TextPrimary.G.ShouldBeLessThan((byte)100);
        light.TextPrimary.B.ShouldBeLessThan((byte)100);
    }

    [Fact]
    public void LightTheme_ShouldHaveAllLogLevelColors()
    {
        // Arrange
        var light = ThemeColors.Light;

        // Assert
        light.LogDebug.ShouldNotBe(Color.Empty);
        light.LogInfo.ShouldNotBe(Color.Empty);
        light.LogWarning.ShouldNotBe(Color.Empty);
        light.LogError.ShouldNotBe(Color.Empty);
        light.LogFatal.ShouldNotBe(Color.Empty);
        light.LogTimestamp.ShouldNotBe(Color.Empty);
    }

    [Fact]
    public void LightTheme_ShouldHaveAllHighlightColors()
    {
        // Arrange
        var light = ThemeColors.Light;

        // Assert
        light.HighlightKeyboard.ShouldNotBe(Color.Empty);
        light.HighlightHex.ShouldNotBe(Color.Empty);
        light.HighlightConnected.ShouldNotBe(Color.Empty);
        light.HighlightDisconnected.ShouldNotBe(Color.Empty);
        light.HighlightAction.ShouldNotBe(Color.Empty);
    }

    [Fact]
    public void LightTheme_ShouldHaveAllUIColors()
    {
        // Arrange
        var light = ThemeColors.Light;

        // Assert
        light.Border.ShouldNotBe(Color.Empty);
        light.ButtonHover.ShouldNotBe(Color.Empty);
        light.ButtonPressed.ShouldNotBe(Color.Empty);
        light.Separator.ShouldNotBe(Color.Empty);
    }

    #endregion

    #region Contrast Tests

    [Fact]
    public void DarkTheme_LogError_ShouldBeDistinctFromOtherLevels()
    {
        // Arrange
        var dark = ThemeColors.Dark;

        // Assert - Error should be red-ish
        dark.LogError.R.ShouldBeGreaterThan(dark.LogError.G);
        dark.LogError.R.ShouldBeGreaterThan(dark.LogError.B);
    }

    [Fact]
    public void DarkTheme_LogWarning_ShouldBeDistinctFromOtherLevels()
    {
        // Arrange
        var dark = ThemeColors.Dark;

        // Assert - Warning should be yellow-ish (high R and G, low B)
        dark.LogWarning.R.ShouldBeGreaterThan(dark.LogWarning.B);
        dark.LogWarning.G.ShouldBeGreaterThan(dark.LogWarning.B);
    }

    [Fact]
    public void DarkTheme_HighlightConnected_ShouldBeGreenish()
    {
        // Arrange
        var dark = ThemeColors.Dark;

        // Assert - Connected should be green-ish
        dark.HighlightConnected.G.ShouldBeGreaterThan(dark.HighlightConnected.R);
    }

    [Fact]
    public void DarkTheme_HighlightDisconnected_ShouldBeReddish()
    {
        // Arrange
        var dark = ThemeColors.Dark;

        // Assert - Disconnected should be red-ish
        dark.HighlightDisconnected.R.ShouldBeGreaterThan(dark.HighlightDisconnected.B);
    }

    [Fact]
    public void LightTheme_HighlightConnected_ShouldBeGreenish()
    {
        // Arrange
        var light = ThemeColors.Light;

        // Assert - Connected should be green-ish
        light.HighlightConnected.G.ShouldBeGreaterThan(light.HighlightConnected.R);
    }

    [Fact]
    public void LightTheme_HighlightDisconnected_ShouldBeReddish()
    {
        // Arrange
        var light = ThemeColors.Light;

        // Assert - Disconnected should be red-ish
        light.HighlightDisconnected.R.ShouldBeGreaterThan(light.HighlightDisconnected.B);
    }

    #endregion

    #region Singleton Tests

    [Fact]
    public void DarkTheme_ShouldReturnSameInstance()
    {
        // Arrange & Act
        var dark1 = ThemeColors.Dark;
        var dark2 = ThemeColors.Dark;

        // Assert
        ReferenceEquals(dark1, dark2).ShouldBeTrue();
    }

    [Fact]
    public void LightTheme_ShouldReturnSameInstance()
    {
        // Arrange & Act
        var light1 = ThemeColors.Light;
        var light2 = ThemeColors.Light;

        // Assert
        ReferenceEquals(light1, light2).ShouldBeTrue();
    }

    [Fact]
    public void DarkTheme_ShouldNotBeSameAsLightTheme()
    {
        // Arrange & Act
        var dark = ThemeColors.Dark;
        var light = ThemeColors.Light;

        // Assert
        ReferenceEquals(dark, light).ShouldBeFalse();
        dark.Background.ShouldNotBe(light.Background);
    }

    #endregion

    #region Specific Color Value Tests

    [Fact]
    public void DarkTheme_Background_ShouldBeSpecificColor()
    {
        // Assert
        ThemeColors.Dark.Background.ShouldBe(Color.FromArgb(30, 30, 30));
    }

    [Fact]
    public void LightTheme_Background_ShouldBeWhite()
    {
        // Assert
        ThemeColors.Light.Background.ShouldBe(Color.FromArgb(255, 255, 255));
    }

    [Fact]
    public void DarkTheme_TextPrimary_ShouldBeLightGray()
    {
        // Assert
        ThemeColors.Dark.TextPrimary.ShouldBe(Color.FromArgb(220, 220, 220));
    }

    [Fact]
    public void LightTheme_TextPrimary_ShouldBeNearBlack()
    {
        // Assert
        ThemeColors.Light.TextPrimary.ShouldBe(Color.FromArgb(23, 23, 23));
    }

    #endregion
}
