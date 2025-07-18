using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Sanlog;

public static class ILoggingBuilderExtensions
{
    public static ILoggingBuilder AddSanlogEntityFrameworkCore(
        this ILoggingBuilder builder,
        Action<DbContextOptionsBuilder> contextConfigure,
        Action<SanlogLoggerOptions>? loggingConfigure = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(contextConfigure);

        builder.AddConfiguration();
        builder.Services
            .AddMessageBroker(builder => builder.SetHandler<MyCustomSanlogLoggerProvider, MyCustomLoggingEntryMessageHandler>()) // register here your IMessageHandler
            .Configure<SanlogLoggerOptions>(x => x.FormattedOptions ??= new LoggerFormatterOptions(LoggerFormatterOptions.Default))
            // Register your storage of logs entry
            .TryAddEnumerable(ServiceDescriptor.Singleton<ILoggerProvider, MyCustomSanlogLoggerProvider>());
        LoggerProviderOptions.RegisterProviderOptions<SanlogLoggerOptions, MyCustomSanlogLoggerProvider>(builder.Services);
        if (loggingConfigure is not null)
            _ = builder.Services.Configure(loggingConfigure);
        return builder;
    }
}