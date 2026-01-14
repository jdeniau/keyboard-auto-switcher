using KeyboardAutoSwitcher.Models;
using KeyboardAutoSwitcher.UI;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;

namespace KeyboardAutoSwitcher.Services
{
    /// <summary>
    /// Background worker that monitors keyboard connection and switches layouts automatically
    /// Uses event-based USB monitoring and power events instead of polling for better performance
    /// </summary>
    public class KeyboardSwitcherWorker : BackgroundService
    {
        private readonly ILogger<KeyboardSwitcherWorker> _logger;
        private readonly IUSBDeviceDetector _usbDetector;
        private readonly IConfigurationService _configService;
        private bool _isFirstCheck = true;

        /// <summary>
        /// Event raised when the keyboard layout changes
        /// </summary>
        public static event EventHandler<LayoutChangedEventArgs>? LayoutChanged;

        /// <summary>
        /// Event raised when the external keyboard connection status changes
        /// </summary>
        public static event EventHandler<KeyboardStatusEventArgs>? KeyboardStatusChanged;

        public KeyboardSwitcherWorker(
            ILogger<KeyboardSwitcherWorker> logger,
            IUSBDeviceDetector usbDetector,
            IConfigurationService configService)
        {
            _logger = logger;
            _usbDetector = usbDetector;
            _configService = configService;

            // Subscribe to configuration changes
            _configService.ConfigurationChanged += OnConfigurationChanged;
        }

        private void OnConfigurationChanged(object? sender, ConfigurationChangedEventArgs e)
        {
            _logger.LogInformation("Configuration changed, rechecking layout");
            CheckAndSwitchLayout();
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Keyboard Auto Switcher worker starting (event-based monitoring)");

            // Initialize layout cache
            KeyboardLayout.RefreshLayoutCache();

            // Check initial state
            CheckAndSwitchLayout();

            // Set up power event monitoring (for resume from sleep)
            SystemEvents.PowerModeChanged += OnPowerModeChanged;
            _logger.LogInformation("Power mode monitoring started");

            // Set up session switch monitoring (for lock/unlock)
            SystemEvents.SessionSwitch += OnSessionSwitch;
            _logger.LogInformation("Session monitoring started");

            // Set up USB event monitoring
            try
            {
                _usbDetector.DeviceChanged += OnUSBDeviceChanged;
                _usbDetector.StartMonitoring();
                _logger.LogInformation("USB event monitoring started");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to start USB event monitoring, falling back to polling");
                SystemEvents.PowerModeChanged -= OnPowerModeChanged;
                SystemEvents.SessionSwitch -= OnSessionSwitch;
                await FallbackPollingMode(stoppingToken);
                return;
            }

            // Wait for cancellation
            try
            {
                await Task.Delay(Timeout.Infinite, stoppingToken);
            }
            catch (TaskCanceledException)
            {
                // graceful shutdown
            }
            finally
            {
                SystemEvents.PowerModeChanged -= OnPowerModeChanged;
                SystemEvents.SessionSwitch -= OnSessionSwitch;
                _usbDetector.DeviceChanged -= OnUSBDeviceChanged;
                _usbDetector.StopMonitoring();
                _logger.LogInformation("Keyboard Auto Switcher worker stopping");
            }
        }

        private void OnPowerModeChanged(object sender, PowerModeChangedEventArgs e)
        {
            if (e.Mode == PowerModes.Resume)
            {
                _logger.LogInformation("System resumed from sleep/hibernation");
                // Small delay to let USB devices re-enumerate
                _ = Task.Delay(2000).ContinueWith(_ => CheckAndSwitchLayout());
            }
        }

