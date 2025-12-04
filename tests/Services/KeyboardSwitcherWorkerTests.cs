using KeyboardAutoSwitcher.Services;
using KeyboardAutoSwitcher.UI;
using Microsoft.Extensions.Logging;
using Moq;
using Shouldly;

namespace KeyboardAutoSwitcher.Tests.Services
{
    /// <summary>
    /// Unit tests for KeyboardSwitcherWorker
    /// </summary>
    [Collection("KeyboardSwitcherWorker")]
    public class KeyboardSwitcherWorkerTests
    {
        /// <summary>
        /// Mock implementation of IUSBDeviceDetector for testing
        /// </summary>
        private class TestUSBDeviceDetector : IUSBDeviceDetector
        {
            private bool _isConnected;
            public bool MonitoringStarted { get; private set; }
            public bool MonitoringStopped { get; private set; }

            public event EventHandler<USBDeviceEventArgs>? DeviceChanged;

            public bool IsTargetKeyboardConnected()
            {
                return _isConnected;
            }


            public void StartMonitoring()
            {
                MonitoringStarted = true;
            }

            public void StopMonitoring()
            {
                MonitoringStopped = true;
            }

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
            Mock<ILogger<KeyboardSwitcherWorker>> loggerMock = new();
            Mock<IUSBDeviceDetector> detectorMock = new();

            // Act & Assert
            KeyboardSwitcherWorker worker = new(loggerMock.Object, detectorMock.Object);
            _ = worker.ShouldNotBeNull();
        }

        [Fact]
        public void Constructor_WithNullLogger_ShouldStillCreate()
        {
            // Arrange
            Mock<IUSBDeviceDetector> detectorMock = new();

            // Act - Constructor allows null (no validation in constructor)
            KeyboardSwitcherWorker worker = new(null!, detectorMock.Object);

            // Assert
            _ = worker.ShouldNotBeNull();
        }

        [Fact]
        public void Constructor_WithNullDetector_ShouldStillCreate()
        {
            // Arrange
            Mock<ILogger<KeyboardSwitcherWorker>> loggerMock = new();

            // Act - Constructor allows null (no validation in constructor)
            KeyboardSwitcherWorker worker = new(loggerMock.Object, null!);

            // Assert
            _ = worker.ShouldNotBeNull();
        }

        [Fact]
        public void LayoutChanged_StaticEvent_CanSubscribeAndUnsubscribe()
        {
            // Arrange
            bool eventRaised = false;
            void handler(object? s, LayoutChangedEventArgs e)
            {
                eventRaised = true;
            }

            // Act

            KeyboardSwitcherWorker.LayoutChanged += handler;
            KeyboardSwitcherWorker.LayoutChanged -= handler;

            // Assert - No exception means success
            eventRaised.ShouldBeFalse();
        }

        [Fact]
        public void KeyboardStatusChanged_StaticEvent_CanSubscribeAndUnsubscribe()
        {
            // Arrange
            bool eventRaised = false;
            void handler(object? s, KeyboardStatusEventArgs e)
            {
                eventRaised = true;
            }

            // Act

            KeyboardSwitcherWorker.KeyboardStatusChanged += handler;
            KeyboardSwitcherWorker.KeyboardStatusChanged -= handler;

            // Assert - No exception means success
            eventRaised.ShouldBeFalse();
        }

        [Fact]
        public void TestUSBDeviceDetector_SimulateConnect_ShouldRaiseEventWithTrue()
        {
            // Arrange
            TestUSBDeviceDetector detector = new();
            bool eventRaised = false;
            bool connectionState = false;

            detector.DeviceChanged += (s, e) =>
            {
                eventRaised = true;
                connectionState = e.IsTargetKeyboardConnected;
            };

            // Act
            detector.SimulateConnect();

            // Assert
            eventRaised.ShouldBeTrue();
            connectionState.ShouldBeTrue();
            detector.IsTargetKeyboardConnected().ShouldBeTrue();
        }

