using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Configuration;
using Microsoft.Extensions.Options;

namespace Leviasan.Sanlog.EntityFrameworkCore
{
    public static class ILoggingBuilderExtensions
    {
        [RequiresDynamicCode("Binding TOptions to configuration values may require generating dynamic code at runtime.")]
        [RequiresUnreferencedCode("EF Core isn't fully compatible with trimming, and running the application may generate unexpected runtime failures." +
            " Some specific coding pattern are usually required to make trimming work properly, see https://aka.ms/efcore-docs-trimming for more details." +
            " TOptions's dependent types may have their members trimmed. Ensure all required members are preserved.")]
        public static ILoggingBuilder AddSanlogLoggerEFCore(this ILoggingBuilder builder,
            Action<DbContextOptionsBuilder> contextConfigure,
            Action<SanlogLoggerOptions>? loggingConfigure = default,
            bool allowSynchronousContinuations = false)
        {
            ArgumentNullException.ThrowIfNull(builder);
            ArgumentNullException.ThrowIfNull(contextConfigure);

            builder.AddConfiguration();
            builder.Services
                .AddDbContextFactory<SanlogDbContext>(contextConfigure) // IL2026
                .AddSingleton(serviceProvider => new EFCoreProcessor(
                    contextFactory: serviceProvider.GetRequiredService<IDbContextFactory<SanlogDbContext>>(),
                    allowSynchronousContinuations: allowSynchronousContinuations))
                .TryAddEnumerable(ServiceDescriptor.Singleton<ILoggerProvider, SanlogLoggerProvider>(
                    serviceProvider => new SanlogLoggerProvider(
                        serviceKey: typeof(EFCoreProcessor),
                        writer: serviceProvider.GetRequiredService<EFCoreProcessor>(),
                        optionsMonitor: serviceProvider.GetRequiredService<IOptionsMonitor<SanlogLoggerOptions>>())));
            LoggerProviderOptions.RegisterProviderOptions<SanlogLoggerOptions, SanlogLoggerProvider>(builder.Services); // IL2026 + IL3050
            if (loggingConfigure is not null) _ = builder.Services.Configure(loggingConfigure);
            return builder;
        }
    }
}