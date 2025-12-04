using KeyboardAutoSwitcher;
using KeyboardAutoSwitcher.Logging;
using KeyboardAutoSwitcher.Services;
using KeyboardAutoSwitcher.UI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Velopack;

internal class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        // Velopack must be the first thing to run
        // SetLogger redirects Velopack internal logs to our Serilog logger
        VelopackApp.Build()
            .SetLogger(new SerilogVelopackLogger())
            .Run();

        // Configure Serilog
        string logPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "KeyboardAutoSwitcher",
            "logs",
            "log.txt"
        );
        _ = Directory.CreateDirectory(Path.GetDirectoryName(logPath)!);

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
            _ = MessageBox.Show(
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

    /// <summary>
    /// Registers common services used by both GUI and Service modes
    /// </summary>
    private static void RegisterCommonServices(IServiceCollection services)
    {
        _ = services.AddSerilog();

        // Register registry service
        _ = services.AddSingleton<IRegistryService, WindowsRegistryService>();

        // Register USB device detector
        _ = services.AddSingleton<IUSBDeviceDetector, USBDeviceDetector>();

        // Register startup manager
        _ = services.AddSingleton<IStartupManager, StartupManager>();

        // Register background worker
        _ = services.AddHostedService<KeyboardSwitcherWorker>();
    }

    private static void RunAsGuiApplication(string[] args)
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        _ = Application.SetHighDpiMode(HighDpiMode.SystemAware);

        HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);
        RegisterCommonServices(builder.Services);

        // GUI-specific: Register update manager with Velopack
        _ = builder.Services.AddSingleton<IUpdateManager, UpdateService>();

        IHost host = builder.Build();

        // Resolve services from DI container
        IUpdateManager updateManager = host.Services.GetRequiredService<IUpdateManager>();
        IStartupManager startupManager = host.Services.GetRequiredService<IStartupManager>();

        using TrayApplicationContext context = new(host, updateManager, startupManager);
        Application.Run(context);
    }

    private static void RunAsService(string[] args)
    {
        HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);
        RegisterCommonServices(builder.Services);

        // Service-specific: Configure Windows Service
        _ = builder.Services.AddWindowsService(options =>
        {
            options.ServiceName = "Keyboard Auto Switcher";
        });

        IHost app = builder.Build();
        app.Run();
    }
}
