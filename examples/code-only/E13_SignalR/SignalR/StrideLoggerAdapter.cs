using Microsoft.Extensions.Logging;
using Stride.Core.Diagnostics;
using MsLogger = Microsoft.Extensions.Logging.ILogger;

namespace E13_SignalR.SignalR;

/// <summary>
/// Bridges Microsoft.Extensions.Logging ILogger to Stride's logging system.
/// Messages are forwarded to the provided Stride <see cref="Logger"/>.
/// </summary>
/// <typeparam name="T">Category type for the logger.</typeparam>
public sealed class StrideLoggerAdapter<T>(Logger strideLogger) : ILogger<T>
{
    private readonly Logger _strideLogger = strideLogger ?? throw new ArgumentNullException(nameof(strideLogger));

    // Explicit interface implementations through an alias: Stride.Core.Diagnostics has an ILogger of its own
    IDisposable MsLogger.BeginScope<TState>(TState state) => NullScope.Instance;

    bool MsLogger.IsEnabled(LogLevel logLevel) => true; // Stride filtering can be done via listeners

    void MsLogger.Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (formatter is null) return;

        var message = formatter(state, exception);

        if (string.IsNullOrEmpty(message) && exception is null) return;

        var line = exception is null ? message : $"{message}\n{exception}";

        switch (logLevel)
        {
            case LogLevel.Trace:
            case LogLevel.Debug:
                _strideLogger.Debug(line);
                break;
            case LogLevel.Information:
                _strideLogger.Info(line);
                break;
            case LogLevel.Warning:
                _strideLogger.Warning(line);
                break;
            case LogLevel.Error:
                _strideLogger.Error(line);
                break;
            case LogLevel.Critical:
                _strideLogger.Fatal(line);
                break;
        }
    }

    private sealed class NullScope : IDisposable
    {
        public static readonly NullScope Instance = new();

        public void Dispose() { }
    }
}