        private void OnSessionSwitch(object sender, SessionSwitchEventArgs e)
        {
            switch (e.Reason)
            {
                case SessionSwitchReason.SessionUnlock:
                    _logger.LogInformation("Session unlocked");
                    // Small delay to ensure session is fully restored
                    _ = Task.Delay(500).ContinueWith(_ => CheckAndSwitchLayout());
                    break;
                case SessionSwitchReason.SessionLock:
                    _logger.LogDebug("Session locked");
                    break;
                case SessionSwitchReason.RemoteConnect:
                    _logger.LogInformation("Remote session connected");
                    _ = Task.Delay(500).ContinueWith(_ => CheckAndSwitchLayout());
                    break;
                case SessionSwitchReason.ConsoleConnect:
                    _logger.LogInformation("Console session connected");
                    _ = Task.Delay(500).ContinueWith(_ => CheckAndSwitchLayout());
                    break;
                case SessionSwitchReason.ConsoleDisconnect:
                    break;
                case SessionSwitchReason.RemoteDisconnect:
                    break;
                case SessionSwitchReason.SessionLogon:
                    break;
                case SessionSwitchReason.SessionLogoff:
                    break;
                case SessionSwitchReason.SessionRemoteControl:
                    break;
                default:
                    break;
            }
        }

        private void OnUSBDeviceChanged(object? sender, USBDeviceEventArgs e)
        {
            try
            {
                _logger.LogDebug("USB device event detected (keyboard connected: {IsConnected})", e.IsTargetKeyboardConnected);
                CheckAndSwitchLayout();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling USB event");
            }
        }

        private void CheckAndSwitchLayout()
        {
            try
            {
                AppConfiguration config = _configService.Configuration;
                int currentLayoutId = KeyboardLayout.GetCurrentLayoutId();

                // Check which configured device is connected (if any)
                UsbDeviceMapping? connectedDevice = _usbDetector.GetConnectedDevice(config.DeviceMappings);
                bool isExternalKeyboardConnected = connectedDevice != null;

                // Notify UI about keyboard status
                KeyboardStatusChanged?.Invoke(this, new KeyboardStatusEventArgs(
                    isExternalKeyboardConnected,
                    connectedDevice?.DeviceName));

                // Determine target layout
                int targetLayoutId;
                string targetLayoutDisplayName;

                if (connectedDevice != null)
                {
                    targetLayoutId = connectedDevice.LayoutId;
                    targetLayoutDisplayName = connectedDevice.LayoutDisplayName;
                    _logger.LogInformation("External keyboard detected: {DeviceName}", connectedDevice.DeviceName);
                }
                else
                {
                    targetLayoutId = config.DefaultLayoutId;
                    targetLayoutDisplayName = config.DefaultLayoutDisplayName;
                    _logger.LogInformation("No configured external keyboard detected");
                }

                string currentLayoutDisplay = KeyboardLayout.GetDisplayNameForLayoutId(currentLayoutId);
                _logger.LogInformation("Current layout: 0x{LayoutId:X8} => {Layout}", currentLayoutId, currentLayoutDisplay);

                // Check if we need to switch (compare by exact ID or language ID)
                bool needsSwitch = currentLayoutId != targetLayoutId &&
                                   (currentLayoutId & 0xFFFF) != (targetLayoutId & 0xFFFF);

                if (needsSwitch)
                {
                    _logger.LogInformation("Switching to {Layout}...", targetLayoutDisplayName);
                    KeyboardLayout.ActivateLayoutById(targetLayoutId);
                    _logger.LogInformation("Switched to: {Layout}", targetLayoutDisplayName);

                    // Notify UI about layout change
                    LayoutChanged?.Invoke(this, new LayoutChangedEventArgs(
                        targetLayoutDisplayName,
                        isExternalKeyboardConnected,
                        _isFirstCheck));
                }
                else
                {
                    _logger.LogDebug("Already using correct layout");

                    // Still notify UI on first check
                    if (_isFirstCheck)
                    {
                        LayoutChanged?.Invoke(this, new LayoutChangedEventArgs(
                            currentLayoutDisplay,
                            isExternalKeyboardConnected,
                            true));
                    }
                }

                _isFirstCheck = false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while checking/switching keyboard layout");
            }
        }

        /// <summary>
        /// Fallback to polling mode if event monitoring fails
        /// </summary>
        private async Task FallbackPollingMode(CancellationToken stoppingToken)
        {
            _logger.LogWarning("Running in polling mode (less efficient)");
            const int POLLING_INTERVAL_MS = 10000; // 10 seconds for fallback mode

            while (!stoppingToken.IsCancellationRequested)
            {
                CheckAndSwitchLayout();

                try
                {
                    await Task.Delay(POLLING_INTERVAL_MS, stoppingToken);
                }
                catch (TaskCanceledException)
                {
                    break;
                }
            }
        }
    }
}
