using Shouldly;
using Xunit;

namespace KeyboardAutoSwitcher.Tests;

/// <summary>
/// Unit tests for USBDeviceDetector class
/// </summary>
public class USBDeviceDetectorTests
{
    #region KeyboardInstanceName Tests

    [Fact]
    public void KeyboardInstanceName_ShouldBeTypeMatrixVidPid()
    {
        // Assert
        USBDeviceDetector.KeyboardInstanceName.ShouldBe(@"USB\VID_1E54&PID_2030\");
    }

    [Fact]
    public void KeyboardInstanceName_ShouldContainVidAndPid()
    {
        // Assert
        USBDeviceDetector.KeyboardInstanceName.ShouldContain("VID_");
        USBDeviceDetector.KeyboardInstanceName.ShouldContain("PID_");
    }

    [Fact]
    public void KeyboardInstanceName_VID_ShouldBe1E54()
    {
        // Assert - TypeMatrix VID
        USBDeviceDetector.KeyboardInstanceName.ShouldContain("VID_1E54");
    }

    [Fact]
    public void KeyboardInstanceName_PID_ShouldBe2030()
    {
        // Assert - TypeMatrix PID
        USBDeviceDetector.KeyboardInstanceName.ShouldContain("PID_2030");
    }

    #endregion

    #region Constructor Tests

    [Fact]
    public void Constructor_ShouldNotThrow()
    {
        // Act & Assert
        using var detector = new USBDeviceDetector();
        detector.ShouldNotBeNull();
    }

    #endregion

    #region DeviceChanged Event Tests

    [Fact]
    public void DeviceChanged_ShouldBeAccessible()
    {
        // Arrange
        using var detector = new USBDeviceDetector();
        bool eventSubscribed = false;

        // Act
        detector.DeviceChanged += (s, e) => eventSubscribed = true;
        detector.DeviceChanged -= (s, e) => eventSubscribed = true;

        // Assert - Should be able to subscribe and unsubscribe
        eventSubscribed.ShouldBeFalse();
    }

    #endregion

    #region StartMonitoring Tests

    [Fact]
    public void StartMonitoring_ShouldNotThrow()
    {
        // Arrange
        using var detector = new USBDeviceDetector();

        // Act & Assert - Should not throw
        detector.StartMonitoring();
    }

    [Fact]
    public void StartMonitoring_CalledMultipleTimes_ShouldNotThrow()
    {
        // Arrange
        using var detector = new USBDeviceDetector();

        // Act & Assert - Multiple calls should be safe (idempotent)
        detector.StartMonitoring();
        detector.StartMonitoring();
        detector.StartMonitoring();
    }

    #endregion

    #region StopMonitoring Tests

    [Fact]
    public void StopMonitoring_WithoutStarting_ShouldNotThrow()
    {
        // Arrange
        using var detector = new USBDeviceDetector();

        // Act & Assert - Should not throw even if never started
        detector.StopMonitoring();
    }

    [Fact]
    public void StopMonitoring_AfterStarting_ShouldNotThrow()
    {
        // Arrange
        using var detector = new USBDeviceDetector();
        detector.StartMonitoring();

        // Act & Assert
        detector.StopMonitoring();
    }

    [Fact]
    public void StopMonitoring_CalledMultipleTimes_ShouldNotThrow()
    {
        // Arrange
        using var detector = new USBDeviceDetector();
        detector.StartMonitoring();

        // Act & Assert - Multiple calls should be safe
        detector.StopMonitoring();
        detector.StopMonitoring();
        detector.StopMonitoring();
    }

    #endregion

    #region StartStop Cycle Tests

    [Fact]
    public void StartStopCycle_ShouldWorkCorrectly()
    {
        // Arrange
        using var detector = new USBDeviceDetector();

        // Act & Assert - Multiple start/stop cycles should work
        detector.StartMonitoring();
        detector.StopMonitoring();
        detector.StartMonitoring();
        detector.StopMonitoring();
    }

    #endregion

    #region Dispose Tests

    [Fact]
    public void Dispose_ShouldNotThrow()
    {
        // Arrange
        var detector = new USBDeviceDetector();

        // Act & Assert
        detector.Dispose();
    }

    [Fact]
    public void Dispose_CalledMultipleTimes_ShouldNotThrow()
    {
        // Arrange
        var detector = new USBDeviceDetector();

        // Act & Assert - Double dispose should be safe
        detector.Dispose();
        detector.Dispose();
        detector.Dispose();
    }

    [Fact]
    public void Dispose_AfterMonitoring_ShouldNotThrow()
    {
        // Arrange
        var detector = new USBDeviceDetector();
        detector.StartMonitoring();

        // Act & Assert
        detector.Dispose();
    }

    [Fact]
    public void Dispose_ShouldStopMonitoring()
    {
        // Arrange
        var detector = new USBDeviceDetector();
        detector.StartMonitoring();

        // Act
        detector.Dispose();

        // Assert - No direct assertion possible, but should not throw
    }

    #endregion

    #region IsTargetKeyboardConnected Tests

    [Fact]
    public void IsTargetKeyboardConnected_ShouldReturnBoolean()
    {
        // Arrange
        using var detector = new USBDeviceDetector();

        // Act - Test method completes without throwing
        // The actual value depends on whether a TypeMatrix keyboard is connected
        _ = detector.IsTargetKeyboardConnected();
        
        // Assert - Method execution completed successfully
        // No assertion needed as we're testing that the method doesn't throw
    }

    [Fact]
    public void IsTargetKeyboardConnected_MultipleCallsShouldBeConsistent()
    {
        // Arrange
        using var detector = new USBDeviceDetector();

        // Act
        bool result1 = detector.IsTargetKeyboardConnected();
        bool result2 = detector.IsTargetKeyboardConnected();

        // Assert - Should return consistent results (assuming no hardware changes)
        result1.ShouldBe(result2);
    }

    #endregion
}
