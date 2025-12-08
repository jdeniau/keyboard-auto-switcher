using KeyboardAutoSwitcher.Models;

namespace KeyboardAutoSwitcher
{
    /// <summary>
    /// Interface for USB device detection, allows mocking in tests
    /// </summary>
    public interface IUSBDeviceDetector
    {
        /// <summary>
        /// Check if the target keyboard is currently connected (legacy - uses first configured device)
        /// </summary>
        bool IsTargetKeyboardConnected();

        /// <summary>
        /// Check which configured devices are currently connected
        /// Returns the first matching device mapping, or null if none connected
        /// </summary>
        UsbDeviceMapping? GetConnectedDevice(IEnumerable<UsbDeviceMapping> deviceMappings);

        /// <summary>
        /// Check if any of the configured devices is connected
        /// </summary>
        bool IsAnyConfiguredDeviceConnected(IEnumerable<UsbDeviceMapping> deviceMappings);

        /// <summary>
        /// Event raised when a USB device is connected or disconnected
        /// </summary>
        event EventHandler<USBDeviceEventArgs>? DeviceChanged;

        /// <summary>
        /// Start monitoring USB device events
        /// </summary>
        void StartMonitoring();

        /// <summary>
        /// Stop monitoring USB device events
        /// </summary>
        void StopMonitoring();
    }

    /// <summary>
    /// Event args for USB device changes
    /// </summary>
    public class USBDeviceEventArgs(bool isTargetKeyboardConnected) : EventArgs
    {
        public bool IsTargetKeyboardConnected { get; } = isTargetKeyboardConnected;
    }
}
