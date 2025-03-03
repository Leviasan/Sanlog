using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Sanlog.Extensions.Hosting.Broker;
using Sanlog.Extensions.Hosting.Brokers;

namespace Sanlog
{
    internal static class MessageBrokerServiceCollectionExtensions
    {
        public static IServiceCollection AddMessageBroker(this IServiceCollection services, Action<IMessageBrokerBuilder> configure)
        {
            ArgumentNullException.ThrowIfNull(services);
            ArgumentNullException.ThrowIfNull(configure);

            services
                .AddOptions<MessageBrokerOptions>()
                .Services
                .TryAddSingleton<IMessageBroker, MessageBroker>();
            configure.Invoke(new MessageBrokerBuilder(services));
            return services;
        }
    }
}