using System;
using Microsoft.Extensions.DependencyInjection;

namespace Sanlog.Formatters
{
    /// <summary>
    /// Extension methods for registering message broker in an <see cref="IServiceCollection"/>.
    /// </summary>
    internal static class IServiceCollectionExtensions
    {
        /// <summary>
        /// Adds a <see cref="FormattedLogValuesFormatter"/> service to the specified <see cref="IServiceCollection"/>.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
        /// <param name="configureOptions">A callback to configure the <see cref="FormattedLogValuesFormatter"/>.</param>
        /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
        public static IServiceCollection AddFormattedLogValuesFormatter(this IServiceCollection services, Action<FormattedLogValuesFormatterOptions>? configureOptions = null)
        {
            var options = new FormattedLogValuesFormatterOptions(FormattedLogValuesFormatterOptions.Default);
            configureOptions?.Invoke(options);
            _ = options.MakeReadOnly();

            return services
                .Configure(configureOptions ?? (_ => { }))
                .AddSingleton<FormattedLogValuesFormatter>();
        }
    }
}