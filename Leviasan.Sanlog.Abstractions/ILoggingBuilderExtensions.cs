using System;
using System.IO;
using System.ComponentModel;
using System.Security;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
//using Leviasan.Sanlog.EntityFrameworkCore;

namespace Leviasan.Sanlog
{
    /// <summary>
    /// Provides the <see cref="ILoggingBuilder"/> extension methods.
    /// </summary>
    public static class ILoggingBuilderExtensions
    {
        /*
        /// <summary>
        /// Adds Sanlog logging services with file storage.
        /// </summary>
        /// <param name="builder">The logging builder.</param>
        /// <param name="directory">The path to the log directory. The default is the current application directory.</param>
        /// <param name="filePrefix">The prefix of the file name used to store the logging information. The current date in the format YYYYMMDD is added after the specified value. The default is "diagnostics-".</param>
        /// <param name="fileSizeLimit">The maximum log size in bytes. Once the log is full behavior depends on <paramref name="strategy"/>. The default is 10MB.</param>
        /// <param name="fileCountLimit">The maximum retained file count. The default is 2.</param>
        /// <param name="strategy">Specifies the behavior to use when writing to a log file that is already full. The default is <see cref="FileLoggingFullMode.DropWrite"/>.</param>
        /// <param name="loggingConfigure">The configure options for logging.</param>
        /// <returns>The logging builder.</returns>
        /// <exception cref="ArgumentException">The <paramref name="directory"/> is a zero-length string or contains only white space.</exception>
        /// <exception cref="ArgumentNullException">The <paramref name="builder"/> or <paramref name="directory"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentOutOfRangeException">The <paramref name="fileSizeLimit"/> or <paramref name="fileCountLimit"/> is less then 0.</exception>
        /// <exception cref="DirectoryNotFoundException">The specified <paramref name="directory"/> is invalid (for example, it is on an unmapped drive).</exception>
        /// <exception cref="InvalidEnumArgumentException">The <paramref name="strategy"/> is invalid enumeration value.</exception>
        /// <exception cref="IOException">The <paramref name="directory"/> specified by path is a file. -or- The network name is not known.</exception>
        /// <exception cref="NotSupportedException">The <paramref name="directory"/> contains a colon character (:) that is not part of a drive label ("C:\").</exception>
        /// <exception cref="PathTooLongException">The specified <paramref name="directory"/> exceed the system-defined maximum length.</exception>
        /// <exception cref="SecurityException">The caller does not have the required permission.</exception>
        /// <exception cref="UnauthorizedAccessException">The caller does not have the required permission.</exception>
        public static ILoggingBuilder AddSanlogService(
            this ILoggingBuilder builder, string directory = "./", string? filePrefix = "diagnostics-",
            int fileSizeLimit = 10485760, int fileCountLimit = 2, FileLoggingFullMode strategy = FileLoggingFullMode.DropWrite,
            Action<SanlogLoggerOptions>? loggingConfigure = default)
        {
            ArgumentNullException.ThrowIfNull(builder);
            // Add configuration
            builder.AddConfiguration();
            // Register service to write log entries to storage
            _ = builder.Services.AddSingleton<UnboundedSingleConsumerChannel>();
            _ = builder.Services.AddSingleton(serviceProvider => new FileLoggingWriter(serviceProvider.GetRequiredService<UnboundedSingleConsumerChannel>(), directory, filePrefix, fileSizeLimit, fileCountLimit, strategy));
            // Register logger provider
            builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<ILoggerProvider, SanlogLoggerProvider>(
                serviceProvider => new SanlogLoggerProvider(
                    channelWriter: serviceProvider.GetRequiredService<UnboundedSingleConsumerChannel>(),
                    optionsMonitor: serviceProvider.GetRequiredService<IOptionsMonitor<SanlogLoggerOptions>>())));
            LoggerProviderOptions.RegisterProviderOptions<SanlogLoggerOptions, SanlogLoggerProvider>(builder.Services); // IL3050 + IL3026
            // Configure logging options
            if (loggingConfigure is not null) _ = builder.Services.Configure(loggingConfigure);
            return builder;
        }
        */
        /*
        // <summary>
        // Adds Sanlog logging services with EntityFrameworkCore storage as a hosted service.
        // </summary>
        // <param name="builder">The logging builder.</param>
        // <param name="contextConfigure">The configure options for context.</param>
        // <param name="loggingConfigure">The configure options for logging.</param>
        // <returns>The logging builder.</returns>
        // <exception cref="ArgumentNullException">The <paramref name="builder"/> or <paramref name="contextConfigure"/> is <see langword="null"/>.</exception>

        /// <summary>
        /// Adds Sanlog logging services with file storage as a hosted service.
        /// </summary>
        /// <param name="builder">The logging builder.</param>
        /// <param name="directory">The path to the log directory. The default is the current application directory.</param>
        /// <param name="filePrefix">The prefix of the file name used to store the logging information. The current date in the format YYYYMMDD is added after the specified value. The default is "diagnostics-".</param>
        /// <param name="fileSizeLimit">The maximum log size in bytes. Once the log is full behavior depends on <paramref name="strategy"/>. The default is 10MB.</param>
        /// <param name="fileCountLimit">The maximum retained file count. The default is 2.</param>
        /// <param name="strategy">Specifies the behavior to use when writing to a log file that is already full. The default is <see cref="FileLoggingFullMode.DropWrite"/>.</param>
        /// <param name="loggingConfigure">The configure options for logging.</param>
        /// <returns>The logging builder.</returns>
        /// <exception cref="ArgumentException">The <paramref name="directory"/> is a zero-length string or contains only white space.</exception>
        /// <exception cref="ArgumentNullException">The <paramref name="builder"/> or <paramref name="directory"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentOutOfRangeException">The <paramref name="fileSizeLimit"/> or <paramref name="fileCountLimit"/> is less then 0.</exception>
        /// <exception cref="DirectoryNotFoundException">The specified <paramref name="directory"/> is invalid (for example, it is on an unmapped drive).</exception>
        /// <exception cref="InvalidEnumArgumentException">The <paramref name="strategy"/> is invalid enumeration value.</exception>
        /// <exception cref="IOException">The <paramref name="directory"/> specified by path is a file. -or- The network name is not known.</exception>
        /// <exception cref="NotSupportedException">The <paramref name="directory"/> contains a colon character (:) that is not part of a drive label ("C:\").</exception>
        /// <exception cref="PathTooLongException">The specified <paramref name="directory"/> exceed the system-defined maximum length.</exception>
        /// <exception cref="SecurityException">The caller does not have the required permission.</exception>
        /// <exception cref="UnauthorizedAccessException">The caller does not have the required permission.</exception>
        public static ILoggingBuilder AddHostedSanlogService(
            this ILoggingBuilder builder, string directory = "./", string? filePrefix = "diagnostics-",
            int fileSizeLimit = 10485760, int fileCountLimit = 2, FileLoggingFullMode strategy = FileLoggingFullMode.DropWrite,
            Action<SanlogLoggerOptions>? loggingConfigure = default)
        {
            ArgumentNullException.ThrowIfNull(builder);
            // Add configuration
            builder.AddConfiguration();
            // Register clipboard between loggers and storage
            _ = builder.Services.AddSingleton<UnboundedSingleConsumerQueue>();




            // Register logger provider
            builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<ILoggerProvider, SanlogLoggerProvider>(
                serviceProvider => new SanlogLoggerProvider(
                    eventWriter: serviceProvider.GetRequiredService<UnboundedSingleConsumerQueue>(),
                    optionsMonitor: serviceProvider.GetRequiredService<IOptionsMonitor<SanlogLoggerOptions>>())));
            LoggerProviderOptions.RegisterProviderOptions<SanlogLoggerOptions, SanlogLoggerProvider>(builder.Services);
            // Register SanlogDbContext factory
            _ = builder.Services.AddDbContextFactory<SanlogDbContext>(contextConfigure);
            // Register background hosted service
            _ = builder.Services.AddHostedService<HostedSanlogDbContextWriter>();
            // Configure logging options
            if (loggingConfigure is not null) _ = builder.Services.Configure(loggingConfigure);
            return builder;
        }
        */
    }
}