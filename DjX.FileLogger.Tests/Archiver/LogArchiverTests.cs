using DjX.FileLogger.Archiver;
using DjX.FileLogger.Tests.TestDoubles;

namespace DjX.FileLogger.Tests.Archiver;

[TestFixture]
public class LogArchiverTests
{
    private readonly string _logFileDirectoryPath = AppDomain.CurrentDomain.BaseDirectory;
    private readonly string _logFileName = "testLogFile.log";

    private string? _logFilePath;
    private string? _archivedLogFilePath;
    private string? _logArchiveDirectoryPath;

    [TearDown]
    public void CleanUp()
    {
        if (File.Exists(_logFilePath))
        {
            File.Delete(_logFilePath);
        }

        if (Directory.Exists(_logArchiveDirectoryPath))
        {
            Directory.Delete(_logArchiveDirectoryPath, true);
        }
    }

    [TestCase(2020, 1, 1, "2019-12-31_")]
    [TestCase(2020, 12, 31, "2020-12-30_")]
    [TestCase(2020, 3, 1, "2020-02-29_")]
    [TestCase(2021, 3, 1, "2021-02-28_")]
    [TestCase(2121, 8, 10, "2121-08-09_")]
    public void Archive_logfile_if_older_than_one_day_for_daily_archiving_frequency(
        int year, int month, int day, string archiveLogFileNamePrefix)
    {
        InitializeLogPaths(archiveLogFileNamePrefix + "testLogFile.log");

        File.AppendAllText(_logFilePath!, "dummy file content");

        var logFile = new FileInfo(_logFilePath!)
        {
            LastWriteTime = new DateTime(year, month, day) - TimeSpan.FromDays(1)
        };

        var sut = CreateDjXvFileLogArchiver(
            new LogArchivingPolicy(_logArchiveDirectoryPath!, ArchivingFrequency.Daily),
            year, month, day);

        sut.RunLogMaintenance();

        Assert.Multiple(() =>
        {
            Assert.That(File.Exists(_logFilePath), Is.False);
            Assert.That(File.Exists(_archivedLogFilePath), Is.True);
        });
    }

    [Test]
    public void Do_not_archive_logfile_if_not_older_than_one_day_for_daily_archiving_frequency()
    {
        InitializeLogPaths("1989-05-14_testLogFile.log");

        File.AppendAllText(_logFilePath!, "dummy file content");

        var logFile = new FileInfo(_logFilePath!)
        {
            LastWriteTime = new DateTime(1989, 5, 15)
        };

        var sut = CreateDjXvFileLogArchiver(
            new LogArchivingPolicy(_logArchiveDirectoryPath!, ArchivingFrequency.Daily),
            1989, 5, 15);

        sut.RunLogMaintenance();

        Assert.Multiple(() =>
        {
            Assert.That(File.Exists(_logFilePath), Is.True);
            Assert.That(File.Exists(_archivedLogFilePath), Is.False);
        });
    }

    [TestCase(2020, 1, "2019-12_")]
    [TestCase(2020, 12, "2020-11_")]
    [TestCase(2020, 3, "2020-02_")]
    [TestCase(2021, 3, "2021-02_")]
    [TestCase(2121, 8, "2121-07_")]
    public void Archive_logfile_if_older_than_one_month_for_monthly_archiving_frequency(
        int year, int month, string archiveLogFileNamePrefix)
    {
        InitializeLogPaths(archiveLogFileNamePrefix + "testLogFile.log");

        File.AppendAllText(_logFilePath!, "dummy file content");

        var logFile = new FileInfo(_logFilePath!)
        {
            LastWriteTime =
                month > 1
                    ? new DateTime(year, month - 1, 1)
                    : new DateTime(year - 1, 12, 1)
        };

        var sut = CreateDjXvFileLogArchiver(
            new LogArchivingPolicy(_logArchiveDirectoryPath!, ArchivingFrequency.Monthly),
            year, month, 1);

        sut.RunLogMaintenance();

        Assert.Multiple(() =>
        {
            Assert.That(File.Exists(_logFilePath), Is.False);
            Assert.That(File.Exists(_archivedLogFilePath), Is.True);
        });
    }

