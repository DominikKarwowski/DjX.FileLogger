using Microsoft.Extensions.Logging;

namespace DjX.FileLogger;

public sealed class FileLoggerConfiguration
{
    public int EventId { get; }
    public LogLevel LogLevel { get; set; }
    public string? LogFilePath { get; set; }
    public Action? ExecuteFallbackLogger { get; set; }
}
