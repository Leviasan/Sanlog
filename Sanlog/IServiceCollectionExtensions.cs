using Microsoft.Extensions.DependencyInjection;
using Sanlog.Compliance.Redaction;
using System;
using Sanlog.Compliance.Classification;
using Microsoft.Extensions.Configuration;
using System.Diagnostics.CodeAnalysis;

namespace Sanlog
{
    /// <summary>
    /// 
    /// </summary>
    public static class IServiceCollectionExtensions
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="services"></param>
        /// <param name="configuration"></param>
        /// <returns></returns>
        [RequiresDynamicCode("Binding strongly typed objects to configuration values may require generating dynamic code at runtime.")]
        [RequiresUnreferencedCode("TOptions's dependent types may have their members trimmed. Ensure all required members are preserved.")]
        public static IServiceCollection AddCompliance(this IServiceCollection services, IConfiguration configuration)
        {
            ArgumentNullException.ThrowIfNull(services);
            return services
                .AddRedaction(builder => builder.SetRedactor<SensitiveRedactor>(SanlogTaxonomy.Sensitive))
                .Configure<SanlogLoggerOptions>(configuration) // IL2026 + IL3050
                .AddSingleton<IMessageBroker, MessageBroker>();
        }
    }
}