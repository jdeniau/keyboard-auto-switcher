using KeyboardAutoSwitcher;
using KeyboardAutoSwitcher.Services;
using KeyboardAutoSwitcher.UI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

internal class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        // Configure Serilog
        var logPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "KeyboardAutoSwitcher",
            "logs",
            "log.txt"
        );
        Directory.CreateDirectory(Path.GetDirectoryName(logPath)!);

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.File(logPath,
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 7,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
            .CreateLogger();

        try
        {
            Log.Information("Starting Keyboard Auto Switcher");

            // Check if running as Windows Service
            bool isService = !Environment.UserInteractive;

            if (isService)
            {
                // Run as Windows Service (headless)
                RunAsService(args);
            }
            else
            {
                // Run as Windows Forms application with system tray
                RunAsGuiApplication(args);
            }
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Application terminated unexpectedly");
            MessageBox.Show(
                $"Erreur fatale: {ex.Message}\n\nConsultez les logs pour plus de détails.",
                "Keyboard Auto Switcher",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }

    private static void RunAsGuiApplication(string[] args)
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.SetHighDpiMode(HighDpiMode.SystemAware);

        var builder = Host.CreateApplicationBuilder(args);
        builder.Services.AddSerilog();

        // Register USB device detector
        builder.Services.AddSingleton<IUSBDeviceDetector, USBDeviceDetector>();

        builder.Services.AddHostedService<KeyboardSwitcherWorker>();

        var host = builder.Build();

        using var context = new TrayApplicationContext(host);
        Application.Run(context);
    }

    private static void RunAsService(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);

        builder.Services.AddSerilog();
        builder.Services.AddWindowsService(options =>
        {
            options.ServiceName = "Keyboard Auto Switcher";
        });

        // Register USB device detector
        builder.Services.AddSingleton<IUSBDeviceDetector, USBDeviceDetector>();

        builder.Services.AddHostedService<KeyboardSwitcherWorker>();

        var app = builder.Build();
        app.Run();
    }
}
