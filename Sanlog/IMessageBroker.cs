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
        /// <returns><see langword="true"/> if the element is registered; <see langword="false"/> if the element is already present.</returns>
        bool Register(Type serviceType, IMessageHandler handler);
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