    [Test]
    public void Do_not_archive_logfile_if_not_older_than_one_month_for_monthly_archiving_frequency()
    {
        InitializeLogPaths("1989-05_testLogFile.log");

        File.AppendAllText(_logFilePath!, "dummy file content");

        var logFile = new FileInfo(_logFilePath!)
        {
            LastWriteTime = new DateTime(1989, 5, 1)
        };

        var sut = CreateDjXvFileLogArchiver(
            new LogArchivingPolicy(_logArchiveDirectoryPath!, ArchivingFrequency.Monthly),
            1989, 5, 15);

        sut.RunLogMaintenance();

        Assert.Multiple(() =>
        {
            Assert.That(File.Exists(_logFilePath), Is.True);
            Assert.That(File.Exists(_archivedLogFilePath), Is.False);
        });
    }

    [Test]
    public void Archive_logfile_if_older_than_one_month_for_monthly_archiving_frequency()
    {
        InitializeLogPaths("2015_testLogFile.log");

        File.AppendAllText(_logFilePath!, "dummy file content");

        var logFile = new FileInfo(_logFilePath!)
        {
            LastWriteTime = new DateTime(2015, 12, 31)
        };

        var sut = CreateDjXvFileLogArchiver(
            new LogArchivingPolicy(_logArchiveDirectoryPath!, ArchivingFrequency.Yearly),
            2016, 1, 1);

        sut.RunLogMaintenance();

        Assert.Multiple(() =>
        {
            Assert.That(File.Exists(_logFilePath), Is.False);
            Assert.That(File.Exists(_archivedLogFilePath), Is.True);
        });
    }

    [Test]
    public void Do_not_archive_logfile_if_not_older_than_one_year_for_yearly_archiving_frequency()
    {
        InitializeLogPaths("1989_testLogFile.log");

        File.AppendAllText(_logFilePath!, "dummy file content");

        var logFile = new FileInfo(_logFilePath!)
        {
            LastWriteTime = new DateTime(1989, 1, 1)
        };

        var sut = CreateDjXvFileLogArchiver(
            new LogArchivingPolicy(_logArchiveDirectoryPath!, ArchivingFrequency.Yearly),
            1989, 5, 15);

        sut.RunLogMaintenance();

        Assert.Multiple(() =>
        {
            Assert.That(File.Exists(_logFilePath), Is.True);
            Assert.That(File.Exists(_archivedLogFilePath), Is.False);
        });
    }

    [Test]
    public void Add_suffix_to_the_archive_log_file_if_one_with_the_same_name_already_exists_and_keep_original_file()
    {
        InitializeLogPaths("1988_testLogFile.log");

        File.AppendAllText(_logFilePath!, "dummy file content");

        Directory.CreateDirectory(_logArchiveDirectoryPath!);
        File.AppendAllText(_archivedLogFilePath!, "hey! I'm already here");

        var expectedNewArchivedLogFilePath = _archivedLogFilePath + "_1";

        var logFile = new FileInfo(_logFilePath!)
        {
            LastWriteTime = new DateTime(1988, 12, 12)
        };

        var sut = CreateDjXvFileLogArchiver(
            new LogArchivingPolicy(_logArchiveDirectoryPath!, ArchivingFrequency.Yearly),
            1989, 5, 15);

        sut.RunLogMaintenance();

        Assert.Multiple(() =>
        {
            Assert.That(File.Exists(_logFilePath), Is.False);
            Assert.That(File.Exists(_archivedLogFilePath), Is.True);
            Assert.That(File.Exists(expectedNewArchivedLogFilePath), Is.True);
        });
    }

