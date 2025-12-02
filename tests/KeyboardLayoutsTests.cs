using FluentAssertions;
using Xunit;

namespace KeyboardAutoSwitcher.Tests;

/// <summary>
/// Unit tests for KeyboardLayouts static class
/// </summary>
public class KeyboardLayoutsTests
{
    #region Predefined Layouts Tests

    [Fact]
    public void UsDvorak_ShouldHaveCorrectConfiguration()
    {
        // Assert
        KeyboardLayouts.UsDvorak.Should().NotBeNull();
        KeyboardLayouts.UsDvorak.CultureName.Should().Be("en-US");
        KeyboardLayouts.UsDvorak.DisplayName.Should().Contain("Dvorak");
        KeyboardLayouts.UsDvorak.LayoutId.Should().Be(unchecked((int)0xF0020409));
    }

    [Fact]
    public void FrenchStandard_ShouldHaveCorrectConfiguration()
    {
        // Assert
        KeyboardLayouts.FrenchStandard.Should().NotBeNull();
        KeyboardLayouts.FrenchStandard.CultureName.Should().Be("fr-FR");
        KeyboardLayouts.FrenchStandard.DisplayName.Should().Contain("French");
        KeyboardLayouts.FrenchStandard.LayoutId.Should().Be(0x040C040C);
    }

    #endregion

    #region GetByCultureName Tests

    [Fact]
    public void GetByCultureName_WithEnUS_ShouldReturnUsDvorak()
    {
        // Act
        var result = KeyboardLayouts.GetByCultureName("en-US");

        // Assert
        result.Should().NotBeNull();
        result.Should().Be(KeyboardLayouts.UsDvorak);
    }

    [Fact]
    public void GetByCultureName_WithFrFR_ShouldReturnFrenchStandard()
    {
        // Act
        var result = KeyboardLayouts.GetByCultureName("fr-FR");

        // Assert
        result.Should().NotBeNull();
        result.Should().Be(KeyboardLayouts.FrenchStandard);
    }

    [Theory]
    [InlineData("de-DE")]
    [InlineData("es-ES")]
    [InlineData("ja-JP")]
    [InlineData("unknown")]
    [InlineData("")]
    public void GetByCultureName_WithUnknownCulture_ShouldReturnNull(string cultureName)
    {
        // Act
        var result = KeyboardLayouts.GetByCultureName(cultureName);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region GetByLayoutId Tests

    [Fact]
    public void GetByLayoutId_WithDvorakLayoutId_ShouldReturnUsDvorak()
    {
        // Arrange
        int dvorakLayoutId = unchecked((int)0xF0020409);

        // Act
        var result = KeyboardLayouts.GetByLayoutId(dvorakLayoutId);

        // Assert
        result.Should().NotBeNull();
        result.Should().Be(KeyboardLayouts.UsDvorak);
    }

    [Fact]
    public void GetByLayoutId_WithFrenchLayoutId_ShouldReturnFrenchStandard()
    {
        // Arrange
        int frenchLayoutId = 0x040C040C;

        // Act
        var result = KeyboardLayouts.GetByLayoutId(frenchLayoutId);

        // Assert
        result.Should().NotBeNull();
        result.Should().Be(KeyboardLayouts.FrenchStandard);
    }

    [Theory]
    [InlineData(0x00000000)]
    [InlineData(0x04090409)]  // US QWERTY (not Dvorak)
    [InlineData(0x00070407)]  // German
    public void GetByLayoutId_WithUnknownLayoutId_ShouldReturnNull(int layoutId)
    {
        // Act
        var result = KeyboardLayouts.GetByLayoutId(layoutId);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region GetByLanguageId Tests

    [Fact]
    public void GetByLanguageId_WithEnglishUSLangId_ShouldReturnUsDvorak()
    {
        // Arrange
        int englishUSLangId = 0x0409;

        // Act
        var result = KeyboardLayouts.GetByLanguageId(englishUSLangId);

        // Assert
        result.Should().NotBeNull();
        result.Should().Be(KeyboardLayouts.UsDvorak);
    }

    [Fact]
    public void GetByLanguageId_WithFrenchLangId_ShouldReturnFrenchStandard()
    {
        // Arrange
        int frenchLangId = 0x040C;

        // Act
        var result = KeyboardLayouts.GetByLanguageId(frenchLangId);

        // Assert
        result.Should().NotBeNull();
        result.Should().Be(KeyboardLayouts.FrenchStandard);
    }

    [Theory]
    [InlineData(0x0407)]  // German
    [InlineData(0x0C0A)]  // Spanish
    [InlineData(0x0000)]
    public void GetByLanguageId_WithUnknownLangId_ShouldReturnNull(int langId)
    {
        // Act
        var result = KeyboardLayouts.GetByLanguageId(langId);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region Consistency Tests

    [Fact]
    public void AllPredefinedLayouts_ShouldHaveValidLanguageId()
    {
        // Assert - Language ID should be lower 16 bits of layout ID
        KeyboardLayouts.UsDvorak.GetLanguageId().Should().Be(0x0409);
        KeyboardLayouts.FrenchStandard.GetLanguageId().Should().Be(0x040C);
    }

    [Fact]
    public void GetByCultureName_ShouldBeConsistentWithGetByLayoutId()
    {
        // The layout returned by culture name should match the one returned by its layout ID
        var byCulture = KeyboardLayouts.GetByCultureName("en-US");
        var byLayoutId = KeyboardLayouts.GetByLayoutId(byCulture!.LayoutId);

        byCulture.Should().Be(byLayoutId);
    }

    #endregion
}
