using System.Drawing;
using KeyboardAutoSwitcher.Resources;
using Shouldly;

namespace KeyboardAutoSwitcher.Tests.Resources
{
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
            Color backgroundColor = Color.Green;
            Color textColor = Color.White;
            string text = "DV";

            // Act
            using Icon icon = IconGenerator.CreateLayoutIcon(text, backgroundColor, textColor);

            // Assert
            _ = icon.ShouldNotBeNull();
        }

        [Fact]
        public void CreateLayoutIcon_ShouldReturnIcon()
        {
            // Act
            using Icon icon = IconGenerator.CreateLayoutIcon("TE", Color.Blue, Color.White);

            // Assert
            _ = icon.ShouldNotBeNull();
            _ = icon.ShouldBeOfType<Icon>();
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
            using Icon icon = IconGenerator.CreateLayoutIcon(text, Color.Gray, Color.White);
            _ = icon.ShouldNotBeNull();
        }

        #endregion

        #region CreateDvorakIcon Tests

        [Fact]
        public void CreateDvorakIcon_ShouldReturnValidIcon()
        {
            // Act
            using Icon icon = IconGenerator.CreateDvorakIcon();

            // Assert
            _ = icon.ShouldNotBeNull();
            _ = icon.ShouldBeOfType<Icon>();
        }

        [Fact]
        public void CreateDvorakIcon_MultipleCalls_ShouldCreateNewInstances()
        {
            // Act
            using Icon icon1 = IconGenerator.CreateDvorakIcon();
            using Icon icon2 = IconGenerator.CreateDvorakIcon();

            // Assert - Each call should create a new icon instance
            _ = icon1.ShouldNotBeNull();
            _ = icon2.ShouldNotBeNull();
            ReferenceEquals(icon1, icon2).ShouldBeFalse();
        }

        #endregion

        #region CreateAzertyIcon Tests

        [Fact]
        public void CreateAzertyIcon_ShouldReturnValidIcon()
        {
            // Act
            using Icon icon = IconGenerator.CreateAzertyIcon();

            // Assert
            _ = icon.ShouldNotBeNull();
            _ = icon.ShouldBeOfType<Icon>();
        }

        [Fact]
        public void CreateAzertyIcon_MultipleCalls_ShouldCreateNewInstances()
        {
            // Act
            using Icon icon1 = IconGenerator.CreateAzertyIcon();
            using Icon icon2 = IconGenerator.CreateAzertyIcon();

            // Assert
            _ = icon1.ShouldNotBeNull();
            _ = icon2.ShouldNotBeNull();
            ReferenceEquals(icon1, icon2).ShouldBeFalse();
        }

        #endregion

        #region CreateDefaultIcon Tests

        [Fact]
        public void CreateDefaultIcon_ShouldReturnValidIcon()
        {
            // Act
            using Icon icon = IconGenerator.CreateDefaultIcon();

            // Assert
            _ = icon.ShouldNotBeNull();
            _ = icon.ShouldBeOfType<Icon>();
        }

        [Fact]
        public void CreateDefaultIcon_MultipleCalls_ShouldCreateNewInstances()
        {
            // Act
            using Icon icon1 = IconGenerator.CreateDefaultIcon();
            using Icon icon2 = IconGenerator.CreateDefaultIcon();

            // Assert
            _ = icon1.ShouldNotBeNull();
            _ = icon2.ShouldNotBeNull();
            ReferenceEquals(icon1, icon2).ShouldBeFalse();
        }

        #endregion

        #region Icon Property Tests

        [Fact]
        public void AllIcons_ShouldHaveValidSize()
        {
            // Act
            using Icon dvorak = IconGenerator.CreateDvorakIcon();
            using Icon azerty = IconGenerator.CreateAzertyIcon();
            using Icon defaultIcon = IconGenerator.CreateDefaultIcon();

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
            Color expectedColor = Color.FromArgb(76, 175, 80); // Material Design Green

            // Act
            using Icon icon = IconGenerator.CreateDvorakIcon();
            using Bitmap bitmap = icon.ToBitmap();

            // Check a pixel that should be background (top-left area, inside rounded rect but outside text)
            // Rect starts at 1,1 with radius 6. 
            // Pixel at 16, 2 should be background (top center, above text)
            Color pixel = bitmap.GetPixel(16, 2);

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
            Color expectedColor = Color.FromArgb(33, 150, 243); // Material Design Blue

            // Act
            using Icon icon = IconGenerator.CreateAzertyIcon();
            using Bitmap bitmap = icon.ToBitmap();

            // Check a pixel that should be background
            Color pixel = bitmap.GetPixel(16, 2);

            // Assert
            ((int)pixel.R).ShouldBeInRange(expectedColor.R - 5, expectedColor.R + 5);
            ((int)pixel.G).ShouldBeInRange(expectedColor.G - 5, expectedColor.G + 5);
            ((int)pixel.B).ShouldBeInRange(expectedColor.B - 5, expectedColor.B + 5);
        }

        [Fact]
        public void DefaultIcon_ShouldUseGrayBackground()
        {
            // Arrange
            Color expectedColor = Color.FromArgb(158, 158, 158); // Material Design Gray

            // Act
            using Icon icon = IconGenerator.CreateDefaultIcon();
            using Bitmap bitmap = icon.ToBitmap();

            // Check a pixel that should be background
            Color pixel = bitmap.GetPixel(16, 2);

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
            using Icon icon = IconGenerator.CreateLayoutIcon("X", Color.Transparent, Color.Black);

            // Assert
            _ = icon.ShouldNotBeNull();
        }

        [Fact]
        public void CreateLayoutIcon_WithSameBackgroundAndTextColor_ShouldNotThrow()
        {
            // Act - Even though this would result in invisible text, it should not throw
            using Icon icon = IconGenerator.CreateLayoutIcon("X", Color.Red, Color.Red);

            // Assert
            _ = icon.ShouldNotBeNull();
        }

        [Fact]
        public void CreateLayoutIcon_WithLongText_ShouldNotThrow()
        {
            // Act - Long text should still work (will be clipped)
            using Icon icon = IconGenerator.CreateLayoutIcon("LONGTEXT", Color.Gray, Color.White);

            // Assert
            _ = icon.ShouldNotBeNull();
        }

        #endregion
    }
}