    [Test]
    public void Add_incremental_suffix_to_the_archive_log_file_if_one_with_the_same_name_already_exists_and_keep_original_files()
    {
        InitializeLogPaths("1988_testLogFile.log");

        var suffixedArchivedLogFilePath = _archivedLogFilePath + "_1";

        File.AppendAllText(_logFilePath!, "dummy file content");

        Directory.CreateDirectory(_logArchiveDirectoryPath!);
        File.AppendAllText(_archivedLogFilePath!, "hey! I'm already here");
        File.AppendAllText(suffixedArchivedLogFilePath!, "and I'm already here, too");

        var expectedNewArchivedLogFilePath = _archivedLogFilePath + "_2";

        var logFile = new FileInfo(_logFilePath!)
        {
            LastWriteTime = new DateTime(1988, 12, 12)
        };

        var sut = CreateDjXvFileLogArchiver(
            new LogArchivingPolicy(_logArchiveDirectoryPath!, ArchivingFrequency.Yearly),
            1989, 5, 15);

        sut.RunLogMaintenance();

        Assert.Multiple(() =>
        {
            Assert.That(File.Exists(_logFilePath), Is.False);
            Assert.That(File.Exists(_archivedLogFilePath), Is.True);
            Assert.That(File.Exists(suffixedArchivedLogFilePath), Is.True);
            Assert.That(File.Exists(expectedNewArchivedLogFilePath), Is.True);
        });
    }

    [Test]
    public void Remove_only_archived_logs_older_than_specified_in_a_policy()
    {
        InitializeLogPaths("2007-04-10_testLogFile.log");

        // add archived log files
        var archivedLogFile1Path = Path.Combine(_logArchiveDirectoryPath!, "2006-05-16_testLogFile.log");
        var archivedLogFile2Path = Path.Combine(_logArchiveDirectoryPath!, "2006-05-15_testLogFile.log");
        var archivedLogFile3Path = Path.Combine(_logArchiveDirectoryPath!, "2006-05-14_testLogFile.log");
        var archivedLogFile4Path = Path.Combine(_logArchiveDirectoryPath!, "2006-04-30_testLogFile.log");
        var archivedLogFile5Path = Path.Combine(_logArchiveDirectoryPath!, "2005-01-01_testLogFile.log");

        Directory.CreateDirectory(_logArchiveDirectoryPath!);

        File.AppendAllText(_archivedLogFilePath!, "some log content");
        File.AppendAllText(archivedLogFile1Path, "some log content");
        File.AppendAllText(archivedLogFile2Path, "some log content");
        File.AppendAllText(archivedLogFile3Path, "some log content");
        File.AppendAllText(archivedLogFile4Path, "some log content");
        File.AppendAllText(archivedLogFile5Path, "some log content");

        var archivedlogFile = new FileInfo(_archivedLogFilePath!)
        {
            LastWriteTime = new DateTime(2007, 04, 10)
        };

        var archivedLogFile1 = new FileInfo(archivedLogFile1Path)
        {
            LastWriteTime = new DateTime(2006, 05, 16)
        };

        var archivedLogFile2 = new FileInfo(archivedLogFile2Path)
        {
            LastWriteTime = new DateTime(2006, 05, 15)
        };

        var archivedLogFile3 = new FileInfo(archivedLogFile3Path)
        {
            LastWriteTime = new DateTime(2006, 05, 14)
        };

        var archivedLogFile4 = new FileInfo(archivedLogFile4Path)
        {
            LastWriteTime = new DateTime(2006, 04, 30)
        };

        var archivedLogFile5 = new FileInfo(archivedLogFile5Path)
        {
            LastWriteTime = new DateTime(2005, 01, 01)
        };

        var sut = CreateDjXvFileLogArchiver(
            new LogArchivingPolicy(_logArchiveDirectoryPath!, ArchivingFrequency.Daily, 365),
            2007, 5, 15);

        sut.RunLogMaintenance();

        Assert.Multiple(() =>
        {
            Assert.That(File.Exists(_archivedLogFilePath), Is.True);
            Assert.That(File.Exists(archivedLogFile1Path), Is.True);
            Assert.That(File.Exists(archivedLogFile2Path), Is.True);
            Assert.That(File.Exists(archivedLogFile3Path), Is.False);
            Assert.That(File.Exists(archivedLogFile4Path), Is.False);
            Assert.That(File.Exists(archivedLogFile5Path), Is.False);
        });
    }

