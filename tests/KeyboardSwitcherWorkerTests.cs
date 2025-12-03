using KeyboardAutoSwitcher.Services;
using KeyboardAutoSwitcher.UI;
using Microsoft.Extensions.Logging;
using Moq;
using Shouldly;
using Xunit;

namespace KeyboardAutoSwitcher.Tests;

/// <summary>
/// Unit tests for KeyboardSwitcherWorker
/// </summary>
public class KeyboardSwitcherWorkerTests
{
    /// <summary>
    /// Mock implementation of IUSBDeviceDetector for testing
    /// </summary>
    private class TestUSBDeviceDetector : IUSBDeviceDetector
    {
        private bool _isConnected;

        public event EventHandler<USBDeviceEventArgs>? DeviceChanged;

        public bool IsTargetKeyboardConnected() => _isConnected;

        public void StartMonitoring() { }

        public void StopMonitoring() { }

        public void SimulateConnect()
        {
            _isConnected = true;
            DeviceChanged?.Invoke(this, new USBDeviceEventArgs(true));
        }

        public void SimulateDisconnect()
        {
            _isConnected = false;
            DeviceChanged?.Invoke(this, new USBDeviceEventArgs(false));
        }

        public void SetInitialState(bool isConnected)
        {
            _isConnected = isConnected;
        }
    }

    [Fact]
    public void Constructor_ShouldNotThrow()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<KeyboardSwitcherWorker>>();
        var detectorMock = new Mock<IUSBDeviceDetector>();

        // Act & Assert
        var worker = new KeyboardSwitcherWorker(loggerMock.Object, detectorMock.Object);
        worker.ShouldNotBeNull();
    }

    [Fact]
    public void LayoutChanged_StaticEvent_ShouldExist()
    {
        // Assert - The static event should be accessible
        bool hasEvent = typeof(KeyboardSwitcherWorker).GetEvent(nameof(KeyboardSwitcherWorker.LayoutChanged)) != null;
        hasEvent.ShouldBeTrue();
    }

    [Fact]
    public void KeyboardStatusChanged_StaticEvent_ShouldExist()
    {
        // Assert - The static event should be accessible
        bool hasEvent = typeof(KeyboardSwitcherWorker).GetEvent(nameof(KeyboardSwitcherWorker.KeyboardStatusChanged)) != null;
        hasEvent.ShouldBeTrue();
    }
}

/// <summary>
/// Unit tests for LayoutChangedEventArgs
/// </summary>
public class LayoutChangedEventArgsTests
{
    [Fact]
    public void Constructor_WithAllParameters_ShouldSetPropertiesCorrectly()
    {
        // Arrange & Act
        var args = new LayoutChangedEventArgs("English (United States) - Dvorak", true, false);

        // Assert
        args.LayoutName.ShouldBe("English (United States) - Dvorak");
        args.IsExternalKeyboard.ShouldBeTrue();
        args.IsInitial.ShouldBeFalse();
    }

    [Fact]
    public void Constructor_WithDefaultIsInitial_ShouldDefaultToFalse()
    {
        // Arrange & Act
        var args = new LayoutChangedEventArgs("French (France)", false);

        // Assert
        args.LayoutName.ShouldBe("French (France)");
        args.IsExternalKeyboard.ShouldBeFalse();
        args.IsInitial.ShouldBeFalse();
    }

    [Fact]
    public void Constructor_WithIsInitialTrue_ShouldSetCorrectly()
    {
        // Arrange & Act
        var args = new LayoutChangedEventArgs("English (United States) - Dvorak", true, true);

        // Assert
        args.IsInitial.ShouldBeTrue();
    }

    [Theory]
    [InlineData("Dvorak", true)]
    [InlineData("AZERTY", false)]
    [InlineData("QWERTY", true)]
    [InlineData("", false)]
    public void Constructor_VariousInputs_ShouldHandleCorrectly(string layoutName, bool isExternal)
    {
        // Arrange & Act
        var args = new LayoutChangedEventArgs(layoutName, isExternal);

        // Assert
        args.LayoutName.ShouldBe(layoutName);
        args.IsExternalKeyboard.ShouldBe(isExternal);
    }
}

/// <summary>
/// Unit tests for KeyboardStatusEventArgs
/// </summary>
public class KeyboardStatusEventArgsTests
{
    [Fact]
    public void Constructor_WhenConnected_ShouldSetIsConnectedToTrue()
    {
        // Arrange & Act
        var args = new KeyboardStatusEventArgs(true);

        // Assert
        args.IsConnected.ShouldBeTrue();
    }

    [Fact]
    public void Constructor_WhenDisconnected_ShouldSetIsConnectedToFalse()
    {
        // Arrange & Act
        var args = new KeyboardStatusEventArgs(false);

        // Assert
        args.IsConnected.ShouldBeFalse();
    }

    [Fact]
    public void EventArgs_ShouldInheritFromEventArgs()
    {
        // Arrange & Act
        var args = new KeyboardStatusEventArgs(true);

        // Assert
        args.ShouldBeAssignableTo<EventArgs>();
    }
}
