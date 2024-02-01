using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Configuration;

namespace DjX.FileLogger;

public static class FileLoggerExtensions
{
    public static ILoggingBuilder AddDjXvFileLogger(this ILoggingBuilder builder)
    {
        builder.AddConfiguration();

        builder.Services.TryAddEnumerable(
            ServiceDescriptor.Singleton<ILoggerProvider, FileLoggerProvider>());

        LoggerProviderOptions.RegisterProviderOptions<
            FileLoggerConfiguration, FileLoggerProvider>(builder.Services);

        return builder;
    }

    public static ILoggingBuilder AddDjXvFileLogger(this ILoggingBuilder builder,
        Action<FileLoggerConfiguration> configure)
    {
        builder.AddDjXvFileLogger();
        builder.Services.Configure(configure);

        return builder;
    }
}
