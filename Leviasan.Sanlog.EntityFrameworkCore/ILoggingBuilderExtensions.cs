using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Configuration;
using Microsoft.Extensions.Options;

namespace Leviasan.Sanlog.EntityFrameworkCore
{
    /// <summary>
    /// Provides the <see cref="ILoggingBuilder"/> extension methods.
    /// </summary>
    public static class ILoggingBuilderExtensions
    {
        /// <summary>
        /// Adds Sanlog logging services with EntityFrameworkCore storage.
        /// </summary>
        /// <param name="builder">The logging builder.</param>
        /// <param name="contextConfigure">The configure options for context.</param>
        /// <param name="loggingConfigure">The configure options for logging.</param>
        /// <returns>The logging builder.</returns>
        /// <exception cref="ArgumentNullException">The <paramref name="builder"/> or <paramref name="contextConfigure"/> is <see langword="null"/>.</exception>
        public static ILoggingBuilder AddSanlogService(this ILoggingBuilder builder, Action<DbContextOptionsBuilder> contextConfigure, Action<SanlogLoggerOptions>? loggingConfigure = default)
        {
            ArgumentNullException.ThrowIfNull(builder);
            ArgumentNullException.ThrowIfNull(contextConfigure);

            // Add configuration
            builder.AddConfiguration();
            // Register SanlogDbContext factory
            _ = builder.Services.AddDbContextFactory<SanlogDbContext>(contextConfigure);
            // Register service to write log entries to storage
            _ = builder.Services.AddSingleton<SanlogDbContextWriter>();
            // Register logger provider
            builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<ILoggerProvider, SanlogLoggerProvider>(
                serviceProvider => new SanlogLoggerProvider(
                    eventWriter: serviceProvider.GetRequiredService<SanlogDbContextWriter>(),
                    optionsMonitor: serviceProvider.GetRequiredService<IOptionsMonitor<SanlogLoggerOptions>>())));
            LoggerProviderOptions.RegisterProviderOptions<SanlogLoggerOptions, SanlogLoggerProvider>(builder.Services);
            // Configure logging options
            if (loggingConfigure is not null) _ = builder.Services.Configure(loggingConfigure);
            return builder;
        }
        /// <summary>
        /// Adds Sanlog logging services with EntityFrameworkCore storage as a hosted service.
        /// </summary>
        /// <param name="builder">The logging builder.</param>
        /// <param name="contextConfigure">The configure options for context.</param>
        /// <param name="loggingConfigure">The configure options for logging.</param>
        /// <returns>The logging builder.</returns>
        /// <exception cref="ArgumentNullException">The <paramref name="builder"/> or <paramref name="contextConfigure"/> is <see langword="null"/>.</exception>
        public static ILoggingBuilder AddHostedSanlogService(this ILoggingBuilder builder, Action<DbContextOptionsBuilder> contextConfigure, Action<SanlogLoggerOptions>? loggingConfigure = default)
        {
            ArgumentNullException.ThrowIfNull(builder);
            ArgumentNullException.ThrowIfNull(contextConfigure);

            // Add configuration
            builder.AddConfiguration();
            // Register clipboard between loggers and storage
            _ = builder.Services.AddKeyedSingleton<UnboundedSingleConsumerChannel>(nameof(HostedSanlogDbContextWriter));
            // Register logger provider
            builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<ILoggerProvider, SanlogLoggerProvider>(
                serviceProvider => new SanlogLoggerProvider(
                    eventWriter: serviceProvider.GetRequiredKeyedService<UnboundedSingleConsumerChannel>(nameof(HostedSanlogDbContextWriter)),
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
    }
}