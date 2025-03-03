using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Sanlog.Extensions.Hosting.Broker;
using Sanlog.Extensions.Hosting.Brokers;

namespace Sanlog
{
    internal sealed class MessageBrokerBuilder(IServiceCollection services) : IMessageBrokerBuilder
    {
        public IServiceCollection Services { get; } = services;

        public IMessageBrokerBuilder SetHandler<TMessage, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] THandler>() where THandler : class, IMessageHandler
        {
            Services.TryAddEnumerable(ServiceDescriptor.Singleton<IMessageHandler, THandler>());
            _ = Services.Configure<MessageBrokerOptions>(options => options.Handlers[typeof(TMessage)] = typeof(THandler));
            return this;
        }
        public IMessageBrokerBuilder SetFallbackHandler<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] THandler>() where THandler : class, IMessageHandler
        {
            Services.TryAddEnumerable(ServiceDescriptor.Singleton<IMessageHandler, THandler>());
            _ = Services.Configure<MessageBrokerOptions>(options => options.FallbackHandler = typeof(THandler));
            return this;
        }
    }
}