using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Sanlog.Brokers
{
    /// <summary>
    /// Represents a service to handle messages.
    /// </summary>
    [SuppressMessage("Performance", "CA1812: Avoid uninstantiated internal classes", Justification = "Instantiated via reflection")]
    internal sealed class MessageBroker : BackgroundService
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
        /// Initializes a new instance of the <see cref="MessageBroker"/> class based on a buffered channel, array of handlers, and broker options.
        /// </summary>
        /// <param name="channel">The underlying channel.</param>
        /// <param name="handlers">The registered handlers.</param>
        /// <param name="options">The configuration of the <see cref="MessageBroker"/>.</param>
        /// <exception cref="ArgumentNullException">One of the parameters is <see langword="null"/>.</exception>
        public MessageBroker(Channel<MessageContext> channel, IEnumerable<IMessageHandler> handlers, IOptions<MessageBrokerOptions> options)
        {
            ArgumentNullException.ThrowIfNull(channel);
            ArgumentNullException.ThrowIfNull(handlers);
            ArgumentNullException.ThrowIfNull(options);

            _channel = channel;
            _consumers = GetClassHandlerMap(handlers, options.Value.Handlers);
            if (options.Value.FallbackHandler is not null)
                _fallbackHandler = handlers.SingleOrDefault(x => x.GetType() == options.Value.FallbackHandler);

            static FrozenDictionary<Type, IMessageHandler> GetClassHandlerMap(IEnumerable<IMessageHandler> handlers, Dictionary<Type, Type> map)
            {
                Dictionary<Type, IMessageHandler> dictionary = new(map.Count);
                foreach (KeyValuePair<Type, Type> kvp in map)
                {
                    foreach (IMessageHandler handler in handlers)
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
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (await _channel.Reader.WaitToReadAsync(stoppingToken).ConfigureAwait(false))
            {
                while (_channel.Reader.TryRead(out MessageContext? context))
                {
                    stoppingToken.ThrowIfCancellationRequested();
                    if (_consumers.TryGetValue(context.ServiceType, out IMessageHandler? handler))
                    {
                        await TryHandleAsync(handler, context.Message, stoppingToken).ConfigureAwait(false);
                    }
                    else if (_fallbackHandler is not null)
                    {
                        await TryHandleAsync(_fallbackHandler, context.Message, stoppingToken).ConfigureAwait(false);
                    }
                }
            }

            [SuppressMessage("Design", "CA1031: Do not catch general exception types", Justification = "Suppressing throwing exception while handle message")]
            static async ValueTask TryHandleAsync(IMessageHandler handler, object? message, CancellationToken cancellationToken)
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
    }
}