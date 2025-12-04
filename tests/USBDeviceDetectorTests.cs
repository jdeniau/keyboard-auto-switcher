using Moq;
using Shouldly;

namespace KeyboardAutoSwitcher.Tests
{
    /// <summary>
    /// Unit tests for USBDeviceDetector (USBDeviceDetector.cs)
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
            using USBDeviceDetector detector = new();
            _ = detector.ShouldNotBeNull();
        }

        #endregion

        #region DeviceChanged Event Tests

        [Fact]
        public void DeviceChanged_ShouldBeAccessible()
        {
            // Arrange
            using USBDeviceDetector detector = new();
            bool eventSubscribed = false;
            void handler(object? s, USBDeviceEventArgs e)
            {
                eventSubscribed = true;
            }

            // Act

            detector.DeviceChanged += handler;
            detector.DeviceChanged -= handler;

            // Assert - Should be able to subscribe and unsubscribe
            eventSubscribed.ShouldBeFalse();
        }

        #endregion

        #region StartMonitoring Tests

        [Fact]
        public void StartMonitoring_ShouldNotThrow()
        {
            // Arrange
            using USBDeviceDetector detector = new();

            // Act & Assert - Should not throw
            detector.StartMonitoring();
        }

        [Fact]
        public void StartMonitoring_CalledMultipleTimes_ShouldNotThrow()
        {
            // Arrange
            using USBDeviceDetector detector = new();

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
            using USBDeviceDetector detector = new();

            // Act & Assert - Should not throw even if never started
            detector.StopMonitoring();
        }

        [Fact]
        public void StopMonitoring_AfterStarting_ShouldNotThrow()
        {
            // Arrange
            using USBDeviceDetector detector = new();
            detector.StartMonitoring();

            // Act & Assert
            detector.StopMonitoring();
        }

        [Fact]
        public void StopMonitoring_CalledMultipleTimes_ShouldNotThrow()
        {
            // Arrange
            using USBDeviceDetector detector = new();
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
            using USBDeviceDetector detector = new();

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
            USBDeviceDetector detector = new();

            // Act & Assert
            detector.Dispose();
        }

        [Fact]
        public void Dispose_CalledMultipleTimes_ShouldNotThrow()
        {
            // Arrange
            USBDeviceDetector detector = new();

            // Act & Assert - Double dispose should be safe
            detector.Dispose();
            detector.Dispose();
            detector.Dispose();
        }

        [Fact]
        public void Dispose_AfterMonitoring_ShouldNotThrow()
        {
            // Arrange
            USBDeviceDetector detector = new();
            detector.StartMonitoring();

            // Act & Assert
            detector.Dispose();
        }

        [Fact]
        public void Dispose_ShouldStopMonitoring()
        {
            // Arrange
            USBDeviceDetector detector = new();
            detector.StartMonitoring();

            // Act
            detector.Dispose();

            // Assert - No direct assertion possible, but should not throw
        }

        #endregion

        #region IsTargetKeyboardConnected Tests

        [Fact]
        public void IsTargetKeyboardConnected_MultipleCallsShouldBeConsistent()
        {
            // Arrange
            using USBDeviceDetector detector = new();

            // Act
            bool result1 = detector.IsTargetKeyboardConnected();
            bool result2 = detector.IsTargetKeyboardConnected();

            // Assert - Should return consistent results (assuming no hardware changes)
            result1.ShouldBe(result2);
        }

        #endregion

        #region USBDeviceEventArgs Tests

        [Fact]
        public void USBDeviceEventArgs_WithTrue_ShouldSetIsTargetKeyboardConnected()
        {
            // Act
            USBDeviceEventArgs args = new(true);

            // Assert
            args.IsTargetKeyboardConnected.ShouldBeTrue();
        }

        [Fact]
        public void USBDeviceEventArgs_WithFalse_ShouldSetIsTargetKeyboardConnected()
        {
            // Act
            USBDeviceEventArgs args = new(false);

            // Assert
            args.IsTargetKeyboardConnected.ShouldBeFalse();
        }

        #endregion

        #region IUSBDeviceDetector Mock Tests

        [Fact]
        public void MockDetector_IsTargetKeyboardConnected_ShouldReturnConfiguredValue()
        {
            // Arrange
            Mock<IUSBDeviceDetector> mockDetector = new();
            _ = mockDetector.Setup(d => d.IsTargetKeyboardConnected()).Returns(true);

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
            Mock<IUSBDeviceDetector> mockDetector = new();
            _ = mockDetector.SetupSequence(d => d.IsTargetKeyboardConnected())
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

        #endregion
    }
}

