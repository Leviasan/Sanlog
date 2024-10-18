using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Configuration;

namespace Sanlog.EFCore
{
    public static class ILoggingBuilderExtensions
    {
        /// <summary>
        /// Registers <see cref="SanlogLoggerProvider"/> with store as database.
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="appId"></param>
        /// <param name="tenantId"></param>
        /// <param name="contextConfigure"></param>
        /// <param name="loggingConfigure"></param>
        /// <param name="allowSynchronousContinuations"></param>
        /// <returns></returns>
        [RequiresDynamicCode("Binding TOptions to configuration values may require generating dynamic code at runtime.")]
        [RequiresUnreferencedCode("EF Core isn't fully compatible with trimming, and running the application may generate unexpected runtime failures." +
            " Some specific coding pattern are usually required to make trimming work properly, see https://aka.ms/efcore-docs-trimming for more details." +
            " TOptions's dependent types may have their members trimmed. Ensure all required members are preserved.")]
        public static ILoggingBuilder AddSanlogEFCore(this ILoggingBuilder builder,
            Guid appId, Guid tenantId,
            Action<DbContextOptionsBuilder> contextConfigure,
            Action<SanlogLoggerOptions>? loggingConfigure,
            bool allowSynchronousContinuations)
        {
            ArgumentNullException.ThrowIfNull(builder);
            ArgumentNullException.ThrowIfNull(contextConfigure);
            builder.AddConfiguration();
            builder.Services
                .AddDbContextFactory<SanlogDbContext>(
                    optionsAction: contextConfigure,
                    lifetime: ServiceLifetime.Scoped) // IL2026
                .AddSingleton(serviceProvider => new EFCoreProcessor(
                    contextFactory: serviceProvider.GetRequiredService<IDbContextFactory<SanlogDbContext>>(),
                    allowSynchronousContinuations: allowSynchronousContinuations))
                .AddSingleton(new TenantService(appId, tenantId))
                .TryAddEnumerable(ServiceDescriptor.Singleton<ILoggerProvider, SanlogLoggerProvider>());
            LoggerProviderOptions.RegisterProviderOptions<SanlogLoggerOptions, SanlogLoggerProvider>(builder.Services); // IL2026 + IL3050
            if (loggingConfigure is not null) _ = builder.Services.Configure(loggingConfigure);
            return builder;
        }
    }
}