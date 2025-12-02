using KeyboardAutoSwitcher;
using KeyboardAutoSwitcher.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

internal class Program
{
    public static async Task Main(string[] args)
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
            .WriteTo.Console()
            .WriteTo.File(logPath, 
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 7,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
            .CreateLogger();

        try
        {
            Log.Information("Starting Keyboard Auto Switcher");

            var builder = Host.CreateApplicationBuilder(args);

            // Use Serilog for logging
            builder.Services.AddSerilog();

            // Enable Windows Service integration when running as a service
            builder.Services.AddWindowsService(options =>
            {
                options.ServiceName = "Keyboard Auto Switcher";
            });

            // Register USB device detector
            builder.Services.AddSingleton<IUSBDeviceDetector, USBDeviceDetector>();

            // Add our background worker
            builder.Services.AddHostedService<KeyboardSwitcherWorker>();

            var app = builder.Build();
            await app.RunAsync();
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Application terminated unexpectedly");
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }
}