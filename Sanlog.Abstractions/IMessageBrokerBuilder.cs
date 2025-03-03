using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;

namespace Sanlog.Abstractions
{
    /// <summary>
    /// Configures a message broker.
    /// </summary>
    public interface IMessageBrokerBuilder
    {
        /// <summary>
        /// Gets the service collection into which the handlers instances are registered.
        /// </summary>
        IServiceCollection Services { get; }

        /// <summary>
        /// Sets the handler to use for <typeparamref name="TMessage"/>.
        /// </summary>
        /// <typeparam name="TMessage">The message type.</typeparam>
        /// <typeparam name="THandler">The handler type.</typeparam>
        /// <returns>The current instance of the builder.</returns>
        IMessageBrokerBuilder SetHandler<TMessage, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] THandler>() where THandler : class, IMessageHandler;
        /// <summary>
        /// Sets the handler to use when processing data for which no specific handler has been registered.
        /// </summary>
        /// <typeparam name="THandler">The handler type.</typeparam>
        /// <returns>The current instance of the builder.</returns>
        IMessageBrokerBuilder SetFallbackHandler<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] THandler>() where THandler : class, IMessageHandler;
    }
}