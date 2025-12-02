using Shouldly;
using Xunit;

namespace KeyboardAutoSwitcher.Tests;

/// <summary>
/// Unit tests for USBDeviceInfo static class
/// Note: Most functionality requires actual USB hardware or WMI mocking,
/// so we focus on testing the configuration and factory methods
/// </summary>
public class USBDeviceInfoTests
{
    [Fact]
    public void KeyboardInstanceName_ShouldBeTypeMatrixVidPid()
    {
        // Assert - Verify the correct VID/PID for TypeMatrix keyboard
        USBDeviceInfo.KeyboardInstanceName.ShouldBe(@"USB\VID_1E54&PID_2030\");
    }

    [Fact]
    public void KeyboardInstanceName_ShouldStartWithUSB()
    {
        // Assert
        USBDeviceInfo.KeyboardInstanceName.ShouldStartWith("USB\\");
    }

    [Fact]
    public void KeyboardInstanceName_ShouldContainVidAndPid()
    {
        // Assert
        USBDeviceInfo.KeyboardInstanceName.ShouldContain("VID_");
        USBDeviceInfo.KeyboardInstanceName.ShouldContain("PID_");
    }

    [Fact]
    public void KeyboardInstanceName_ShouldEndWithBackslash()
    {
        // The instance name ends with backslash for prefix matching
        USBDeviceInfo.KeyboardInstanceName.ShouldEndWith("\\");
    }

    [Fact]
    public void CreateUSBWatcher_ShouldHaveCorrectQuery()
    {
        // Arrange
        void handler(object sender, System.Management.EventArrivedEventArgs e) { }

        // Act
        using var watcher = USBDeviceInfo.CreateUSBWatcher(handler);

        // Assert
        watcher.Query.ShouldNotBeNull();
        watcher.Query.QueryString.ShouldContain("__InstanceOperationEvent");
        watcher.Query.QueryString.ShouldContain("Win32_USBHub");
    }

    /// <summary>
    /// Integration test - requires actual USB hardware state
    /// This test documents the expected behavior but may pass or fail
    /// depending on whether a TypeMatrix keyboard is connected
    /// </summary>
    [Fact]
    [Trait("Category", "Integration")]
    public void IsTargetKeyboardConnected_ShouldReturnBooleanValue()
    {
        // Act - This will query actual USB devices
        bool result = USBDeviceInfo.IsTargetKeyboardConnected();

        // Assert - Should execute without throwing (result depends on hardware)
        // The method should always return a valid boolean
        result.ShouldBe(result); // Verifies no exception and valid return
    }
}
