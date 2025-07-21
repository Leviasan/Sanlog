using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Configuration;
using Microsoft.Extensions.Options;
using Sanlog.Brokers;

namespace Sanlog.EntityFrameworkCore
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
        /// <param name="loggingConfigure">A callback to configure the <see cref="Sanlog.SanlogLoggerProvider"/>.</param>
        /// <param name="contextConfigure">A callback to configure the <see cref="DbContextOptionsBuilder"/>.</param>
        /// <returns>The <see cref="ILoggingBuilder"/> to use.</returns>
        /// <exception cref="ArgumentNullException">The <paramref name="builder"/> or <paramref name="contextConfigure"/> is <see langword="null"/>.</exception>
        public static ILoggingBuilder AddSanlogEntityFrameworkCore(
            this ILoggingBuilder builder,
            Action<DbContextOptionsBuilder> contextConfigure,
            Action<SanlogLoggerOptions>? loggingConfigure = null)
        {
            ArgumentNullException.ThrowIfNull(builder);
            ArgumentNullException.ThrowIfNull(contextConfigure);

            builder.AddConfiguration();
            builder.Services
                .AddMessageBroker(builder => builder.SetHandler<SanlogLoggerProvider, LoggingEntryMessageHandler>())
                .AddPooledDbContextFactory<SanlogDbContext>((sp, x) =>
                {
                    SanlogLoggerOptions opts = sp.GetRequiredService<IOptions<SanlogLoggerOptions>>().Value;
                    _ = x.UseLoggerFactory(NullLoggerFactory.Instance);
                    _ = x.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTrackingWithIdentityResolution);
                    _ = x.AddInterceptors(new SanlogDbContext.TenantValidatorInterceptor(opts.AppId, opts.TenantId));
                    contextConfigure.Invoke(x);
                })
                .TryAddEnumerable(ServiceDescriptor.Singleton<ILoggerProvider, SanlogLoggerProvider>());
            LoggerProviderOptions.RegisterProviderOptions<SanlogLoggerOptions, SanlogLoggerProvider>(builder.Services);
            if (loggingConfigure is not null)
                _ = builder.Services.Configure(loggingConfigure);
            _ = builder.Services.PostConfigure<SanlogLoggerOptions>(options => options.FormattedOptions.MakeReadOnly());
            return builder;
        }
    }
}