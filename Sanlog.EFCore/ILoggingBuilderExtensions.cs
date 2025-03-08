using System;
using System.Reflection.Metadata;
using System.Threading.Channels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Configuration;
using Sanlog.Brokers;
using Sanlog.Formatters;
using Sanlog.Models;

namespace Sanlog
{
    /// <summary>
    /// Extension methods for registering logger in an <see cref="ILoggingBuilder"/>.
    /// </summary>
    public static class ILoggingBuilderExtensions
    {
        /// <summary>
        /// Adds Sanlog logger to the factory.
        /// </summary>
        /// <param name="builder">The <see cref="ILoggingBuilder"/> to use.</param>
        /// <param name="contextConfigure">A callback to configure the <see cref="DbContextOptionsBuilder"/>.</param>
        /// <param name="loggingConfigure">A callback to configure the <see cref="SanlogLoggerOptions"/>.</param>
        /// <param name="configureFormatter">A callback to configure the <see cref="FormattedLogValuesFormatter"/>.</param>
        /// <returns>The <see cref="ILoggingBuilder"/> to use.</returns>
        /// <exception cref="ArgumentNullException">The <paramref name="builder"/> or <paramref name="contextConfigure"/> is <see langword="null"/>.</exception>
        public static ILoggingBuilder AddSanlogLogging(
            this ILoggingBuilder builder,
            Action<DbContextOptionsBuilder> contextConfigure,
            Action<SanlogLoggerOptions>? loggingConfigure = null,
            Action<FormattedLogValuesFormatterOptions>? configureFormatter = null)
        {
            ArgumentNullException.ThrowIfNull(builder);
            ArgumentNullException.ThrowIfNull(contextConfigure);

            builder.AddConfiguration();
            builder.Services
                .AddSanlogInfrastructure(
                    configureBroker: builder => builder.SetHandler<EntityFrameworkCoreSanlogLoggerProvider, EntityFrameworkCoreMessageHandler>(),
                    configureFormatter: configureFormatter)
                .AddPooledDbContextFactory<SanlogDbContext>(contextConfigure)
                .TryAddEnumerable(ServiceDescriptor.Singleton<ILoggerProvider, EntityFrameworkCoreSanlogLoggerProvider>());
            LoggerProviderOptions.RegisterProviderOptions<SanlogLoggerOptions, EntityFrameworkCoreSanlogLoggerProvider>(builder.Services);
            if (loggingConfigure is not null)
                _ = builder.Services.Configure(loggingConfigure);

            return builder;
        }
    }
}