namespace DjX.FileLogger.Archiver;

public record LogArchivingPolicy(
    string LogFileArchiveSubDirectoryPath,
    ArchivingFrequency ArchivingFrequency,
    uint LogRetentionNbOfDays = 0);

public enum ArchivingFrequency
{
    Daily, Monthly, Yearly
}
