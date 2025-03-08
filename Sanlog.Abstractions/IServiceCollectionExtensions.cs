using System;
using System.Threading.Channels;
using Microsoft.Extensions.DependencyInjection;
using Sanlog.Brokers;
using Sanlog.Formatters;

namespace Sanlog
{
    /// <summary>
    /// Extension methods for registering logger infrastructure in an <see cref="IServiceCollection"/>.
    /// </summary>
    public static class IServiceCollectionExtensions
    {
        /// <summary>
        /// Adds message broker service based on unbounded channel and <see cref="FormattedLogValuesFormatter"/> service to the specified <see cref="IServiceCollection"/>.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
        /// <param name="configureBroker">A callback to configure the <see cref="IMessageBrokerBuilder"/>.</param>
        /// <param name="configureFormatter">A callback to configure the <see cref="FormattedLogValuesFormatter"/>.</param>
        /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
        /// <exception cref="ArgumentNullException">The <paramref name="services"/> or <paramref name="configureBroker"/> is <see langword="null"/>.</exception>
        public static IServiceCollection AddSanlogInfrastructure(
            this IServiceCollection services,
            Action<IMessageBrokerBuilder> configureBroker,
            Action<FormattedLogValuesFormatterOptions>? configureFormatter = null)
        {
            ArgumentNullException.ThrowIfNull(services);
            ArgumentNullException.ThrowIfNull(configureBroker);

            return services
                .AddMessageBroker(configureBroker)
                .AddFormattedLogValuesFormatter(configureFormatter);
        }
        /// <summary>
        /// Adds message broker service based on bounded channel and <see cref="FormattedLogValuesFormatter"/> service to the specified <see cref="IServiceCollection"/>.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
        /// <param name="configureBroker">A callback to configure the <see cref="IMessageBrokerBuilder"/>.</param>
        /// <param name="capacity">The maximum number of items the bounded channel may store.</param>
        /// <param name="fullMode">The behavior incurred by write operations when the channel is full.</param>
        /// <param name="itemDropped">Delegate that will be called when item is being dropped from channel.</param>
        /// <param name="configureFormatter">A callback to configure the <see cref="FormattedLogValuesFormatter"/>.</param>
        /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
        /// <exception cref="ArgumentNullException">The <paramref name="services"/> or <paramref name="configureBroker"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentOutOfRangeException">The <paramref name="capacity"/> is less then 1. -or- Passed an invalid <paramref name="fullMode"/>.</exception>
        public static IServiceCollection AddSanlogInfrastructure(
            this IServiceCollection services,
            Action<IMessageBrokerBuilder> configureBroker,
            int capacity,
            BoundedChannelFullMode fullMode,
            Action<object?>? itemDropped = null,
            Action<FormattedLogValuesFormatterOptions>? configureFormatter = null)
        {
            ArgumentNullException.ThrowIfNull(services);
            ArgumentNullException.ThrowIfNull(configureBroker);
            ArgumentOutOfRangeException.ThrowIfLessThan(capacity, 1);
            ArgumentOutOfRangeException.ThrowIfLessThan((int)fullMode, 0);
            ArgumentOutOfRangeException.ThrowIfGreaterThan((int)fullMode, 3);

            return services
                .AddMessageBroker(configureBroker, capacity, fullMode, itemDropped)
                .AddFormattedLogValuesFormatter(configureFormatter);
        }
    }
}