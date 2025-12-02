namespace KeyboardAutoSwitcher;

/// <summary>
/// Interface for USB device detection, allows mocking in tests
/// </summary>
public interface IUSBDeviceDetector
{
    /// <summary>
    /// Check if the target keyboard is currently connected
    /// </summary>
    bool IsTargetKeyboardConnected();

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
public class USBDeviceEventArgs : EventArgs
{
    public bool IsTargetKeyboardConnected { get; }

    public USBDeviceEventArgs(bool isTargetKeyboardConnected)
    {
        IsTargetKeyboardConnected = isTargetKeyboardConnected;
    }
}
