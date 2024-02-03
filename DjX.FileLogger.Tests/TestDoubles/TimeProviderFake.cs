using DjX.Providers.Abstractions;

namespace DjX.FileLogger.Tests.TestDoubles;
public class TimeProviderFake : ITimeProvider
{
    private readonly DateTime _now;

    public DateTime Now => _now;

    public DateTime Today => new(_now.Year, _now.Month, _now.Day);

    public TimeProviderFake(DateTime dateTimeNow)
    {
        _now = dateTimeNow;
    }
}
