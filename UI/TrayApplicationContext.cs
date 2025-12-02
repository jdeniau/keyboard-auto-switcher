using System.Drawing;
using KeyboardAutoSwitcher.Resources;
using KeyboardAutoSwitcher.Services;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace KeyboardAutoSwitcher.UI;

/// <summary>
/// Application context that manages the system tray icon and menu
/// </summary>
public class TrayApplicationContext : ApplicationContext
{
    private readonly NotifyIcon _notifyIcon;
    private readonly IHost _host;
    private readonly CancellationTokenSource _cts;
    private readonly ToolStripMenuItem _statusMenuItem;
    private readonly ToolStripMenuItem _keyboardMenuItem;
    private Icon _dvorakIcon;
    private Icon _azertyIcon;
    private Icon _defaultIcon;
    private LogViewerForm? _logViewerForm;

    public TrayApplicationContext(IHost host)
    {
        _host = host;
        _cts = new CancellationTokenSource();

        // Generate icons
        _dvorakIcon = IconGenerator.CreateDvorakIcon();
        _azertyIcon = IconGenerator.CreateAzertyIcon();
        _defaultIcon = IconGenerator.CreateDefaultIcon();

        // Create context menu
        _statusMenuItem = new ToolStripMenuItem("Initialisation...")
        {
            Enabled = false
        };

        _keyboardMenuItem = new ToolStripMenuItem("Clavier: Vérification...")
        {
            Enabled = false
        };

        var contextMenu = new ContextMenuStrip();
        contextMenu.Items.Add(_statusMenuItem);
        contextMenu.Items.Add(_keyboardMenuItem);
        contextMenu.Items.Add(new ToolStripSeparator());
        contextMenu.Items.Add("📋 Afficher les logs", null, OnShowLogViewer);
        contextMenu.Items.Add("📁 Ouvrir le dossier de logs", null, OnOpenLogsFolder);
        contextMenu.Items.Add(new ToolStripSeparator());
        contextMenu.Items.Add("Quitter", null, OnExit);

        // Create notify icon
        _notifyIcon = new NotifyIcon
        {
            Icon = _defaultIcon,
            ContextMenuStrip = contextMenu,
            Text = "Keyboard Auto Switcher",
            Visible = true
        };

        _notifyIcon.DoubleClick += OnNotifyIconDoubleClick;

        // Subscribe to layout change events
        KeyboardSwitcherWorker.LayoutChanged += OnLayoutChanged;
        KeyboardSwitcherWorker.KeyboardStatusChanged += OnKeyboardStatusChanged;

        // Start the host
        StartHostAsync();
    }

