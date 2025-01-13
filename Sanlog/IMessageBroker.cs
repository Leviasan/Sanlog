using System;
using System.Threading.Tasks;
using System.Threading;

namespace Sanlog
{
    /// <summary>
    /// Provider a mechanism to send/deliver messages to handlers.
    /// </summary>
    public interface IMessageBroker
    {
        /// <summary>
        /// Registers a service that handles messages of the <paramref name="serviceType"/>.
        /// </summary>
        /// <param name="serviceType">The message type to handle.</param>
        /// <param name="handler">The handler of the message.</param>
        /// <returns><see langword="true"/> if the handler is registered; <see langword="false"/> if the handler is already present.</returns>
        bool Subscribe(Type serviceType, IMessageHandler handler);
        /// <summary>
        /// Unsubscribes a service from listen to messages of the <paramref name="serviceType"/>.
        /// </summary>
        /// <param name="serviceType">The message type to handle.</param>
        /// <param name="handler">The handler of the message.</param>
        /// <returns><see langword="true"/> if operation to remove handler is successful; <see langword="false"/> if the handler is not found.</returns>
        bool Unsubscribe(Type serviceType, IMessageHandler handler);
        /// <summary>
        /// Unsubscribes all services from listen to messages of the <paramref name="serviceType"/>.
        /// </summary>
        /// <param name="serviceType">The message type to handle.</param>
        /// <returns><see langword="true"/> if all handlers are unsubscribe; otherwise <see langword="false"/>.</returns>
        bool Unsubscribe(Type serviceType);
        /// <summary>
        /// Sends a message to handle.
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
        /// Asynchronously sends a message to handle.
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
        /// <summary>
        /// Starts listen to messages.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token used to cancel the operation.</param>
        /// <returns>A <see cref="Task"/> that represents the asynchronous operation.</returns>
        Task StartAsync(CancellationToken cancellationToken);
        /// <summary>
        /// Stops listen to messages after a specified time interval.
        /// </summary>
        /// <param name="delay">The time span to wait before completing the returned task, or <see cref="Timeout.InfiniteTimeSpan"/> to wait indefinitely.</param>
        /// <param name="cancellationToken">A cancellation token used to cancel the operation.</param>
        /// <returns>A <see cref="Task"/> that represents the asynchronous operation.</returns>
        Task StopAsync(TimeSpan delay, CancellationToken cancellationToken);
    }
}