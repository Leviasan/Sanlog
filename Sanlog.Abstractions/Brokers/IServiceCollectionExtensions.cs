using System;
using System.Threading.Channels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace Sanlog.Brokers
{
    /// <summary>
    /// Extension methods for registering message broker in an <see cref="IServiceCollection"/>.
    /// </summary>
    public static class IServiceCollectionExtensions
    {
        /// <summary>
        /// Registers logger infrastructure with configure a message broker service based on unbounded channel to the specified <see cref="IServiceCollection"/>.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
        /// <param name="configureBroker">A callback to configure the <see cref="IMessageBrokerBuilder"/>.</param>
        /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
        /// <exception cref="ArgumentNullException">The <paramref name="services"/> or <paramref name="configureBroker"/> is <see langword="null"/>.</exception>
        public static IServiceCollection AddMessageBroker(this IServiceCollection services, Action<IMessageBrokerBuilder> configureBroker)
        {
            ArgumentNullException.ThrowIfNull(services);
            ArgumentNullException.ThrowIfNull(configureBroker);

            services.TryAddSingleton(
                Channel.CreateUnbounded<MessageContext>(
                    new UnboundedChannelOptions { SingleReader = true }));
            services
                .AddOptions<MessageBrokerOptions>()
                .Services
                .TryAddSingleton<MessageBroker>();
            services
                .AddHostedService(sp => sp.GetRequiredService<MessageBroker>())
                .TryAddSingleton<IMessageReceiver, MessageReceiver>();
            configureBroker.Invoke(new MessageBrokerBuilder(services));
            return services;
        }
        /// <summary>
        /// Registers logger infrastructure with configure a message broker service based on bounded channel to the specified <see cref="IServiceCollection"/>.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
        /// <param name="configureBroker">A callback to configure the <see cref="IMessageBrokerBuilder"/>.</param>
        /// <param name="capacity">The maximum number of items the bounded channel may store.</param>
        /// <param name="fullMode">The behavior incurred by write operations when the channel is full.</param>
        /// <param name="itemDropped">Delegate that will be called when item is being dropped from channel.</param>
        /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
        /// <exception cref="ArgumentNullException">The <paramref name="services"/> or <paramref name="configureBroker"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentOutOfRangeException">The <paramref name="capacity"/> is less then 1. -or- Passed an invalid <paramref name="fullMode"/>.</exception>
        public static IServiceCollection AddMessageBroker(
            this IServiceCollection services,
            Action<IMessageBrokerBuilder> configureBroker,
            int capacity,
            BoundedChannelFullMode fullMode,
            Action<object?>? itemDropped = null)
        {
            ArgumentNullException.ThrowIfNull(services);
            ArgumentNullException.ThrowIfNull(configureBroker);
            ArgumentOutOfRangeException.ThrowIfLessThan(capacity, 1);
            ArgumentOutOfRangeException.ThrowIfLessThan((int)fullMode, 0);
            ArgumentOutOfRangeException.ThrowIfGreaterThan((int)fullMode, 3);

            services.TryAddSingleton(
                Channel.CreateBounded<MessageContext>(
                    options: new BoundedChannelOptions(capacity)
                    {
                        FullMode = fullMode,
                        SingleReader = true
                    },
                    itemDropped: ctx => itemDropped?.Invoke(ctx.Message)));
            services
                .AddOptions<MessageBrokerOptions>()
                .Services
                .TryAddSingleton<MessageBroker>();
            services
                .AddHostedService(sp => sp.GetRequiredService<MessageBroker>())
                .TryAddSingleton<IMessageReceiver, MessageReceiver>();
            configureBroker.Invoke(new MessageBrokerBuilder(services));
            return services;
        }
    }
}