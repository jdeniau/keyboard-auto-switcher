using System.Drawing;
using KeyboardAutoSwitcher.Resources;
using Shouldly;
using Xunit;

namespace KeyboardAutoSwitcher.Tests;

/// <summary>
/// Unit tests for IconGenerator class
/// Note: These tests verify the color and text parameters used,
/// actual icon rendering requires Windows Forms runtime
/// </summary>
public class IconGeneratorTests
{
    #region CreateLayoutIcon Tests

    [Fact]
    public void CreateLayoutIcon_WithValidParameters_ShouldNotThrow()
    {
        // Arrange
        var backgroundColor = Color.Green;
        var textColor = Color.White;
        var text = "DV";

        // Act
        using var icon = IconGenerator.CreateLayoutIcon(text, backgroundColor, textColor);

        // Assert
        icon.ShouldNotBeNull();
    }

    [Fact]
    public void CreateLayoutIcon_ShouldReturnIcon()
    {
        // Act
        using var icon = IconGenerator.CreateLayoutIcon("TE", Color.Blue, Color.White);

        // Assert
        icon.ShouldNotBeNull();
        icon.ShouldBeOfType<Icon>();
    }

    [Theory]
    [InlineData("DV")]
    [InlineData("AZ")]
    [InlineData("KB")]
    [InlineData("X")]
    [InlineData("")]
    public void CreateLayoutIcon_WithVariousText_ShouldNotThrow(string text)
    {
        // Act & Assert - Should not throw for various text inputs
        using var icon = IconGenerator.CreateLayoutIcon(text, Color.Gray, Color.White);
        icon.ShouldNotBeNull();
    }

    #endregion

    #region CreateDvorakIcon Tests

    [Fact]
    public void CreateDvorakIcon_ShouldReturnValidIcon()
    {
        // Act
        using var icon = IconGenerator.CreateDvorakIcon();

        // Assert
        icon.ShouldNotBeNull();
        icon.ShouldBeOfType<Icon>();
    }

    [Fact]
    public void CreateDvorakIcon_MultipleCalls_ShouldCreateNewInstances()
    {
        // Act
        using var icon1 = IconGenerator.CreateDvorakIcon();
        using var icon2 = IconGenerator.CreateDvorakIcon();

        // Assert - Each call should create a new icon instance
        icon1.ShouldNotBeNull();
        icon2.ShouldNotBeNull();
        ReferenceEquals(icon1, icon2).ShouldBeFalse();
    }

    #endregion

    #region CreateAzertyIcon Tests

    [Fact]
    public void CreateAzertyIcon_ShouldReturnValidIcon()
    {
        // Act
        using var icon = IconGenerator.CreateAzertyIcon();

        // Assert
        icon.ShouldNotBeNull();
        icon.ShouldBeOfType<Icon>();
    }

    [Fact]
    public void CreateAzertyIcon_MultipleCalls_ShouldCreateNewInstances()
    {
        // Act
        using var icon1 = IconGenerator.CreateAzertyIcon();
        using var icon2 = IconGenerator.CreateAzertyIcon();

        // Assert
        icon1.ShouldNotBeNull();
        icon2.ShouldNotBeNull();
        ReferenceEquals(icon1, icon2).ShouldBeFalse();
    }

    #endregion

    #region CreateDefaultIcon Tests

    [Fact]
    public void CreateDefaultIcon_ShouldReturnValidIcon()
    {
        // Act
        using var icon = IconGenerator.CreateDefaultIcon();

        // Assert
        icon.ShouldNotBeNull();
        icon.ShouldBeOfType<Icon>();
    }

    [Fact]
    public void CreateDefaultIcon_MultipleCalls_ShouldCreateNewInstances()
    {
        // Act
        using var icon1 = IconGenerator.CreateDefaultIcon();
        using var icon2 = IconGenerator.CreateDefaultIcon();

        // Assert
        icon1.ShouldNotBeNull();
        icon2.ShouldNotBeNull();
        ReferenceEquals(icon1, icon2).ShouldBeFalse();
    }

    #endregion

    #region Icon Property Tests

