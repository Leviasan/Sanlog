using System;
using System.Threading.Channels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Sanlog.Abstractions
{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    public static class IServiceCollectionExtensions
    {
        public static IServiceCollection AddMessageBroker(this IServiceCollection services, Action<IMessageBrokerBuilder> configure)
        {
            ArgumentNullException.ThrowIfNull(services);
            ArgumentNullException.ThrowIfNull(configure);

            services.TryAddSingleton(sp => Channel.CreateUnbounded<MessageContext>(new UnboundedChannelOptions { SingleReader = true }));

            services
                .AddOptions<MessageBrokerOptions>()
                .Services
                .AddHostedService<MessageBrokerHostedService>()
                .TryAddSingleton<IMessageBroker, MessageBroker>();

            configure.Invoke(new MessageBrokerBuilder(services));

            return services;
        }
    }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
}