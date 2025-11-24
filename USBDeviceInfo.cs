using System.Management; // need to add System.Management to your project references.

class USBDeviceInfo
{

    public static string KeyboardInstanceName = "USB\\VID_1E54&PID_2030\\";

    public USBDeviceInfo(string deviceID, string pnpDeviceID, string description)
    {
        this.DeviceID = deviceID;
        this.PnpDeviceID = pnpDeviceID;
        this.Description = description;
    }
    public string DeviceID { get; private set; }
    public string PnpDeviceID { get; private set; }
    public string Description { get; private set; }


    public static List<USBDeviceInfo> GetUSBDevices()
    {
        List<USBDeviceInfo> devices = new List<USBDeviceInfo>();

        using var searcher = new ManagementObjectSearcher(
            @"Select * From Win32_USBHub");
        using ManagementObjectCollection collection = searcher.Get();

        foreach (ManagementObject device in collection)
        {
            try
            {
                devices.Add(new USBDeviceInfo(
                    (string)device.GetPropertyValue("DeviceID"),
                    (string)device.GetPropertyValue("PNPDeviceID"),
                    (string)device.GetPropertyValue("Description")
                    ));
            }
            finally
            {
                device?.Dispose(); // Fix memory leak: dispose ManagementObject
            }
        }
        return devices;
    }

    /// <summary>
    /// Check if the target keyboard is currently connected
    /// </summary>
    public static bool IsTargetKeyboardConnected()
    {
        using var searcher = new ManagementObjectSearcher(
            @"Select PNPDeviceID From Win32_USBHub");
        using ManagementObjectCollection collection = searcher.Get();

        foreach (ManagementObject device in collection)
        {
            try
            {
                string pnpDeviceID = (string)device.GetPropertyValue("PNPDeviceID");
                if (pnpDeviceID?.StartsWith(KeyboardInstanceName) == true)
                {
                    return true;
                }
            }
            finally
            {
                device?.Dispose();
            }
        }
        return false;
    }

    /// <summary>
    /// Create a watcher for USB device connection/disconnection events
    /// </summary>
    public static ManagementEventWatcher CreateUSBWatcher(EventArrivedEventHandler handler)
    {
        var watcher = new ManagementEventWatcher();
        var query = new WqlEventQuery("SELECT * FROM __InstanceOperationEvent WITHIN 2 WHERE TargetInstance ISA 'Win32_USBHub'");
        watcher.EventArrived += handler;
        watcher.Query = query;
        return watcher;
    }
}