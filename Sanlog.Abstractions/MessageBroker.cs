using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Channels;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.Extensions.Options;
using System.Collections.Frozen;
using Microsoft.Extensions.Hosting;
using System.Linq;

namespace Sanlog.Abstractions
{
    /// <summary>
    /// Represents a service to deliver messages to handlers.
    /// </summary>
    [SuppressMessage("Performance", "CA1812: Avoid uninstantiated internal classes", Justification = "Instantiated via reflection")]
    internal sealed class MessageBroker : BackgroundService, IMessageBroker
    {
        /// <summary>
        /// The underlying channel.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly Channel<MessageContext> _channel;
        /// <summary>
        /// The dictionary of mappings between a message type and its handler.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly FrozenDictionary<Type, IMessageHandler> _consumers;
        /// <summary>
        /// The fallback handler to use when no type-specific handler exists.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly IMessageHandler? _fallbackHandler;
        /// <summary>
        /// To detect redundant calls Dispose method.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private bool _disposedValue;

        /// <summary>
        /// Initializes a new instance of the <see cref="MessageBroker"/> class based on a buffered channel of unbounded capacity for use by any number of writers but at most a single reader at a time.
        /// </summary>
        /// <param name="handlers">The registered handlers.</param>
        /// <param name="options">The configuration of the <see cref="MessageBroker"/>.</param>
        /// <exception cref="ArgumentNullException">One of the parameters is <see langword="null"/>.</exception>
        public MessageBroker(IEnumerable<IMessageHandler> handlers, IOptions<MessageBrokerOptions> options)
        {
            ArgumentNullException.ThrowIfNull(handlers);
            ArgumentNullException.ThrowIfNull(options);

            _consumers = GetClassHandlerMap(handlers, options.Value.Handlers);
            _channel = Channel.CreateUnbounded<MessageContext>(new UnboundedChannelOptions { SingleReader = true });
            if (options.Value.FallbackHandler is not null)
                _fallbackHandler = handlers.SingleOrDefault(x => x.GetType() == options.Value.FallbackHandler);

            static FrozenDictionary<Type, IMessageHandler> GetClassHandlerMap(IEnumerable<IMessageHandler> handlers, Dictionary<Type, Type> map)
            {
                var dictionary = new Dictionary<Type, IMessageHandler>(map.Count);
                foreach (var kvp in map)
                {
                    foreach (var handler in handlers)
                    {
                        if (handler.GetType() == kvp.Value)
                        {
                            dictionary[kvp.Key] = handler;
                            break;
                        }
                    }
                }
                return dictionary.ToFrozenDictionary();
            }
        }

        /// <inheritdoc/>
        public override void Dispose()
        {
            if (!_disposedValue)
            {
                _channel.Writer.Complete();
                _disposedValue = true;
            }
            base.Dispose();
        }
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
        /// <inheritdoc/>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (await _channel.Reader.WaitToReadAsync(stoppingToken).ConfigureAwait(false))
            {
                while (_channel.Reader.TryRead(out var context))
                {
                    if (_consumers.TryGetValue(context.ServiceType, out var handler))
                    {
                        await HandleAsync(handler, context.Message, stoppingToken).ConfigureAwait(false);
                    }
                    else if (_fallbackHandler is not null)
                    {
                        await HandleAsync(_fallbackHandler, context.Message, stoppingToken).ConfigureAwait(false);
                    }
                }
            }

            [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Suppressing throwing exception while handle message")]
            static async ValueTask HandleAsync(IMessageHandler handler, object? message, CancellationToken cancellationToken)
            {
                try
                {
                    await handler.HandleAsync(message, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception exception)
                {
                    Debug.WriteLine(exception, typeof(MessageBroker).FullName);
                }
            }
        }

        /// <summary>
        /// Represents the context of the message.
        /// </summary>
        /// <param name="ServiceType">The service type that mappings with the specified <paramref name="Message"/>.</param>
        /// <param name="Message">The message to handle.</param>
        private sealed record class MessageContext(Type ServiceType, object? Message);
    }
}