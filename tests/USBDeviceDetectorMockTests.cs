using Shouldly;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace KeyboardAutoSwitcher.Tests;

/// <summary>
/// Unit tests for IUSBDeviceDetector with mocked USB events
/// Tests the connection/disconnection behavior
/// </summary>
public class USBDeviceDetectorMockTests
{
    /// <summary>
    /// Mock implementation of IUSBDeviceDetector for testing
    /// </summary>
    public class MockUSBDeviceDetector : IUSBDeviceDetector
    {
        private bool _isConnected;

        public event EventHandler<USBDeviceEventArgs>? DeviceChanged;

        public bool IsTargetKeyboardConnected() => _isConnected;

        public void StartMonitoring() { }

        public void StopMonitoring() { }

        /// <summary>
        /// Simulate connecting the keyboard
        /// </summary>
        public void SimulateConnect()
        {
            _isConnected = true;
            DeviceChanged?.Invoke(this, new USBDeviceEventArgs(true));
        }

        /// <summary>
        /// Simulate disconnecting the keyboard
        /// </summary>
        public void SimulateDisconnect()
        {
            _isConnected = false;
            DeviceChanged?.Invoke(this, new USBDeviceEventArgs(false));
        }

        /// <summary>
        /// Set initial connection state without triggering event
        /// </summary>
        public void SetInitialState(bool isConnected)
        {
            _isConnected = isConnected;
        }
    }

    [Fact]
    public void IsTargetKeyboardConnected_WhenDisconnected_ShouldReturnFalse()
    {
        // Arrange
        var detector = new MockUSBDeviceDetector();
        detector.SetInitialState(false);

        // Act
        bool result = detector.IsTargetKeyboardConnected();

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public void IsTargetKeyboardConnected_WhenConnected_ShouldReturnTrue()
    {
        // Arrange
        var detector = new MockUSBDeviceDetector();
        detector.SetInitialState(true);

        // Act
        bool result = detector.IsTargetKeyboardConnected();

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public void SimulateConnect_ShouldRaiseDeviceChangedEvent()
    {
        // Arrange
        var detector = new MockUSBDeviceDetector();
        bool eventRaised = false;
        bool? receivedConnectionState = null;

        detector.DeviceChanged += (sender, e) =>
        {
            eventRaised = true;
            receivedConnectionState = e.IsTargetKeyboardConnected;
        };

        // Act
        detector.SimulateConnect();

        // Assert
        eventRaised.ShouldBeTrue();
        receivedConnectionState.ShouldBe(true);
        detector.IsTargetKeyboardConnected().ShouldBeTrue();
    }

    [Fact]
    public void SimulateDisconnect_ShouldRaiseDeviceChangedEvent()
    {
        // Arrange
        var detector = new MockUSBDeviceDetector();
        detector.SetInitialState(true);
        bool eventRaised = false;
        bool? receivedConnectionState = null;

        detector.DeviceChanged += (sender, e) =>
        {
            eventRaised = true;
            receivedConnectionState = e.IsTargetKeyboardConnected;
        };

        // Act
        detector.SimulateDisconnect();

        // Assert
        eventRaised.ShouldBeTrue();
        receivedConnectionState.ShouldBe(false);
        detector.IsTargetKeyboardConnected().ShouldBeFalse();
    }

    [Fact]
    public void DeviceChanged_MultipleSubscribers_ShouldNotifyAll()
    {
        // Arrange
        var detector = new MockUSBDeviceDetector();
        int subscriber1Count = 0;
        int subscriber2Count = 0;

        detector.DeviceChanged += (s, e) => subscriber1Count++;
        detector.DeviceChanged += (s, e) => subscriber2Count++;

        // Act
        detector.SimulateConnect();
        detector.SimulateDisconnect();

        // Assert
        subscriber1Count.ShouldBe(2);
        subscriber2Count.ShouldBe(2);
    }

    [Fact]
    public void ConnectDisconnectSequence_ShouldTrackStateCorrectly()
    {
        // Arrange
        var detector = new MockUSBDeviceDetector();
        var states = new List<bool>();

        detector.DeviceChanged += (s, e) => states.Add(e.IsTargetKeyboardConnected);

        // Act - Simulate plug/unplug cycle
        detector.SimulateConnect();
        detector.SimulateDisconnect();
        detector.SimulateConnect();

        // Assert
        states.Count.ShouldBe(3);
        states.ShouldBe(new[] { true, false, true });
    }

    [Fact]
    public void USBDeviceEventArgs_ShouldStoreConnectionState()
    {
        // Arrange & Act
        var connectedArgs = new USBDeviceEventArgs(true);
        var disconnectedArgs = new USBDeviceEventArgs(false);

        // Assert
        connectedArgs.IsTargetKeyboardConnected.ShouldBeTrue();
        disconnectedArgs.IsTargetKeyboardConnected.ShouldBeFalse();
    }

    [Fact]
    public void MockDetector_WithMoq_ShouldWorkCorrectly()
    {
        // Arrange - Using Moq for more complex scenarios
        var mockDetector = new Mock<IUSBDeviceDetector>();
        mockDetector.Setup(d => d.IsTargetKeyboardConnected()).Returns(true);

        // Act
        bool result = mockDetector.Object.IsTargetKeyboardConnected();

        // Assert
        result.ShouldBeTrue();
        mockDetector.Verify(d => d.IsTargetKeyboardConnected(), Times.Once);
    }

    [Fact]
    public void MockDetector_SequenceOfCalls_ShouldReturnDifferentValues()
    {
        // Arrange
        var mockDetector = new Mock<IUSBDeviceDetector>();
        mockDetector.SetupSequence(d => d.IsTargetKeyboardConnected())
            .Returns(false)  // First call - disconnected
            .Returns(true)   // Second call - connected
            .Returns(true)   // Third call - still connected
            .Returns(false); // Fourth call - disconnected

        // Act & Assert
        mockDetector.Object.IsTargetKeyboardConnected().ShouldBeFalse();
        mockDetector.Object.IsTargetKeyboardConnected().ShouldBeTrue();
        mockDetector.Object.IsTargetKeyboardConnected().ShouldBeTrue();
        mockDetector.Object.IsTargetKeyboardConnected().ShouldBeFalse();
    }
}