    [Test]
    public void Keep_all_archived_logs_if_retention_time_is_set_to_default_value_of_zero()
    {
        InitializeLogPaths("2007-04-10_testLogFile.log");

        // add archived log files
        var archivedLogFile1Path = Path.Combine(_logArchiveDirectoryPath!, "2006-05-16_testLogFile.log");
        var archivedLogFile2Path = Path.Combine(_logArchiveDirectoryPath!, "2006-05-15_testLogFile.log");
        var archivedLogFile3Path = Path.Combine(_logArchiveDirectoryPath!, "2006-04-30_testLogFile.log");
        var archivedLogFile4Path = Path.Combine(_logArchiveDirectoryPath!, "2005-01-01_testLogFile.log");

        Directory.CreateDirectory(_logArchiveDirectoryPath!);

        File.AppendAllText(_archivedLogFilePath!, "some log content");
        File.AppendAllText(archivedLogFile1Path, "some log content");
        File.AppendAllText(archivedLogFile2Path, "some log content");
        File.AppendAllText(archivedLogFile3Path, "some log content");
        File.AppendAllText(archivedLogFile4Path, "some log content");

        var archivedlogFile = new FileInfo(_archivedLogFilePath!)
        {
            LastWriteTime = new DateTime(2007, 04, 10)
        };

        var archivedLogFile1 = new FileInfo(archivedLogFile1Path)
        {
            LastWriteTime = new DateTime(2006, 05, 16)
        };

        var archivedLogFile2 = new FileInfo(archivedLogFile2Path)
        {
            LastWriteTime = new DateTime(2006, 05, 15)
        };

        var archivedLogFile3 = new FileInfo(archivedLogFile3Path)
        {
            LastWriteTime = new DateTime(2006, 04, 30)
        };

        var archivedLogFile4 = new FileInfo(archivedLogFile4Path)
        {
            LastWriteTime = new DateTime(2005, 01, 01)
        };

        var sut = CreateDjXvFileLogArchiver(
            new LogArchivingPolicy(_logArchiveDirectoryPath!, ArchivingFrequency.Daily),
            2007, 5, 15);

        sut.RunLogMaintenance();

        Assert.Multiple(() =>
        {
            Assert.That(File.Exists(_archivedLogFilePath), Is.True);
            Assert.That(File.Exists(archivedLogFile1Path), Is.True);
            Assert.That(File.Exists(archivedLogFile2Path), Is.True);
            Assert.That(File.Exists(archivedLogFile3Path), Is.True);
            Assert.That(File.Exists(archivedLogFile4Path), Is.True);
        });
    }

    private void InitializeLogPaths(string archivedLogFileName)
    {
        _logFilePath = Path.Combine(_logFileDirectoryPath, _logFileName);
        _logArchiveDirectoryPath = Path.Combine(_logFileDirectoryPath, "ArchivedLogs");
        _archivedLogFilePath = Path.Combine(_logArchiveDirectoryPath, archivedLogFileName);
    }

    private LogArchiver CreateDjXvFileLogArchiver(
        LogArchivingPolicy archivingPolicy,
        int currentYear,
        int currentMonth,
        int currentDay) =>
        new(
            _logFilePath!,
            archivingPolicy,
            new TimeProviderFake(
                new DateTime(
                    currentYear,
                    currentMonth,
                    currentDay)));
}
