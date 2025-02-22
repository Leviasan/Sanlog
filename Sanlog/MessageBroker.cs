using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Channels;
using System.Threading.Tasks;
using System.Threading;
using System.Diagnostics.Metrics;
using System.Runtime.InteropServices;

namespace Sanlog
{
    /// <summary>
    /// Represents a service to send/deliver messages to handlers based on <see cref="Channel"/>.
    /// </summary>
    internal sealed class MessageBroker : IMessageBroker, IDisposable
    {
        /// <summary>
        /// The underlying channel.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly Channel<MessageContext> _channel;
        /// <summary>
        /// The dictionary of mappings between a message type and its handlers.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly Dictionary<Type, HashSet<IMessageHandler>> _consumers;
        /// <summary>
        /// The source of the start operation cancellation token.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private CancellationTokenSource? _tokenSource;
        /// <summary>
        /// To detect redundant calls Dispose method.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private bool _disposedValue;

#pragma warning disable IDE0044 // Add readonly modifier
        private Meter _meter;
        private UpDownCounter<int> _counter;
        private Histogram<long> _histogram;
#pragma warning restore IDE0044 // Add readonly modifier

        /// <summary>
        /// Initializes a new instance of the <see cref="MessageBroker"/> class based on a buffered channel of unbounded capacity for use by any number of writers but at most a single reader at a time.
        /// </summary>
        public MessageBroker()
            : this(Channel.CreateUnbounded<MessageContext>(new UnboundedChannelOptions { SingleReader = true })) { }
        /// <summary>
        /// Initializes a new instance of the <see cref="MessageBroker"/> class based on a channel with the specified maximum capacity.
        /// </summary>
        /// <param name="capacity">The maximum number of items the bounded channel may store.</param>
        /// <param name="fullMode">The behavior incurred by write operations when the channel is full.</param>
        /// <param name="itemDropped">Delegate that will be called when item is being dropped from channel.</param>
        /// <exception cref="ArgumentOutOfRangeException">The <paramref name="capacity"/> is less then 1. -or- Passed an invalid <paramref name="fullMode"/>.</exception>
        public MessageBroker(int capacity, BoundedChannelFullMode fullMode, Action<object?>? itemDropped)
            : this(Channel.CreateBounded<MessageContext>(
                options: new BoundedChannelOptions(capacity) { FullMode = fullMode, SingleReader = true },
                itemDropped: itemDropped is not null ? (context) => itemDropped?.Invoke(context.Message) : null))
        { }
        /// <summary>
        /// Initializes a new instance of the <see cref="MessageBroker"/> class with the specified channel.
        /// </summary>
        /// <param name="channel">The underlying channel.</param>
        /// <exception cref="ArgumentNullException">The <paramref name="channel"/>is <see langword="null"/>.</exception>
        private MessageBroker(Channel<MessageContext> channel)
        {
            _channel = channel ?? throw new ArgumentNullException(nameof(channel));
            _consumers = [];

            _meter = new Meter("Sanlog.MessageBroker");
            _counter = _meter.CreateUpDownCounter<int>("broker.channel.reader.count", "{items}", "The current number of items available from the channel reader.");
            _histogram = _meter.CreateHistogram<long>("broker.consumer.duration", "ms", "The duration for processing a message by a handler.");
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        /// <param name="disposing"><see langword="true"/> to dispose managed objects during the finalization phase; otherwise, <see langword="false"/> (not recommended).</param>
        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _channel.Writer.Complete();
                    _tokenSource?.Cancel();
                    _tokenSource?.Dispose();
                    _meter.Dispose();
                }
                _disposedValue = true;
            }
        }

        /// <inheritdoc/>
        /// <exception cref="ArgumentNullException">One of the parameters is <see langword="null"/>.</exception>
        public bool Register(Type serviceType, IMessageHandler handler)
        {
            ArgumentNullException.ThrowIfNull(handler);
            if (_consumers.TryGetValue(serviceType, out var handlers)) // ArgumentNullException
            {
                return handlers.Add(handler);
            }
            else
            {
                _consumers.Add(serviceType, []);
                return _consumers[serviceType].Add(handler);
            }
        }
        /// <inheritdoc/>
        /// <exception cref="ArgumentNullException">One of the parameters is <see langword="null"/>.</exception>
        public bool Remove(Type serviceType, IMessageHandler handler)
        {
            ArgumentNullException.ThrowIfNull(handler);
            return _consumers.TryGetValue(serviceType, out var handlers) && handlers.Remove(handler); // ArgumentNullException
        }
        /// <inheritdoc/>
        /// <exception cref="ArgumentNullException">The <paramref name="serviceType"/> is <see langword="null"/>.</exception>
        public bool Remove(Type serviceType)
            => _consumers.Remove(serviceType); // ArgumentNullException
        /// <inheritdoc/>
        public bool SendMessage<TMessage>(TMessage? message)
            => message is not null && SendMessage(message.GetType(), message);
        /// <inheritdoc/>
        /// <exception cref="ArgumentNullException">The <paramref name="serviceType"/> is <see langword="null"/>.</exception>
        public bool SendMessage<TMessage>(Type serviceType, TMessage? message)
        {
            if (_channel.Writer.TryWrite(new MessageContext(serviceType ?? throw new ArgumentNullException(nameof(serviceType)), message)))
            {
                _counter.Add(1, KeyValuePair.Create<string, object?>("ServiceType", serviceType));
                return true;
            }
            return false;
        }
        /// <inheritdoc/>
        public async ValueTask<bool> SendMessageAsync<TMessage>(TMessage? message, CancellationToken cancellationToken)
            => message is not null && await SendMessageAsync(message.GetType(), message, cancellationToken).ConfigureAwait(false);
        /// <inheritdoc/>
        /// <exception cref="ArgumentNullException">The <paramref name="serviceType"/> is <see langword="null"/>.</exception>
        public async ValueTask<bool> SendMessageAsync<TMessage>(Type serviceType, TMessage? message, CancellationToken cancellationToken)
            => await _channel.Writer.WaitToWriteAsync(cancellationToken).ConfigureAwait(false) && SendMessage(serviceType, message); // ArgumentNullException
        /// <inheritdoc/>
        /// <exception cref="InvalidOperationException">The service is started.</exception>
        [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Suppressing throwing exception while handle context")]
        public Task StartAsync(CancellationToken cancellationToken)
        {
            // Releases cancellation token source
            if (_tokenSource is not null)
            {
                if (_tokenSource.IsCancellationRequested == false)
                {
                    throw new InvalidOperationException("The message broker is started.");
                }
                else
                {
                    _tokenSource.Dispose();
                }
            }
            // Creates new linked cancellation token source
            _tokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            // Starts listening to messages as a long-running operation
            return Task.Factory.StartNew(async delegate
            {
                while (await _channel.Reader.WaitToReadAsync(_tokenSource.Token).ConfigureAwait(false))
                {
                    while (_channel.Reader.TryRead(out var context))
                    {
                        _counter.Add(-1, KeyValuePair.Create<string, object?>("ServiceType", context.ServiceType));
                        if (_consumers.TryGetValue(context.ServiceType, out var handlers))
                        {
                            foreach (var handler in handlers)
                            {
                                try
                                {
                                    var stopwatch = Stopwatch.StartNew();
                                    await handler.HandleAsync(context.Message, _tokenSource.Token).ConfigureAwait(false);
                                    stopwatch.Stop();
                                    _histogram.Record(
                                        value: stopwatch.ElapsedMilliseconds,
                                        tag1: KeyValuePair.Create<string, object?>("ServiceType", context.ServiceType),
                                        tag2: KeyValuePair.Create<string, object?>("HandlerType", handler.GetType()));
                                }
                                catch
                                {
                                    // ignored
                                }
                            }
                        }
                    }
                }
            }, cancellationToken, TaskCreationOptions.LongRunning, TaskScheduler.Default);
        }
        /// <inheritdoc/>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="delay"/> represents a negative time interval other than <see cref="Timeout.InfiniteTimeSpan"/>.
        /// -or- The delay argument's <see cref="TimeSpan.TotalMilliseconds"/> property is greater than 4294967294 on .NET 6 and later versions, or <see cref="int.MaxValue"/> on all previous versions.</exception>
        /// <exception cref="InvalidOperationException">The service is not started.</exception>
        /// <exception cref="TaskCanceledException">The task has been canceled.</exception>
        /// <exception cref="ObjectDisposedException">The provided <paramref name="cancellationToken"/> has already been disposed.</exception>
        public async Task StopAsync(TimeSpan delay, CancellationToken cancellationToken)
        {
            if (_tokenSource is null || _tokenSource.IsCancellationRequested)
            {
                throw new InvalidOperationException("The message broker is not started.");
            }
            await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
            await _tokenSource.CancelAsync().ConfigureAwait(false);
        }

        /// <summary>
        /// Represents the context of the message.
        /// </summary>
        /// <param name="ServiceType">The service type that mappings with the specified <paramref name="Message"/>.</param>
        /// <param name="Message">The message to handle.</param>
        private sealed record class MessageContext(Type ServiceType, object? Message);
    }
}