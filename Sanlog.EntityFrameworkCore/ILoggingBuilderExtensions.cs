using System;
using System.Threading.Channels;
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
        /// Adds Sanlog logger to the factory that uses database as storage through EntityFrameworkCore and configures a message broker service based on unbounded channel.
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
        /// <summary>
        /// Adds Sanlog logger to the factory that uses database as storage through EntityFrameworkCore and configures a message broker service based on bounded channel.
        /// </summary>
        /// <param name="builder">The <see cref="ILoggingBuilder"/> to use.</param>
        /// <param name="contextConfigure">A callback to configure the <see cref="DbContextOptionsBuilder"/>.</param>
        /// <param name="capacity">The maximum number of items the bounded channel may store.</param>
        /// <param name="fullMode">The behavior incurred by write operations when the channel is full.</param>
        /// <param name="itemDropped">Delegate that will be called when item is being dropped from channel.</param>
        /// <param name="loggingConfigure">A callback to configure the <see cref="Sanlog.SanlogLoggerProvider"/>.</param>
        /// <returns>The <see cref="ILoggingBuilder"/> to use.</returns>
        /// <exception cref="ArgumentNullException">The <paramref name="builder"/> or <paramref name="contextConfigure"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentOutOfRangeException">The <paramref name="capacity"/> is less then 1. -or- Passed an invalid <paramref name="fullMode"/>.</exception>
        public static ILoggingBuilder AddSanlogEntityFrameworkCore(
            this ILoggingBuilder builder,
            Action<DbContextOptionsBuilder> contextConfigure,
            int capacity,
            BoundedChannelFullMode fullMode,
            Action<object?>? itemDropped = null,
            Action<SanlogLoggerOptions>? loggingConfigure = null)
        {
            ArgumentNullException.ThrowIfNull(builder);
            ArgumentNullException.ThrowIfNull(contextConfigure);

            builder.AddConfiguration();
            builder.Services
                .AddMessageBroker(builder => builder.SetHandler<SanlogLoggerProvider, LoggingEntryMessageHandler>(), capacity, fullMode, itemDropped)
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