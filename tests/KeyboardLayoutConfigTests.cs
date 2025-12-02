using Shouldly;
using Xunit;

namespace KeyboardAutoSwitcher.Tests;

/// <summary>
/// Unit tests for KeyboardLayoutConfig class
/// </summary>
public class KeyboardLayoutConfigTests
{
    [Fact]
    public void Constructor_ShouldSetPropertiesCorrectly()
    {
        // Arrange
        const string cultureName = "en-US";
        const int layoutId = 0x00010409;
        const string displayName = "English (United States) - Dvorak";

        // Act
        var config = new KeyboardLayoutConfig(cultureName, layoutId, displayName);

        // Assert
        config.CultureName.ShouldBe(cultureName);
        config.LayoutId.ShouldBe(layoutId);
        config.DisplayName.ShouldBe(displayName);
    }

    [Theory]
    [InlineData("en-US")]
    [InlineData("fr-FR")]
    [InlineData("de-DE")]
    public void GetCultureInfo_ShouldReturnValidCultureInfo(string cultureName)
    {
        // Arrange
        var config = new KeyboardLayoutConfig(cultureName, 0x00000000, "Test");

        // Act
        var cultureInfo = config.GetCultureInfo();

        // Assert
        cultureInfo.ShouldNotBeNull();
        cultureInfo.Name.ShouldBe(cultureName);
    }

    [Theory]
    [InlineData("en-US", 0x0409)]  // English US language ID
    [InlineData("fr-FR", 0x040C)]  // French France language ID
    public void GetLanguageId_ShouldReturnCorrectLanguageId(string cultureName, int expectedLanguageId)
    {
        // Arrange
        var config = new KeyboardLayoutConfig(cultureName, 0x00000000, "Test");

        // Act
        var languageId = config.GetLanguageId();

        // Assert
        languageId.ShouldBe(expectedLanguageId);
    }

    [Fact]
    public void GetLanguageId_ShouldMaskHigherBits()
    {
        // Arrange - The language ID should only be the lower 16 bits
        var config = new KeyboardLayoutConfig("en-US", unchecked((int)0xF0020409), "Test");

        // Act
        var languageId = config.GetLanguageId();

        // Assert - Should only return 0x0409, not the full value
        languageId.ShouldBe(0x0409);
        languageId.ShouldBeLessThan(0x10000);  // Should be 16-bit value
    }
}
