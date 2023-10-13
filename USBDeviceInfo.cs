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

        foreach (var device in collection)
        {
            devices.Add(new USBDeviceInfo(
                (string)device.GetPropertyValue("DeviceID"),
                (string)device.GetPropertyValue("PNPDeviceID"),
                (string)device.GetPropertyValue("Description")
                ));
        }
        return devices;
    }
}