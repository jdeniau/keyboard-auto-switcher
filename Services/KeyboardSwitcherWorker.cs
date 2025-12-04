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
    public class KeyboardSwitcherWorker(ILogger<KeyboardSwitcherWorker> logger, IUSBDeviceDetector usbDetector) : BackgroundService
    {
        private readonly ILogger<KeyboardSwitcherWorker> _logger = logger;
        private readonly IUSBDeviceDetector _usbDetector = usbDetector;
        private bool _isFirstCheck = true;

        /// <summary>
        /// Event raised when the keyboard layout changes
        /// </summary>
        public static event EventHandler<LayoutChangedEventArgs>? LayoutChanged;

        /// <summary>
        /// Event raised when the external keyboard connection status changes
        /// </summary>
        public static event EventHandler<KeyboardStatusEventArgs>? KeyboardStatusChanged;

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
                KeyboardLayoutConfig? currentLayout = KeyboardLayout.GetCurrentLayout();
                int currentLayoutId = KeyboardLayout.GetCurrentLayoutId();
                bool isExternalKeyboardConnected = _usbDetector.IsTargetKeyboardConnected();

                // Notify UI about keyboard status
                KeyboardStatusChanged?.Invoke(this, new KeyboardStatusEventArgs(isExternalKeyboardConnected));

                KeyboardLayoutConfig targetLayout = isExternalKeyboardConnected
                    ? KeyboardLayouts.UsDvorak
                    : KeyboardLayouts.FrenchStandard;

                _logger.LogInformation(isExternalKeyboardConnected
                    ? "External keyboard detected"
                    : "No external keyboard");

                _logger.LogInformation("Current layout: 0x{LayoutId:X8} => {Layout}", currentLayoutId,
                    currentLayout != null ? currentLayout.DisplayName : $"Unknown (lang: {currentLayoutId & 0xFFFF:X4})");

                if (currentLayout == null || currentLayout.LayoutId != targetLayout.LayoutId)
                {
                    _logger.LogInformation("Switching to {Layout}...", targetLayout.DisplayName);
                    SetKeyboardLayout(targetLayout);

                    // Notify UI about layout change
                    LayoutChanged?.Invoke(this, new LayoutChangedEventArgs(
                        targetLayout.DisplayName,
                        isExternalKeyboardConnected,
                        _isFirstCheck));
                }
                else
                {
                    _logger.LogDebug("Already using {Layout}", currentLayout.DisplayName);

                    // Still notify UI on first check
                    if (_isFirstCheck)
                    {
                        LayoutChanged?.Invoke(this, new LayoutChangedEventArgs(
                            currentLayout.DisplayName,
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

        private void SetKeyboardLayout(KeyboardLayoutConfig targetLayoutConfig)
        {
            _logger.LogDebug("Looking for layout: {Layout} (0x{LayoutId:X8})", targetLayoutConfig.DisplayName, targetLayoutConfig.LayoutId);

            if (_logger.IsEnabled(LogLevel.Debug))
            {
                nint[]? cachedLayouts = KeyboardLayout.GetCachedLayoutHandles();
                if (cachedLayouts != null)
                {
                    foreach (IntPtr hkl in cachedLayouts)
                    {
                        KeyboardLayoutConfig? knownLayout = KeyboardLayouts.GetByLayoutId((int)hkl);
                        string layoutName = knownLayout != null ? knownLayout.DisplayName : "Unknown";
                        _logger.LogDebug("  0x{HKL:X8} - {LayoutName}", (int)hkl, layoutName);
                    }
                }
            }

            try
            {
                _logger.LogInformation("Activating layout: 0x{LayoutId:X8}", targetLayoutConfig.LayoutId);
                KeyboardLayout.ActivateLayout(targetLayoutConfig);
                _logger.LogInformation("Switched to: {Layout}", targetLayoutConfig.DisplayName);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex.Message);
            }
        }
    }
}
