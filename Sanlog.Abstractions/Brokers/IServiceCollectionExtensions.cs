using System;
using System.Threading.Channels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Sanlog.Brokers
{
    /// <summary>
    /// Extension methods for registering message broker in an <see cref="IServiceCollection"/>.
    /// </summary>
    internal static class IServiceCollectionExtensions
    {
        /// <summary>
        /// Adds message broker service based on unbounded channel to the specified <see cref="IServiceCollection"/>.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
        /// <param name="configure">A callback to configure the <see cref="IMessageBrokerBuilder"/>.</param>
        /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
        public static IServiceCollection AddMessageBroker(
            this IServiceCollection services,
            Action<IMessageBrokerBuilder> configure)
        {
            services.TryAddSingleton(
                Channel.CreateUnbounded<MessageContext>(
                    new UnboundedChannelOptions { SingleReader = true }));
            services
                .AddOptions<MessageBrokerOptions>()
                .Services
                .AddHostedService<MessageBroker>()
                .TryAddSingleton<IMessageReceiver, MessageReceiver>();
            configure.Invoke(new MessageBrokerBuilder(services));
            return services;
        }
        /// <summary>
        /// Adds message broker service based on bounded channel to the specified <see cref="IServiceCollection"/>.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
        /// <param name="configure">A callback to configure the <see cref="IMessageBrokerBuilder"/>.</param>
        /// <param name="capacity">The maximum number of items the bounded channel may store.</param>
        /// <param name="fullMode">The behavior incurred by write operations when the channel is full.</param>
        /// <param name="itemDropped">Delegate that will be called when item is being dropped from channel.</param>
        /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
        public static IServiceCollection AddMessageBroker(
            this IServiceCollection services,
            Action<IMessageBrokerBuilder> configure,
            int capacity,
            BoundedChannelFullMode fullMode,
            Action<object?>? itemDropped)
        {
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
                .AddHostedService<MessageBroker>()
                .TryAddSingleton<IMessageReceiver, MessageReceiver>();
            configure.Invoke(new MessageBrokerBuilder(services));
            return services;
        }
    }
}