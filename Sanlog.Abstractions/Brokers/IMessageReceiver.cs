using System;
using System.Threading;
using System.Threading.Tasks;

namespace Sanlog.Brokers
{
    /// <summary>
    /// Provides a mechanism to deliver messages to handlers.
    /// </summary>
    public interface IMessageReceiver
    {
        /// <summary>
        /// Sends a message to service that handles the specified <typeparamref name="TMessage"/>.
        /// </summary>
        /// <typeparam name="TMessage">The type of the <paramref name="message"/>.</typeparam>
        /// <param name="message">The message to handle.</param>
        /// <returns><see langword="true"/> if the message is accepted for handling; otherwise <see langword="false"/>.</returns>
        bool SendMessage<TMessage>(TMessage? message);
        /// <summary>
        /// Sends a message to service that handles the specified <paramref name="serviceType"/>.
        /// </summary>
        /// <typeparam name="TMessage">The type of the <paramref name="message"/>.</typeparam>
        /// <param name="serviceType">The service type that handles the specified <paramref name="message"/>.</param>
        /// <param name="message">The message to handle.</param>
        /// <returns><see langword="true"/> if the message is accepted for handling; otherwise <see langword="false"/>.</returns>
        bool SendMessage<TMessage>(Type serviceType, TMessage? message);
        /// <summary>
        /// Asynchronously sends a message to service that handles the specified <typeparamref name="TMessage"/>.
        /// </summary>
        /// <typeparam name="TMessage">The type of the <paramref name="message"/>.</typeparam>
        /// <param name="message">The message to handle.</param>
        /// <param name="cancellationToken">A cancellation token used to cancel the operation.</param>
        /// <returns><see langword="true"/> if the message is accepted for handling; otherwise <see langword="false"/>.</returns>
        ValueTask<bool> SendMessageAsync<TMessage>(TMessage? message, CancellationToken cancellationToken);
        /// <summary>
        /// Asynchronously sends a message to service that handles the specified <paramref name="serviceType"/>.
        /// </summary>
        /// <typeparam name="TMessage">The type of the <paramref name="message"/>.</typeparam>
        /// <param name="serviceType">The service type that handles the specified <paramref name="message"/>.</param>
        /// <param name="message">The message to handle.</param>
        /// <param name="cancellationToken">A cancellation token used to cancel the operation.</param>
        /// <returns><see langword="true"/> if the message is accepted for handling; otherwise <see langword="false"/>.</returns>
        ValueTask<bool> SendMessageAsync<TMessage>(Type serviceType, TMessage? message, CancellationToken cancellationToken);
    }
}