        [Fact]
        public void TestUSBDeviceDetector_SimulateDisconnect_ShouldRaiseEventWithFalse()
        {
            // Arrange
            TestUSBDeviceDetector detector = new();
            detector.SetInitialState(true);
            bool eventRaised = false;
            bool connectionState = true;

            detector.DeviceChanged += (s, e) =>
            {
                eventRaised = true;
                connectionState = e.IsTargetKeyboardConnected;
            };

            // Act
            detector.SimulateDisconnect();

            // Assert
            eventRaised.ShouldBeTrue();
            connectionState.ShouldBeFalse();
            detector.IsTargetKeyboardConnected().ShouldBeFalse();
        }

        [Fact]
        public void TestUSBDeviceDetector_SetInitialState_ShouldUpdateConnectionState()
        {
            // Arrange
            TestUSBDeviceDetector detector = new();

            // Act & Assert - Initial state is false
            detector.IsTargetKeyboardConnected().ShouldBeFalse();

            // Act & Assert - Set to true
            detector.SetInitialState(true);
            detector.IsTargetKeyboardConnected().ShouldBeTrue();

            // Act & Assert - Set back to false
            detector.SetInitialState(false);
            detector.IsTargetKeyboardConnected().ShouldBeFalse();
        }

        [Fact]
        public void TestUSBDeviceDetector_StartMonitoring_ShouldSetFlag()
        {
            // Arrange
            TestUSBDeviceDetector detector = new();

            // Act
            detector.StartMonitoring();

            // Assert
            detector.MonitoringStarted.ShouldBeTrue();
        }

        [Fact]
        public void TestUSBDeviceDetector_StopMonitoring_ShouldSetFlag()
        {
            // Arrange
            TestUSBDeviceDetector detector = new();

            // Act
            detector.StopMonitoring();

            // Assert
            detector.MonitoringStopped.ShouldBeTrue();
        }

        [Fact]
        public void TestUSBDeviceDetector_MultipleConnectDisconnect_ShouldTrackCorrectly()
        {
            // Arrange
            TestUSBDeviceDetector detector = new();
            int eventCount = 0;
            bool lastState = false;

            detector.DeviceChanged += (s, e) =>
            {
                eventCount++;
                lastState = e.IsTargetKeyboardConnected;
            };

            // Act
            detector.SimulateConnect();
            detector.SimulateDisconnect();
            detector.SimulateConnect();

            // Assert
            eventCount.ShouldBe(3);
            lastState.ShouldBeTrue();
            detector.IsTargetKeyboardConnected().ShouldBeTrue();
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
            LayoutChangedEventArgs args = new("English (United States) - Dvorak", true, false);

            // Assert
            args.LayoutName.ShouldBe("English (United States) - Dvorak");
            args.IsExternalKeyboard.ShouldBeTrue();
            args.IsInitial.ShouldBeFalse();
        }

        [Fact]
        public void Constructor_WithDefaultIsInitial_ShouldDefaultToFalse()
        {
            // Arrange & Act
            LayoutChangedEventArgs args = new("French (France)", false);

            // Assert
            args.LayoutName.ShouldBe("French (France)");
            args.IsExternalKeyboard.ShouldBeFalse();
            args.IsInitial.ShouldBeFalse();
        }

        [Fact]
        public void Constructor_WithIsInitialTrue_ShouldSetCorrectly()
        {
            // Arrange & Act
            LayoutChangedEventArgs args = new("English (United States) - Dvorak", true, true);

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
            LayoutChangedEventArgs args = new(layoutName, isExternal);

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
            KeyboardStatusEventArgs args = new(true);

            // Assert
            args.IsConnected.ShouldBeTrue();
        }

        [Fact]
        public void Constructor_WhenDisconnected_ShouldSetIsConnectedToFalse()
        {
            // Arrange & Act
            KeyboardStatusEventArgs args = new(false);

            // Assert
            args.IsConnected.ShouldBeFalse();
        }
    }
}

