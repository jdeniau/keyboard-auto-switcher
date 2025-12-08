using System.Text.Json.Serialization;

namespace KeyboardAutoSwitcher.Models
{
    /// <summary>
    /// Represents a USB device configuration with its associated keyboard layout
    /// </summary>
    public class UsbDeviceMapping
    {
        /// <summary>
        /// User-friendly name for this device
        /// </summary>
        public string DeviceName { get; set; } = string.Empty;

        /// <summary>
        /// USB Vendor ID (VID) in hex format, e.g., "1E54"
        /// </summary>
        public string VendorId { get; set; } = string.Empty;

        /// <summary>
        /// USB Product ID (PID) in hex format, e.g., "2030"
        /// </summary>
        public string ProductId { get; set; } = string.Empty;

        /// <summary>
        /// The keyboard layout ID to activate when this device is connected
        /// </summary>
        public int LayoutId { get; set; }

        /// <summary>
        /// Display name of the layout (for UI purposes)
        /// </summary>
        public string LayoutDisplayName { get; set; } = string.Empty;

        /// <summary>
        /// Gets the USB instance prefix for WMI queries (e.g., "USB\VID_1E54&PID_2030\")
        /// </summary>
        [JsonIgnore]
        public string UsbInstancePrefix => $"USB\\VID_{VendorId}&PID_{ProductId}\\";

        /// <summary>
        /// Creates a copy of this mapping
        /// </summary>
        public UsbDeviceMapping Clone()
        {
            return new UsbDeviceMapping
            {
                DeviceName = DeviceName,
                VendorId = VendorId,
                ProductId = ProductId,
                LayoutId = LayoutId,
                LayoutDisplayName = LayoutDisplayName
            };
        }
    }

    /// <summary>
    /// Application configuration containing default layout and device mappings
    /// </summary>
    public class AppConfiguration
    {
        /// <summary>
        /// The default keyboard layout ID to use when no configured device is connected
        /// </summary>
        public int DefaultLayoutId { get; set; }

        /// <summary>
        /// Display name of the default layout (for UI purposes)
        /// </summary>
        public string DefaultLayoutDisplayName { get; set; } = string.Empty;

        /// <summary>
        /// List of USB device to keyboard layout mappings
        /// </summary>
        public List<UsbDeviceMapping> DeviceMappings { get; set; } = [];

        /// <summary>
        /// Creates a default configuration with TypeMatrix keyboard
        /// </summary>
        public static AppConfiguration CreateDefault()
        {
            return new AppConfiguration
            {
                // No default layout - will be set by user in configuration
                DefaultLayoutId = 0,
                DefaultLayoutDisplayName = string.Empty,
                DeviceMappings = []
            };
        }

        /// <summary>
        /// Creates a deep copy of this configuration
        /// </summary>
        public AppConfiguration Clone()
        {
            return new AppConfiguration
            {
                DefaultLayoutId = DefaultLayoutId,
                DefaultLayoutDisplayName = DefaultLayoutDisplayName,
                DeviceMappings = [.. DeviceMappings.Select(m => m.Clone())]
            };
        }
    }
}
