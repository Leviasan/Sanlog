using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Sanlog.Brokers
{
    /// <summary>
    /// Represents a service to deliver messages to handlers.
    /// </summary>
    /// <param name="channel">The underlying channel.</param>
    [SuppressMessage("Performance", "CA1812: Avoid uninstantiated internal classes", Justification = "Instantiated via reflection")]
    internal sealed class MessageReceiver(Channel<MessageContext> channel) : IMessageReceiver
    {
        /// <summary>
        /// The underlying channel.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly Channel<MessageContext> _channel = channel;

        /// <inheritdoc/>
        public bool SendMessage<TMessage>(TMessage? message) => SendMessage(typeof(TMessage), message);
        /// <inheritdoc/>
        /// <exception cref="ArgumentNullException">The <paramref name="serviceType"/> is <see langword="null"/>.</exception>
        public bool SendMessage<TMessage>(Type serviceType, TMessage? message)
            => _channel.Writer.TryWrite(new MessageContext(serviceType ?? throw new ArgumentNullException(nameof(serviceType)), message));
        /// <inheritdoc/>
        public async ValueTask<bool> SendMessageAsync<TMessage>(TMessage? message, CancellationToken cancellationToken)
            => await SendMessageAsync(typeof(TMessage), message, cancellationToken).ConfigureAwait(false);
        /// <inheritdoc/>
        /// <exception cref="ArgumentNullException">The <paramref name="serviceType"/> is <see langword="null"/>.</exception>
        public async ValueTask<bool> SendMessageAsync<TMessage>(Type serviceType, TMessage? message, CancellationToken cancellationToken)
            => await _channel.Writer.WaitToWriteAsync(cancellationToken).ConfigureAwait(false) && SendMessage(serviceType, message); // ArgumentNullException
    }
}