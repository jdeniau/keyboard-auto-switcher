using Microsoft.Win32;

namespace KeyboardAutoSwitcher.Services
{
    /// <summary>
    /// Interface for registry operations, allows mocking in tests
    /// </summary>
    public interface IRegistryService
    {
        /// <summary>
        /// Gets a value from the registry
        /// </summary>
        /// <param name="keyPath">The registry key path (relative to HKCU)</param>
        /// <param name="valueName">The name of the value to retrieve</param>
        /// <returns>The value, or null if not found</returns>
        object? GetValue(string keyPath, string valueName);

        /// <summary>
        /// Sets a value in the registry
        /// </summary>
        /// <param name="keyPath">The registry key path (relative to HKCU)</param>
        /// <param name="valueName">The name of the value to set</param>
        /// <param name="value">The value to set</param>
        /// <returns>True if successful, false otherwise</returns>
        bool SetValue(string keyPath, string valueName, object value);

        /// <summary>
        /// Deletes a value from the registry
        /// </summary>
        /// <param name="keyPath">The registry key path (relative to HKCU)</param>
        /// <param name="valueName">The name of the value to delete</param>
        /// <returns>True if successful, false otherwise</returns>
        bool DeleteValue(string keyPath, string valueName);
    }

    /// <summary>
    /// Default implementation of IRegistryService using Windows Registry
    /// </summary>
    public class WindowsRegistryService : IRegistryService
    {
        /// <inheritdoc />
        public object? GetValue(string keyPath, string valueName)
        {
            using RegistryKey? key = Registry.CurrentUser.OpenSubKey(keyPath, false);
            return key?.GetValue(valueName);
        }

        /// <inheritdoc />
        public bool SetValue(string keyPath, string valueName, object value)
        {
            using RegistryKey? key = Registry.CurrentUser.OpenSubKey(keyPath, true);
            if (key == null)
            {
                return false;
            }

            key.SetValue(valueName, value);
            return true;
        }

        /// <inheritdoc />
        public bool DeleteValue(string keyPath, string valueName)
        {
            using RegistryKey? key = Registry.CurrentUser.OpenSubKey(keyPath, true);
            if (key == null)
            {
                return false;
            }

            if (key.GetValue(valueName) != null)
            {
                key.DeleteValue(valueName, false);
            }

            return true;
        }
    }
}
