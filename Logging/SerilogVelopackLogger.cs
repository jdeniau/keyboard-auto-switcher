using Serilog;
using Velopack.Logging;

namespace KeyboardAutoSwitcher.Logging;

/// <summary>
/// Adapter that redirects Velopack logs to Serilog
/// </summary>
public class SerilogVelopackLogger : IVelopackLogger
{
    public void Log(VelopackLogLevel logLevel, string? message, Exception? exception)
    {
        if (string.IsNullOrEmpty(message) && exception == null)
            return;

        var prefixedMessage = $"[Velopack] {message}";

        switch (logLevel)
        {
            case VelopackLogLevel.Trace:
                Serilog.Log.Verbose(exception, prefixedMessage);
                break;
            case VelopackLogLevel.Debug:
                Serilog.Log.Debug(exception, prefixedMessage);
                break;
            case VelopackLogLevel.Information:
                Serilog.Log.Information(exception, prefixedMessage);
                break;
            case VelopackLogLevel.Warning:
                Serilog.Log.Warning(exception, prefixedMessage);
                break;
            case VelopackLogLevel.Error:
                Serilog.Log.Error(exception, prefixedMessage);
                break;
            case VelopackLogLevel.Critical:
                Serilog.Log.Fatal(exception, prefixedMessage);
                break;
            default:
                Serilog.Log.Information(exception, prefixedMessage);
                break;
        }
    }
}
