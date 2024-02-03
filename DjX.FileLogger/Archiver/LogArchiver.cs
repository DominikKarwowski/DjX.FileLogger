using DjX.Providers.Abstractions;

namespace DjX.FileLogger.Archiver;

public class LogArchiver
{
    private readonly string _logFilePath;
    private readonly LogArchivingPolicy _archivingPolicy;
    private readonly ITimeProvider _timeProvider;

    public LogArchiver(string logFilePath, LogArchivingPolicy archivingPolicy,
        ITimeProvider timeProvider)
    {
        _logFilePath = logFilePath ?? throw new ArgumentNullException(nameof(logFilePath));
        _archivingPolicy = archivingPolicy ?? throw new ArgumentNullException(nameof(logFilePath));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public void RunLogMaintenance()
    {
        try
        {
            var currentLogFile = new FileInfo(_logFilePath);

            if (currentLogFile.Exists)
            {
                ArchiveLogs(currentLogFile);
            }

            CleanUpLogs();
        }
        catch (Exception)
        {
            // do not throw exceptions from log maintenance
        }
    }

    private void CleanUpLogs()
    {
        if (_archivingPolicy.LogRetentionNbOfDays == 0)
        {
            return;
        }

        var archivedLogs = Directory.EnumerateFiles(_archivingPolicy.LogFileArchiveSubDirectoryPath);

        archivedLogs
            .Select(f => new FileInfo(f))
            .Where(f => IsLogEligibleForCleanUp(f.LastWriteTime, _archivingPolicy.LogRetentionNbOfDays))
            .ToList()
            .ForEach(f => f.Delete());
    }

    private void ArchiveLogs(FileInfo currentLogFile)
    {
        if (IsLogEligibleForArchiving(currentLogFile.LastWriteTime, _archivingPolicy.ArchivingFrequency))
        {
            if (!Directory.Exists(_archivingPolicy.LogFileArchiveSubDirectoryPath))
            {
                Directory.CreateDirectory(_archivingPolicy.LogFileArchiveSubDirectoryPath);
            }

            var archivedLogFilePrefix = GetLogFilePrefix(_archivingPolicy.ArchivingFrequency);

            var archivedLogFilePath =
                Path.Combine(
                    _archivingPolicy.LogFileArchiveSubDirectoryPath,
                    archivedLogFilePrefix + currentLogFile.Name);

            archivedLogFilePath = CheckAddSuffix(archivedLogFilePath);

            File.Copy(_logFilePath, archivedLogFilePath);
            File.Delete(_logFilePath);
        }
    }

    private static string CheckAddSuffix(string archivedLogFilePath)
    {
        var suffix = 0;
        var tempFileName = archivedLogFilePath;

        while (File.Exists(tempFileName))
        {
            tempFileName += $"_{++suffix}";
            if (File.Exists(tempFileName))
            {
                tempFileName = archivedLogFilePath;
            }
        }

        return tempFileName;
    }

    private bool IsLogEligibleForArchiving(DateTime logLastWriteTime, ArchivingFrequency archivingFrequency)
    {
        var currentTime = _timeProvider.Today;

        var logLastWriteDate =
            new DateTime(logLastWriteTime.Year, logLastWriteTime.Month, logLastWriteTime.Day);

        return archivingFrequency switch
        {
            ArchivingFrequency.Daily =>
                currentTime - logLastWriteDate >= TimeSpan.FromDays(1),
            ArchivingFrequency.Monthly =>
                currentTime.Month > 1
                    ? logLastWriteDate.Month < currentTime.Month
                    : logLastWriteDate.Year < currentTime.Year,
            ArchivingFrequency.Yearly =>
                logLastWriteDate.Year < currentTime.Year,
            _ =>
                false,
        };
    }

    private bool IsLogEligibleForCleanUp(DateTime logLastWriteTime, uint nbOfDaysForLogRetention)
    {
        var currentTime = _timeProvider.Today;

        var logLastWriteDate =
            new DateTime(logLastWriteTime.Year, logLastWriteTime.Month, logLastWriteTime.Day);

        return currentTime - logLastWriteDate >= TimeSpan.FromDays(nbOfDaysForLogRetention + 1);
    }

    private string GetLogFilePrefix(ArchivingFrequency archivingFrequency)
    {
        var currentTime = _timeProvider.Now;

        return archivingFrequency switch
        {
            ArchivingFrequency.Daily =>
                (currentTime - TimeSpan.FromDays(1))
                    .ToString("yyyy-MM-dd") + "_",
            ArchivingFrequency.Monthly =>
                currentTime.Month > 1
                    ? new DateTime(currentTime.Year, currentTime.Month - 1, 1).ToString("yyyy-MM") + "_"
                    : new DateTime(currentTime.Year - 1, 12, 1).ToString("yyyy-MM") + "_",
            ArchivingFrequency.Yearly =>
                new DateTime(currentTime.Year - 1, 1, 1).ToString("yyyy") + "_",
            _ => string.Empty,
        };
    }
}

