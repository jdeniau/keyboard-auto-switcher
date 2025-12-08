using System.Management; // need to add System.Management to your project references.
using KeyboardAutoSwitcher.Models;

namespace KeyboardAutoSwitcher
{
    /// <summary>
    /// Implementation of IUSBDeviceDetector using WMI for USB device monitoring
    /// </summary>
    public class USBDeviceDetector : IUSBDeviceDetector, IDisposable
    {
        private ManagementEventWatcher? _watcher;
        private bool _disposed;

        // The instance ID prefix for the TypeMatrix target keyboard device (legacy)
        public static string KeyboardInstanceName = "USB\\VID_1E54&PID_2030\\";

        public event EventHandler<USBDeviceEventArgs>? DeviceChanged;

        /// <summary>
        /// Check if the target keyboard is currently connected (legacy - uses hardcoded TypeMatrix)
        /// </summary>
        public bool IsTargetKeyboardConnected()
        {
            return IsDeviceConnected(KeyboardInstanceName);
        }

        /// <summary>
        /// Check which configured devices are currently connected
        /// Returns the first matching device mapping, or null if none connected
        /// </summary>
        public UsbDeviceMapping? GetConnectedDevice(IEnumerable<UsbDeviceMapping> deviceMappings)
        {
            try
            {
                HashSet<string> connectedPrefixes = GetConnectedUsbPrefixes();

                foreach (UsbDeviceMapping mapping in deviceMappings)
                {
                    if (connectedPrefixes.Contains(mapping.UsbInstancePrefix.ToUpperInvariant()))
                    {
                        return mapping;
                    }
                }

                return null;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Check if any of the configured devices is connected
        /// </summary>
        public bool IsAnyConfiguredDeviceConnected(IEnumerable<UsbDeviceMapping> deviceMappings)
        {
            return GetConnectedDevice(deviceMappings) != null;
        }

        /// <summary>
        /// Gets all connected USB device prefixes
        /// </summary>
        private HashSet<string> GetConnectedUsbPrefixes()
        {
            HashSet<string> prefixes = new(StringComparer.OrdinalIgnoreCase);

            try
            {
                Task<HashSet<string>> task = Task.Run(QueryAllConnectedDevices);
                if (task.Wait(TimeSpan.FromSeconds(5)))
                {
                    return task.Result;
                }
                // Timeout: retry once after a short delay
                Task.Delay(500).Wait();
                task = Task.Run(QueryAllConnectedDevices);

                return task.Wait(TimeSpan.FromSeconds(5)) ? task.Result : prefixes;
            }
            catch
            {
                return prefixes;
            }
        }

        /// <summary>
        /// Queries all connected USB devices and returns their prefixes
        /// </summary>
        private HashSet<string> QueryAllConnectedDevices()
        {
            HashSet<string> prefixes = new(StringComparer.OrdinalIgnoreCase);

            using ManagementObjectSearcher searcher = new(
                @"Select PNPDeviceID From Win32_USBHub");
            using ManagementObjectCollection collection = searcher.Get();

            foreach (ManagementObject device in collection.Cast<ManagementObject>())
            {
                try
                {
                    string? pnpDeviceId = device.GetPropertyValue("PNPDeviceID") as string;
                    if (!string.IsNullOrEmpty(pnpDeviceId))
                    {
                        // Extract the prefix (USB\VID_XXXX&PID_YYYY\)
                        int lastBackslash = pnpDeviceId.LastIndexOf('\\');
                        if (lastBackslash > 0)
                        {
                            string prefix = pnpDeviceId[..(lastBackslash + 1)];
                            _ = prefixes.Add(prefix.ToUpperInvariant());
                        }
                    }
                }
                finally
                {
                    device?.Dispose();
                }
            }

            return prefixes;
        }

        /// <summary>
        /// Check if a device with the specified prefix is connected (with timeout to prevent freezes)
        /// </summary>
        private bool IsDeviceConnected(string instancePrefix)
        {
            try
            {
                Task<bool> task = Task.Run(() => QueryDeviceConnection(instancePrefix));
                if (task.Wait(TimeSpan.FromSeconds(5)))
                {
                    return task.Result;
                }
                // Timeout: retry once after a short delay
                Task.Delay(500).Wait();
                task = Task.Run(() => QueryDeviceConnection(instancePrefix));

                return task.Wait(TimeSpan.FromSeconds(5)) && task.Result;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Performs the actual WMI query
        /// </summary>
        private bool QueryDeviceConnection(string instancePrefix)
        {
            using ManagementObjectSearcher searcher = new(
                @"Select PNPDeviceID From Win32_USBHub");
            using ManagementObjectCollection collection = searcher.Get();

            foreach (ManagementObject device in collection.Cast<ManagementObject>())
            {
                try
                {
                    string pnpDeviceID = (string)device.GetPropertyValue("PNPDeviceID");
                    if (pnpDeviceID?.StartsWith(instancePrefix, StringComparison.OrdinalIgnoreCase) == true)
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
        /// Start monitoring USB device events
        /// </summary>
        public void StartMonitoring()
        {
            if (_watcher != null)
            {
                return;
            }

            _watcher = new ManagementEventWatcher();
            WqlEventQuery query = new("SELECT * FROM __InstanceOperationEvent WITHIN 2 WHERE TargetInstance ISA 'Win32_USBHub'");
            _watcher.EventArrived += OnUSBEvent;
            _watcher.Query = query;
            _watcher.Start();
        }

        /// <summary>
        /// Stop monitoring USB device events
        /// </summary>
        public void StopMonitoring()
        {
            if (_watcher == null)
            {
                return;
            }

            _watcher.Stop();
            _watcher.EventArrived -= OnUSBEvent;
            _watcher.Dispose();
            _watcher = null;
        }

        private void OnUSBEvent(object sender, EventArrivedEventArgs e)
        {
            try
            {
                bool isConnected = IsTargetKeyboardConnected();
                DeviceChanged?.Invoke(this, new USBDeviceEventArgs(isConnected));
            }
            finally
            {
                e.NewEvent?.Dispose();
            }
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            StopMonitoring();
            _disposed = true;
            GC.SuppressFinalize(this);
        }
    }
}
