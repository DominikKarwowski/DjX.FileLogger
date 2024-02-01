using DjX.FileLogger;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace DjK.BackupTool.Core.Tests.Logger;

[TestFixture]
public partial class FileLoggerTests
{
    const string _logFileName = "testLogFile";
    string _logFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, _logFileName);

    [TearDown]
    public void CleanUp()
    {
        var logFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, _logFileName);

        if (File.Exists(logFile))
        {
            File.Delete(logFile);
        }
    }

    [Test]
    public async Task Logger_writes_formatted_log_message_to_file()
    {
        var sut = CreateLogger();

        sut.LogInformation("test log message");

        await Task.Delay(100);

        var result = File.ReadAllText(_logFile);

        Assert.That(result, Does.Match(LogEntry()));
    }

    [Test]
    public async Task Logger_is_capable_of_handling_multiple_messages_at_once()
    {
        var sut = CreateLogger();

        var logTask1 = Task.Run(() => sut.LogInformation("test log message 1"));
        var logTask2 = Task.Run(() => sut.LogInformation("test log message 2"));
        var logTask3 = Task.Run(() => sut.LogInformation("test log message 3"));
        var logTask4 = Task.Run(() => sut.LogInformation("test log message 4"));
        var logTask5 = Task.Run(() => sut.LogInformation("test log message 5"));
        var logTask6 = Task.Run(() => sut.LogInformation("test log message 6"));

        await Task.Delay(100);

        var result = File.ReadAllText(_logFile);

        Assert.That(result, Does.Match(MultilineLogEntry()));
    }

    [Test]
    public async Task Logger_writes_timestamps_in_chronological_order_for_multiple_messages_requested_to_be_logged_at_once()
    {
        var sut = CreateLogger();

        for (int i = 0; i < 50; i++)
        {
            var logTask = Task.Run(() => sut.LogInformation("test log message"));
        }

        await Task.Delay(150);

        var logLines = File.ReadAllLines(_logFile);

        var timestamps =
            logLines
                .Select(l => DateTime.Parse(l[..23]));

        var timestampsOrdered = timestamps.OrderBy(l => l);

        Assert.Multiple(() =>
        {
            Assert.That(timestamps.Count(), Is.EqualTo(50));
            CollectionAssert.AreEqual(timestamps, timestampsOrdered);
        });
    }

    [Test]
    public async Task Logger_is_capable_of_handling_multiple_messages_from_different_sources_at_once()
    {
        var sut1 = CreateLogger("TestLogCategory1");
        var sut2 = CreateLogger("TestLogCategory2");
        var sut3 = CreateLogger("TestLogCategory3");

        var logTask1 = Task.Run(() => sut1.LogInformation("test log message 1"));
        var logTask2 = Task.Run(() => sut2.LogInformation("test log message 2"));
        var logTask3 = Task.Run(() => sut3.LogInformation("test log message 3"));
        var logTask4 = Task.Run(() => sut1.LogInformation("test log message 4"));
        var logTask5 = Task.Run(() => sut2.LogInformation("test log message 5"));
        var logTask6 = Task.Run(() => sut3.LogInformation("test log message 6"));

        await Task.Delay(100);

        var result = File.ReadAllText(_logFile);

        Assert.That(result, Does.Match(MultisourceLogEntry()));
    }

    [Test]
    public async Task Logger_writes_timestamps_in_chronological_order_for_multiple_messages_requested_to_be_logged_at_once_from_different_sources()
    {
        var sut1 = CreateLogger("TestLogCategory1");
        var sut2 = CreateLogger("TestLogCategory2");
        var sut3 = CreateLogger("TestLogCategory3");

        for (int i = 0; i < 30; i++)
        {
            var logTask1 = Task.Run(() => sut1.LogInformation("test log message"));
            var logTask2 = Task.Run(() => sut2.LogInformation("test log message"));
            var logTask3 = Task.Run(() => sut3.LogInformation("test log message"));
        }

        await Task.Delay(250);

        var logLines = File.ReadAllLines(_logFile);

        var timestamps =
            logLines
                .Select(l => DateTime.Parse(l[..23]));

        var timestampsOrdered = timestamps.OrderBy(l => l);

        Assert.Multiple(() =>
        {
            Assert.That(timestamps.Count(), Is.EqualTo(90));
            CollectionAssert.AreEqual(timestamps, timestampsOrdered);
        });
    }


    [Test]
    public async Task Logger_only_writes_entries_for_a_log_level_equal_or_below_specified()
    {
        var sut = CreateLogger(logLevel: LogLevel.Error);

        sut.LogTrace("Trace level log");
        sut.LogDebug("Debug level log");
        sut.LogInformation("Information level log");
        sut.LogError("Error level log");
        sut.LogCritical("Critical level log");

        await Task.Delay(50);

        var result = File.ReadAllText(_logFile);

        Assert.Multiple(() =>
        {
            Assert.That(result, Does.Not.Contain("Trace level log"));
            Assert.That(result, Does.Not.Contain("Debug level log"));
            Assert.That(result, Does.Not.Contain("Information level log"));
            Assert.That(result, Does.Contain("Error level log"));
            Assert.That(result, Does.Contain("Critical level log"));
        });
    }


    private FileLogger CreateLogger(string name = "TestLogger", LogLevel logLevel = LogLevel.Debug)
    {
        return new FileLogger(
            name,
            () => new FileLoggerConfiguration()
            {
                LogFilePath = _logFile,
                LogLevel = logLevel,
            });
    }

    [GeneratedRegex(@"^[0-9]{4}-[0-9]{2}-[0-9]{2}\s[0-9]{2}:[0-9]{2}:[0-9]{2}\.[0-9]{3}\s-\s\[INFORMATION]\s-\sCategory:\sTestLogger\s-\sEventId:\s0\s-\stest\slog\smessage\r\n$", RegexOptions.Multiline)]
    private static partial Regex LogEntry();

    [GeneratedRegex(@"^[0-9]{4}-[0-9]{2}-[0-9]{2}\s[0-9]{2}:[0-9]{2}:[0-9]{2}\.[0-9]{3}\s-\s\[INFORMATION]\s-\sCategory:\sTestLogger\s-\sEventId:\s0\s-\stest\slog\smessage\s[1-6]{1}
[0-9]{4}-[0-9]{2}-[0-9]{2}\s[0-9]{2}:[0-9]{2}:[0-9]{2}\.[0-9]{3}\s-\s\[INFORMATION]\s-\sCategory:\sTestLogger\s-\sEventId:\s0\s-\stest\slog\smessage\s[1-6]{1}
[0-9]{4}-[0-9]{2}-[0-9]{2}\s[0-9]{2}:[0-9]{2}:[0-9]{2}\.[0-9]{3}\s-\s\[INFORMATION]\s-\sCategory:\sTestLogger\s-\sEventId:\s0\s-\stest\slog\smessage\s[1-6]{1}
[0-9]{4}-[0-9]{2}-[0-9]{2}\s[0-9]{2}:[0-9]{2}:[0-9]{2}\.[0-9]{3}\s-\s\[INFORMATION]\s-\sCategory:\sTestLogger\s-\sEventId:\s0\s-\stest\slog\smessage\s[1-6]{1}
[0-9]{4}-[0-9]{2}-[0-9]{2}\s[0-9]{2}:[0-9]{2}:[0-9]{2}\.[0-9]{3}\s-\s\[INFORMATION]\s-\sCategory:\sTestLogger\s-\sEventId:\s0\s-\stest\slog\smessage\s[1-6]{1}
[0-9]{4}-[0-9]{2}-[0-9]{2}\s[0-9]{2}:[0-9]{2}:[0-9]{2}\.[0-9]{3}\s-\s\[INFORMATION]\s-\sCategory:\sTestLogger\s-\sEventId:\s0\s-\stest\slog\smessage\s[1-6]{1}
$", RegexOptions.Multiline)]
    private static partial Regex MultilineLogEntry();

    [GeneratedRegex(@"^[0-9]{4}-[0-9]{2}-[0-9]{2}\s[0-9]{2}:[0-9]{2}:[0-9]{2}\.[0-9]{3}\s-\s\[INFORMATION]\s-\sCategory:\s[0-9a-zA-Z]*\s-\sEventId:\s0\s-\stest\slog\smessage\s[1-6]{1}
[0-9]{4}-[0-9]{2}-[0-9]{2}\s[0-9]{2}:[0-9]{2}:[0-9]{2}\.[0-9]{3}\s-\s\[INFORMATION]\s-\sCategory:\s[0-9a-zA-Z]*\s-\sEventId:\s0\s-\stest\slog\smessage\s[1-6]{1}
[0-9]{4}-[0-9]{2}-[0-9]{2}\s[0-9]{2}:[0-9]{2}:[0-9]{2}\.[0-9]{3}\s-\s\[INFORMATION]\s-\sCategory:\s[0-9a-zA-Z]*\s-\sEventId:\s0\s-\stest\slog\smessage\s[1-6]{1}
[0-9]{4}-[0-9]{2}-[0-9]{2}\s[0-9]{2}:[0-9]{2}:[0-9]{2}\.[0-9]{3}\s-\s\[INFORMATION]\s-\sCategory:\s[0-9a-zA-Z]*\s-\sEventId:\s0\s-\stest\slog\smessage\s[1-6]{1}
[0-9]{4}-[0-9]{2}-[0-9]{2}\s[0-9]{2}:[0-9]{2}:[0-9]{2}\.[0-9]{3}\s-\s\[INFORMATION]\s-\sCategory:\s[0-9a-zA-Z]*\s-\sEventId:\s0\s-\stest\slog\smessage\s[1-6]{1}
[0-9]{4}-[0-9]{2}-[0-9]{2}\s[0-9]{2}:[0-9]{2}:[0-9]{2}\.[0-9]{3}\s-\s\[INFORMATION]\s-\sCategory:\s[0-9a-zA-Z]*\s-\sEventId:\s0\s-\stest\slog\smessage\s[1-6]{1}
$", RegexOptions.Multiline)]
    private static partial Regex MultisourceLogEntry();
}
