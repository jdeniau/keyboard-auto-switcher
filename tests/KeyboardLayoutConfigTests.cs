using System.Globalization;
using Shouldly;

namespace KeyboardAutoSwitcher.Tests
{
    /// <summary>
    /// Unit tests for KeyboardLayoutConfig (KeyboardLayoutConfig.cs)
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
            KeyboardLayoutConfig config = new(cultureName, layoutId, displayName);

            // Assert
            config.CultureName.ShouldBe(cultureName);
            config.LayoutId.ShouldBe(layoutId);
            config.DisplayName.ShouldBe(displayName);
        }

        [Fact]
        public void Constructor_WithEmptyStrings_ShouldWork()
        {
            // Arrange & Act
            KeyboardLayoutConfig config = new("", 0, "");

            // Assert
            config.CultureName.ShouldBe("");
            config.LayoutId.ShouldBe(0);
            config.DisplayName.ShouldBe("");
        }

        [Fact]
        public void Constructor_WithSpecialCharacters_ShouldWork()
        {
            // Arrange & Act
            KeyboardLayoutConfig config = new("test-TEST", 123, "Special ! @ # $ %");

            // Assert
            config.DisplayName.ShouldBe("Special ! @ # $ %");
        }

        [Theory]
        [InlineData("en-US")]
        [InlineData("fr-FR")]
        [InlineData("de-DE")]
        public void GetCultureInfo_ShouldReturnValidCultureInfo(string cultureName)
        {
            // Arrange
            KeyboardLayoutConfig config = new(cultureName, 0x00000000, "Test");

            // Act
            CultureInfo cultureInfo = config.GetCultureInfo();

            // Assert
            _ = cultureInfo.ShouldNotBeNull();
            cultureInfo.Name.ShouldBe(cultureName);
        }

        [Theory]
        [InlineData("en-US", 0x0409)]  // English US language ID
        [InlineData("fr-FR", 0x040C)]  // French France language ID
        public void GetLanguageId_ShouldReturnCorrectLanguageId(string cultureName, int expectedLanguageId)
        {
            // Arrange
            KeyboardLayoutConfig config = new(cultureName, 0x00000000, "Test");

            // Act
            int languageId = config.GetLanguageId();

            // Assert
            languageId.ShouldBe(expectedLanguageId);
        }

        [Fact]
        public void GetLanguageId_ShouldMaskHigherBits()
        {
            // Arrange - The language ID should only be the lower 16 bits
            KeyboardLayoutConfig config = new("en-US", unchecked((int)0xF0020409), "Test");

            // Act
            int languageId = config.GetLanguageId();

            // Assert - Should only return 0x0409, not the full value
            languageId.ShouldBe(0x0409);
            languageId.ShouldBeLessThan(0x10000);  // Should be 16-bit value
        }
    }
}

