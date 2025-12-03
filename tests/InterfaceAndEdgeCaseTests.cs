using Shouldly;
using Xunit;

namespace KeyboardAutoSwitcher.Tests;

/// <summary>
/// Unit tests for USBDeviceEventArgs
/// </summary>
public class USBDeviceEventArgsExtendedTests
{
    [Fact]
    public void Constructor_WithTrue_ShouldSetIsTargetKeyboardConnected()
    {
        // Act
        var args = new USBDeviceEventArgs(true);

        // Assert
        args.IsTargetKeyboardConnected.ShouldBeTrue();
    }

    [Fact]
    public void Constructor_WithFalse_ShouldSetIsTargetKeyboardConnected()
    {
        // Act
        var args = new USBDeviceEventArgs(false);

        // Assert
        args.IsTargetKeyboardConnected.ShouldBeFalse();
    }
}

/// <summary>
/// Edge case tests for KeyboardLayouts boundary values
/// </summary>
public class KeyboardLayoutsEdgeCaseTests
{
    [Theory]
    [InlineData(-1)]
    [InlineData(int.MaxValue)]
    [InlineData(int.MinValue)]
    public void GetByLayoutId_WithInvalidValue_ShouldReturnNull(int layoutId)
    {
        // Act
        var result = KeyboardLayouts.GetByLayoutId(layoutId);

        // Assert
        result.ShouldBeNull();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void GetByLanguageId_WithInvalidValue_ShouldReturnNull(int languageId)
    {
        // Act
        var result = KeyboardLayouts.GetByLanguageId(languageId);

        // Assert
        result.ShouldBeNull();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("EN-US")]  // Case-sensitive
    [InlineData(" en-US ")] // Whitespace
    public void GetByCultureName_WithInvalidInput_ShouldReturnNull(string? cultureName)
    {
        // Act
        var result = KeyboardLayouts.GetByCultureName(cultureName!);

        // Assert
        result.ShouldBeNull();
    }
}

/// <summary>
/// Edge case tests for KeyboardLayoutConfig
/// </summary>
public class KeyboardLayoutConfigEdgeCaseTests
{
    [Fact]
    public void Constructor_WithEmptyStrings_ShouldWork()
    {
        // Arrange & Act
        var config = new KeyboardLayoutConfig("", 0, "");

        // Assert
        config.CultureName.ShouldBe("");
        config.LayoutId.ShouldBe(0);
        config.DisplayName.ShouldBe("");
    }

    [Fact]
    public void Constructor_WithSpecialCharacters_ShouldWork()
    {
        // Arrange & Act
        var config = new KeyboardLayoutConfig("test-TEST", 123, "Special ! @ # $ %");

        // Assert
        config.DisplayName.ShouldBe("Special ! @ # $ %");
    }
}
