using Microsoft.Extensions.Logging;

namespace DjX.FileLogger;

using static DjX.FileLogger.LogWriter;

public sealed class FileLogger : ILogger
{
    private readonly string _name;
    private readonly Func<FileLoggerConfiguration> _getCurrentConfig;

    public FileLogger(
        string name,
        Func<FileLoggerConfiguration> getCurrentConfig) =>
        (_name, _getCurrentConfig) = (name, getCurrentConfig);

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => default!;

    public bool IsEnabled(LogLevel logLevel) => _getCurrentConfig().LogLevel <= logLevel;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel)) return;

        var config = _getCurrentConfig();

        if (config.EventId != 0 && config.EventId != eventId) return;

        formatter ??= GetDefaultFormatter<TState>();

        var value = formatter(state, exception);

        if (string.IsNullOrEmpty(value)) return;

        try
        {
            string path = !string.IsNullOrWhiteSpace(config.LogFilePath)
                ? config.LogFilePath
                : GetDefaultLogFileName();

            // TODO: implement producer-consumer pattern correctly
            Task.Run(() => WriteLogEntry(value, path, _name, logLevel, eventId.Id));
        }
        catch (Exception)
        {
            config.ExecuteFallbackLogger?.Invoke();
        }
    }

    private static string GetDefaultLogFileName() =>
        AppDomain.CurrentDomain.FriendlyName + ".log";

    private static Func<TState, Exception?, string> GetDefaultFormatter<TState>() =>
        (s, ex) =>
        {
            string? result;

            if (ex == null) result = s?.ToString();
            else result = s?.ToString() + Environment.NewLine + ex.ToString();
            return result ?? string.Empty;
        };
}
