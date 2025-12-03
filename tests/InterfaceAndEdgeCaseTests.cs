using Shouldly;
using Xunit;

namespace KeyboardAutoSwitcher.Tests;

/// <summary>
/// Unit tests for IUSBDeviceDetector interface
/// </summary>
public class IUSBDeviceDetectorInterfaceTests
{
    [Fact]
    public void Interface_ShouldDefineIsTargetKeyboardConnected()
    {
        // Assert
        var method = typeof(IUSBDeviceDetector).GetMethod(nameof(IUSBDeviceDetector.IsTargetKeyboardConnected));
        method.ShouldNotBeNull();
        method.ReturnType.ShouldBe(typeof(bool));
    }

    [Fact]
    public void Interface_ShouldDefineStartMonitoring()
    {
        // Assert
        var method = typeof(IUSBDeviceDetector).GetMethod(nameof(IUSBDeviceDetector.StartMonitoring));
        method.ShouldNotBeNull();
        method.ReturnType.ShouldBe(typeof(void));
    }

    [Fact]
    public void Interface_ShouldDefineStopMonitoring()
    {
        // Assert
        var method = typeof(IUSBDeviceDetector).GetMethod(nameof(IUSBDeviceDetector.StopMonitoring));
        method.ShouldNotBeNull();
        method.ReturnType.ShouldBe(typeof(void));
    }

    [Fact]
    public void Interface_ShouldDefineDeviceChangedEvent()
    {
        // Assert
        var eventInfo = typeof(IUSBDeviceDetector).GetEvent(nameof(IUSBDeviceDetector.DeviceChanged));
        eventInfo.ShouldNotBeNull();
    }
}

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

    [Fact]
    public void USBDeviceEventArgs_ShouldInheritFromEventArgs()
    {
        // Act
        var args = new USBDeviceEventArgs(true);

        // Assert
        args.ShouldBeAssignableTo<EventArgs>();
    }

    [Fact]
    public void IsTargetKeyboardConnected_ShouldBeReadOnly()
    {
        // Assert - The property should only have a getter
        var property = typeof(USBDeviceEventArgs).GetProperty(nameof(USBDeviceEventArgs.IsTargetKeyboardConnected));
        property.ShouldNotBeNull();
        property.CanRead.ShouldBeTrue();
        property.CanWrite.ShouldBeFalse();
    }
}

/// <summary>
/// Additional tests for KeyboardLayouts to ensure edge cases are covered
/// </summary>
public class KeyboardLayoutsEdgeCaseTests
{
    [Fact]
    public void UsDvorak_GetLanguageId_ShouldReturn0x0409()
    {
        // Act
        var langId = KeyboardLayouts.UsDvorak.GetLanguageId();

        // Assert
        langId.ShouldBe(0x0409);
    }

    [Fact]
    public void FrenchStandard_GetLanguageId_ShouldReturn0x040C()
    {
        // Act
        var langId = KeyboardLayouts.FrenchStandard.GetLanguageId();

        // Assert
        langId.ShouldBe(0x040C);
    }

    [Fact]
    public void GetByLayoutId_WithNegativeValue_ShouldReturnNull()
    {
        // Act
        var result = KeyboardLayouts.GetByLayoutId(-1);

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public void GetByLayoutId_WithMaxValue_ShouldReturnNull()
    {
        // Act
        var result = KeyboardLayouts.GetByLayoutId(int.MaxValue);

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public void GetByLayoutId_WithMinValue_ShouldReturnNull()
    {
        // Act
        var result = KeyboardLayouts.GetByLayoutId(int.MinValue);

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public void GetByLanguageId_WithZero_ShouldReturnNull()
    {
        // Act
        var result = KeyboardLayouts.GetByLanguageId(0);

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public void GetByLanguageId_WithNegativeValue_ShouldReturnNull()
    {
        // Act
        var result = KeyboardLayouts.GetByLanguageId(-1);

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public void GetByCultureName_WithNull_ShouldReturnNull()
    {
        // Act
        var result = KeyboardLayouts.GetByCultureName(null!);

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public void GetByCultureName_WithMixedCase_ShouldReturnNull()
    {
        // Note: Culture names are case-sensitive in the current implementation
        // Act
        var result = KeyboardLayouts.GetByCultureName("EN-US");

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public void GetByCultureName_WithWhitespace_ShouldReturnNull()
    {
        // Act
        var result = KeyboardLayouts.GetByCultureName(" en-US ");

        // Assert
        result.ShouldBeNull();
    }
}

/// <summary>
/// Tests for KeyboardLayoutConfig edge cases
/// </summary>
public class KeyboardLayoutConfigEdgeCaseTests
{
    [Fact]
    public void GetLanguageId_WithLargeLayoutId_ShouldMaskCorrectly()
    {
        // Arrange
        var config = new KeyboardLayoutConfig("en-US", unchecked((int)0xFFFF0409), "Test");

        // Act
        var langId = config.GetLanguageId();

        // Assert - Should only return lower 16 bits
        langId.ShouldBe(0x0409);
    }

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
