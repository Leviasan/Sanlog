using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Configuration;
using Microsoft.Extensions.Options;
using Sanlog;
using Sanlog.EFCore;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Microsoft.Extensions.Logging
#pragma warning restore IDE0130 // Namespace does not match folder structure
{
    /// <summary>
    /// Provides extension methods for the <see cref="ILoggingBuilder"/> .
    /// </summary>
    public static class ILoggingBuilderExtensions
    {
        /// <summary>
        /// Adds EFCore logger named 'Sanlog' to the factory.
        /// </summary>
        /// <param name="builder">The <see cref="ILoggingBuilder"/> to use.</param>
        /// <param name="contextConfigure">A delegate to configure the <see cref="DbContextOptionsBuilder"/>.</param>
        /// <param name="loggingConfigure">A delegate to configure the <see cref="SanlogLoggerOptions"/>.</param>
        /// <param name="sync"><see langword="true"/> if write operations performed on logger should be synchronously; <see langword="false"/> if ones should be invoked asynchronously.</param>
        /// <returns>The <see cref="ILoggingBuilder"/> to use.</returns>
        /// <exception cref="ArgumentNullException">The <paramref name="builder"/> or <paramref name="contextConfigure"/> is <see langword="null"/>.</exception>
        [RequiresDynamicCode("Binding TOptions to configuration values may require generating dynamic code at runtime.")]
        [RequiresUnreferencedCode("EF Core isn't fully compatible with trimming, and running the application may generate unexpected runtime failures." +
            " Some specific coding pattern are usually required to make trimming work properly, see https://aka.ms/efcore-docs-trimming for more details." +
            " TOptions's dependent types may have their members trimmed. Ensure all required members are preserved.")]
        public static ILoggingBuilder AddSanlogLogging(this ILoggingBuilder builder, Action<DbContextOptionsBuilder> contextConfigure, Action<SanlogLoggerOptions>? loggingConfigure = null, bool sync = false)
        {
            ArgumentNullException.ThrowIfNull(builder);
            ArgumentNullException.ThrowIfNull(contextConfigure);

            builder.AddConfiguration();
            builder.Services
                .AddDbContextFactory<SanlogDbContext>(
                    optionsAction: contextConfigure,
                    lifetime: ServiceLifetime.Singleton) // IL2026
                .AddSingleton(serviceProvider => new EFCoreLoggingWriter(
                    contextFactory: serviceProvider.GetRequiredService<IDbContextFactory<SanlogDbContext>>(),
                    allowSynchronousContinuations: sync))
                .TryAddEnumerable(ServiceDescriptor.Singleton<ILoggerProvider, SanlogLoggerProvider>(serviceProvider => new SanlogLoggerProvider(
                    writer: serviceProvider.GetRequiredService<EFCoreLoggingWriter>(),
                    optionsMonitor: serviceProvider.GetRequiredService<IOptionsMonitor<SanlogLoggerOptions>>())));
            LoggerProviderOptions.RegisterProviderOptions<SanlogLoggerOptions, SanlogLoggerProvider>(builder.Services); // IL2026 + IL3050
            if (loggingConfigure is not null)
                _ = builder.Services.Configure(loggingConfigure);
            return builder;
        }
    }
}