using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Sanlog.Brokers
{
    /// <summary>
    /// Represents the builder of the <see cref="MessageBroker"/>.
    /// </summary>
    /// <param name="services">The service collection.</param>
    internal sealed class MessageBrokerBuilder(IServiceCollection services) : IMessageBrokerBuilder
    {
        /// <inheritdoc/>
        public IServiceCollection Services { get; } = services;

        /// <inheritdoc/>
        public IMessageBrokerBuilder SetHandler<TMessage, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] THandler>() where THandler : class, IMessageHandler
        {
            Services.TryAddEnumerable(ServiceDescriptor.Singleton<IMessageHandler, THandler>());
            _ = Services.Configure<MessageBrokerOptions>(options => options.Handlers[typeof(TMessage)] = typeof(THandler));
            return this;
        }
        /// <inheritdoc/>
        public IMessageBrokerBuilder SetFallbackHandler<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] THandler>() where THandler : class, IMessageHandler
        {
            Services.TryAddEnumerable(ServiceDescriptor.Singleton<IMessageHandler, THandler>());
            _ = Services.Configure<MessageBrokerOptions>(options => options.FallbackHandler = typeof(THandler));
            return this;
        }
    }
}