    [Fact]
    public void AllIcons_ShouldHaveValidSize()
    {
        // Act
        using var dvorak = IconGenerator.CreateDvorakIcon();
        using var azerty = IconGenerator.CreateAzertyIcon();
        using var defaultIcon = IconGenerator.CreateDefaultIcon();

        // Assert - Icons should have valid dimensions
        dvorak.Width.ShouldBeGreaterThan(0);
        dvorak.Height.ShouldBeGreaterThan(0);
        
        azerty.Width.ShouldBeGreaterThan(0);
        azerty.Height.ShouldBeGreaterThan(0);
        
        defaultIcon.Width.ShouldBeGreaterThan(0);
        defaultIcon.Height.ShouldBeGreaterThan(0);
    }

    #endregion

    #region Color Validation Tests

    [Fact]
    public void DvorakIcon_ShouldUseGreenBackground()
    {
        // Arrange
        var expectedColor = Color.FromArgb(76, 175, 80); // Material Design Green

        // Act
        using var icon = IconGenerator.CreateDvorakIcon();
        using var bitmap = icon.ToBitmap();
        
        // Check a pixel that should be background (top-left area, inside rounded rect but outside text)
        // Rect starts at 1,1 with radius 6. 
        // Pixel at 16, 2 should be background (top center, above text)
        var pixel = bitmap.GetPixel(16, 2);

        // Assert
        // Allow small tolerance for color conversion/rendering
        ((int)pixel.R).ShouldBeInRange(expectedColor.R - 5, expectedColor.R + 5);
        ((int)pixel.G).ShouldBeInRange(expectedColor.G - 5, expectedColor.G + 5);
        ((int)pixel.B).ShouldBeInRange(expectedColor.B - 5, expectedColor.B + 5);
    }

    [Fact]
    public void AzertyIcon_ShouldUseBlueBackground()
    {
        // Arrange
        var expectedColor = Color.FromArgb(33, 150, 243); // Material Design Blue

        // Act
        using var icon = IconGenerator.CreateAzertyIcon();
        using var bitmap = icon.ToBitmap();
        
        // Check a pixel that should be background
        var pixel = bitmap.GetPixel(16, 2);

        // Assert
        ((int)pixel.R).ShouldBeInRange(expectedColor.R - 5, expectedColor.R + 5);
        ((int)pixel.G).ShouldBeInRange(expectedColor.G - 5, expectedColor.G + 5);
        ((int)pixel.B).ShouldBeInRange(expectedColor.B - 5, expectedColor.B + 5);
    }

    [Fact]
    public void DefaultIcon_ShouldUseGrayBackground()
    {
        // Arrange
        var expectedColor = Color.FromArgb(158, 158, 158); // Material Design Gray

        // Act
        using var icon = IconGenerator.CreateDefaultIcon();
        using var bitmap = icon.ToBitmap();
        
        // Check a pixel that should be background
        var pixel = bitmap.GetPixel(16, 2);

        // Assert
        ((int)pixel.R).ShouldBeInRange(expectedColor.R - 5, expectedColor.R + 5);
        ((int)pixel.G).ShouldBeInRange(expectedColor.G - 5, expectedColor.G + 5);
        ((int)pixel.B).ShouldBeInRange(expectedColor.B - 5, expectedColor.B + 5);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void CreateLayoutIcon_WithTransparentBackground_ShouldNotThrow()
    {
        // Act
        using var icon = IconGenerator.CreateLayoutIcon("X", Color.Transparent, Color.Black);

        // Assert
        icon.ShouldNotBeNull();
    }

    [Fact]
    public void CreateLayoutIcon_WithSameBackgroundAndTextColor_ShouldNotThrow()
    {
        // Act - Even though this would result in invisible text, it should not throw
        using var icon = IconGenerator.CreateLayoutIcon("X", Color.Red, Color.Red);

        // Assert
        icon.ShouldNotBeNull();
    }

    [Fact]
    public void CreateLayoutIcon_WithLongText_ShouldNotThrow()
    {
        // Act - Long text should still work (will be clipped)
        using var icon = IconGenerator.CreateLayoutIcon("LONGTEXT", Color.Gray, Color.White);

        // Assert
        icon.ShouldNotBeNull();
    }

    #endregion
}
