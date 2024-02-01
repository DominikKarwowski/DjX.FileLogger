using Microsoft.Extensions.Logging;
using System.Text;

namespace DjX.FileLogger;

public static class LogWriter
{
    private static readonly object _lockObject = new();

    public static void WriteLogEntry(string logMessage, string logFilePath, string name, LogLevel logLevel, int eventId)
    {
        // TODO: check how to achieve thread safety in a clean and performant manner - publisher-consumer
        // what about message queue?
        // https://stackoverflow.com/questions/63851259/since-iloggert-is-a-singleton-how-different-threads-can-use-beginscope-with
        // https://stackoverflow.com/questions/6195084/thread-safe-logging-class-implementation

        lock (_lockObject)
        {
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            var logLine = string.Format("{0} - [{1}] - Category: {2} - EventId: {3} - {4}{5}",
                timestamp,
                logLevel.ToString().ToUpper(),
                name,
                eventId,
                logMessage,
                Environment.NewLine);

            File.AppendAllText(logFilePath, logLine, Encoding.UTF8);
        }
    }
}