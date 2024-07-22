using System;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Configuration;
using Microsoft.Extensions.Options;

namespace Leviasan.Sanlog
{
    /// <summary>
    /// Provides the <see cref="ILoggingBuilder"/> extension methods.
    /// </summary>
    public static class ILoggingBuilderExtensions
    {
        /// <summary>
        /// Adds Sanlog logging services with file storage.
        /// </summary>
        /// <param name="builder">The logging builder.</param>
        /// <param name="loggingConfigure">The configure options for logging.</param>
        /// <param name="directory">The path to the log directory. The default is the current application directory.</param>
        /// <param name="filePrefix">The prefix of the file name used to store the logging information. The current date in the format YYYYMMDD is added after the specified value. The default is "diagnostics-".</param>
        /// <param name="fileSizeLimit">The maximum log size in bytes. Once the log is full behavior depends on <paramref name="strategy"/>. The default is 10MB.</param>
        /// <param name="fileCountLimit">The maximum retained file count. The default is 2.</param>
        /// <param name="strategy">Specifies the behavior to use when writing to a log file that is already full. The default is <see cref="FileLoggerWriterMode.DropWrite"/>.</param>
        /// <param name="encoding">The encoding in which the output is written. The default is <see cref="Encoding.UTF8"/>.</param>
        /// <param name="allowSynchronousContinuations"><see langword="true"/> if operations performed on a channel may synchronously invoke continuations subscribed to notifications of pending async operations;
        /// <see langword="false"/> if all continuations should be invoked asynchronously.</param>
        /// <returns>The logging builder.</returns>
        [RequiresDynamicCode("Binding TOptions to configuration values may require generating dynamic code at runtime.")]
        [RequiresUnreferencedCode("TOptions's dependent types may have their members trimmed. Ensure all required members are preserved.")]
        public static ILoggingBuilder AddSanlogLogger(this ILoggingBuilder builder, Action<SanlogLoggerOptions>? loggingConfigure = default, string directory = "./", string? filePrefix = "diagnostics-", int fileSizeLimit = 10485760, int fileCountLimit = 2,
            FileLoggerWriterMode strategy = FileLoggerWriterMode.DropWrite, Encoding? encoding = null, bool allowSynchronousContinuations = false)
        {
            ArgumentNullException.ThrowIfNull(builder);
            // Add configuration
            builder.AddConfiguration();
            // Register service to write log entries to the storage
            _ = builder.Services.AddSingleton(serviceProvider => new FileLoggerWriter(directory, filePrefix, fileSizeLimit, fileCountLimit, strategy, encoding, allowSynchronousContinuations));
            // Register logger provider
            builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<ILoggerProvider, SanlogLoggerProvider>(
                serviceProvider => new SanlogLoggerProvider(
                    writer: serviceProvider.GetRequiredService<FileLoggerWriter>(),
                    optionsMonitor: serviceProvider.GetRequiredService<IOptionsMonitor<SanlogLoggerOptions>>())));
            LoggerProviderOptions.RegisterProviderOptions<SanlogLoggerOptions, SanlogLoggerProvider>(builder.Services); // IL3026 + IL3050
            // Configure logging options
            if (loggingConfigure is not null) _ = builder.Services.Configure(loggingConfigure);
            return builder;
        }
    }
}