    private async void StartHostAsync()
    {
        try
        {
            await _host.StartAsync(_cts.Token);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to start host");
            MessageBox.Show(
                $"Erreur au démarrage: {ex.Message}",
                "Keyboard Auto Switcher",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            Application.Exit();
        }
    }

    private void OnLayoutChanged(object? sender, LayoutChangedEventArgs e)
    {
        if (_notifyIcon.ContextMenuStrip?.InvokeRequired == true)
        {
            _notifyIcon.ContextMenuStrip.Invoke(() => OnLayoutChanged(sender, e));
            return;
        }

        UpdateIcon(e.LayoutName, e.IsExternalKeyboard);
        _statusMenuItem.Text = $"Layout: {e.LayoutName}";

        // Show balloon notification on change (only if not initial)
        if (!e.IsInitial)
        {
            _notifyIcon.ShowBalloonTip(
                2000,
                "Keyboard Auto Switcher",
                $"Disposition changée: {e.LayoutName}",
                ToolTipIcon.Info);
        }
    }

    private void OnKeyboardStatusChanged(object? sender, KeyboardStatusEventArgs e)
    {
        if (_notifyIcon.ContextMenuStrip?.InvokeRequired == true)
        {
            _notifyIcon.ContextMenuStrip.Invoke(() => OnKeyboardStatusChanged(sender, e));
            return;
        }

        _keyboardMenuItem.Text = e.IsConnected
            ? "Clavier: TypeMatrix connecté ✓"
            : "Clavier: TypeMatrix non détecté";
    }

    private void UpdateIcon(string layoutName, bool isExternalKeyboard)
    {
        if (layoutName.Contains("Dvorak", StringComparison.OrdinalIgnoreCase))
        {
            _notifyIcon.Icon = _dvorakIcon;
            _notifyIcon.Text = "Keyboard Auto Switcher - Dvorak";
        }
        else if (layoutName.Contains("French", StringComparison.OrdinalIgnoreCase) ||
                 layoutName.Contains("AZERTY", StringComparison.OrdinalIgnoreCase))
        {
            _notifyIcon.Icon = _azertyIcon;
            _notifyIcon.Text = "Keyboard Auto Switcher - AZERTY";
        }
        else
        {
            _notifyIcon.Icon = _defaultIcon;
            _notifyIcon.Text = $"Keyboard Auto Switcher - {layoutName}";
        }
    }

    private void OnNotifyIconDoubleClick(object? sender, EventArgs e)
    {
        OnShowLogViewer(sender, e);
    }

    private void OnShowLogViewer(object? sender, EventArgs e)
    {
        try
        {
            if (_logViewerForm == null || _logViewerForm.IsDisposed)
            {
                _logViewerForm = new LogViewerForm();
            }

            if (_logViewerForm.Visible)
            {
                _logViewerForm.BringToFront();
                _logViewerForm.Activate();
            }
            else
            {
                _logViewerForm.Show();
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to open log viewer");
            MessageBox.Show(
                $"Erreur lors de l'ouverture du visualiseur de logs: {ex.Message}",
                "Keyboard Auto Switcher",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

    private void OnOpenLogsFolder(object? sender, EventArgs e)
    {
        try
        {
            var logPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                "KeyboardAutoSwitcher",
                "logs");

            if (Directory.Exists(logPath))
            {
                System.Diagnostics.Process.Start("explorer.exe", logPath);
            }
            else
            {
                MessageBox.Show(
                    $"Le dossier de logs n'existe pas encore:\n{logPath}",
                    "Keyboard Auto Switcher",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to open logs folder");
        }
    }

    private async void OnExit(object? sender, EventArgs e)
    {
        // Unsubscribe from events
        KeyboardSwitcherWorker.LayoutChanged -= OnLayoutChanged;
        KeyboardSwitcherWorker.KeyboardStatusChanged -= OnKeyboardStatusChanged;

        // Hide icon immediately
        _notifyIcon.Visible = false;

        // Close log viewer if open
        _logViewerForm?.Close();
        _logViewerForm?.Dispose();

        try
        {
            _cts.Cancel();
            await _host.StopAsync(TimeSpan.FromSeconds(5));
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error during shutdown");
        }
        finally
        {
            _host.Dispose();
            _notifyIcon.Dispose();
            _dvorakIcon.Dispose();
            _azertyIcon.Dispose();
            _defaultIcon.Dispose();
            Application.Exit();
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _logViewerForm?.Dispose();
            _notifyIcon?.Dispose();
            _dvorakIcon?.Dispose();
            _azertyIcon?.Dispose();
            _defaultIcon?.Dispose();
        }
        base.Dispose(disposing);
    }
}

/// <summary>
/// Event arguments for layout change notifications
/// </summary>
public class LayoutChangedEventArgs : EventArgs
{
    public string LayoutName { get; }
    public bool IsExternalKeyboard { get; }
    public bool IsInitial { get; }

    public LayoutChangedEventArgs(string layoutName, bool isExternalKeyboard, bool isInitial = false)
    {
        LayoutName = layoutName;
        IsExternalKeyboard = isExternalKeyboard;
        IsInitial = isInitial;
    }
}

/// <summary>
/// Event arguments for keyboard status notifications
/// </summary>
public class KeyboardStatusEventArgs : EventArgs
{
    public bool IsConnected { get; }

    public KeyboardStatusEventArgs(bool isConnected)
    {
        IsConnected = isConnected;
    }
}
