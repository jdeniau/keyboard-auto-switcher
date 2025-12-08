using System.Management; // need to add System.Management to your project references.

namespace KeyboardAutoSwitcher
{
    /// <summary>
    /// Implementation of IUSBDeviceDetector using WMI for USB device monitoring
    /// </summary>
    public class USBDeviceDetector : IUSBDeviceDetector, IDisposable
    {
        private ManagementEventWatcher? _watcher;
        private bool _disposed;

        // The instance ID prefix for the TypeMatrix target keyboard device
        public static string KeyboardInstanceName = "USB\\VID_1E54&PID_2030\\";

        public event EventHandler<USBDeviceEventArgs>? DeviceChanged;

        /// <summary>
        /// Check if the target keyboard is currently connected (with timeout to prevent freezes)
        /// </summary>
        public bool IsTargetKeyboardConnected()
        {
            try
            {
                Task<bool> task = Task.Run(QueryKeyboardConnection);
                if (task.Wait(TimeSpan.FromSeconds(5)))
                {
                    return task.Result;
                }
                // Timeout: retry once after a short delay
                Task.Delay(500).Wait();
                task = Task.Run(QueryKeyboardConnection);

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
        private bool QueryKeyboardConnection()
        {
            using ManagementObjectSearcher searcher = new(
                @"Select PNPDeviceID From Win32_USBHub");
            using ManagementObjectCollection collection = searcher.Get();

            foreach (ManagementObject device in collection.Cast<ManagementObject>())
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
