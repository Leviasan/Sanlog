using System;
using System.Threading.Channels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Sanlog.Abstractions
{
    /// <summary>
    /// Extension methods for registering message broker in an <see cref="IServiceCollection"/>.
    /// </summary>
    public static class MessageBrokerServiceExtensions
    {
        /// <summary>
        /// Adds message broker service to the specified <see cref="IServiceCollection"/>.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
        /// <param name="configure">A callback to configure the <see cref="IMessageBrokerBuilder"/>.</param>
        /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
        /// <exception cref="ArgumentNullException">One of the parameters is <see langword="null"/>.</exception>
        public static IServiceCollection AddMessageBroker(this IServiceCollection services, Action<IMessageBrokerBuilder> configure)
        {
            ArgumentNullException.ThrowIfNull(services);
            ArgumentNullException.ThrowIfNull(configure);

            services.TryAddSingleton(Channel.CreateUnbounded<MessageContext>(new UnboundedChannelOptions { SingleReader = true }));

            services
                .AddOptions<MessageBrokerOptions>()
                .Services
                .AddHostedService<MessageBroker>()
                .TryAddSingleton<IMessageBrokerReceiver, MessageBrokerReceiver>();

            configure.Invoke(new MessageBrokerBuilder(services));

            return services;
        }
    }
}