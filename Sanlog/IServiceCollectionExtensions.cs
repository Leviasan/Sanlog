using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Compliance.Classification;
using Sanlog.Compliance.Redaction;
using System;
using Sanlog.Compliance.Classification;

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
        /// <returns></returns>
        public static IServiceCollection AddCompliance(this IServiceCollection services)
        {
            ArgumentNullException.ThrowIfNull(services);
            return services.AddRedaction(redactionBuilder => redactionBuilder.SetRedactor<SensitiveRedactor>(new DataClassificationSet(SanlogTaxonomy.Sensitive)));
        }
    }
}