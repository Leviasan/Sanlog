using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Sanlog
{
    /// <summary>
    /// Represents the message handler.
    /// </summary>
    /// <typeparam name="T">The type of the message to handle.</typeparam>
    public sealed class MessageHandler<T> : IDisposable
    {
        /// <summary>
        /// The underlying channel.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly Channel<T> _channel;
        /// <summary>
        /// The source of the cancellation token of the reading/writing operation.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly CancellationTokenSource _cancellationTokenSource;
        /// <summary>
        /// To detect redundant calls Dispose method.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private bool _disposedValue;

        /// <summary>
        /// Initializes a new instance of the <see cref="MessageHandler{T}"/> class with the specified maximum number of items the bounded channel may store,
        /// the behavior incurred by write operations when the channel is full and method that defines how to handle the input message.
        /// </summary>
        /// <param name="capacity">The maximum number of items the bounded channel may store.</param>
        /// <param name="fullMode">The behavior incurred by write operations when the channel is ful</param>
        /// <param name="callback">The method that defines how to handle the input message.</param>
        /// <exception cref="ArgumentNullException">One of the parameters is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentOutOfRangeException">The <paramref name="capacity"/> is less then 1. -or- Passed an unsupported <paramref name="fullMode"/>.</exception>
        public MessageHandler(int capacity, BoundedChannelFullMode fullMode, HandleMessage<T> callback)
        {
            ArgumentNullException.ThrowIfNull(callback);
            _channel = Channel.CreateBounded<T>(new BoundedChannelOptions(capacity) // ArgumentOutOfRangeException
            {
                SingleReader = true,
                FullMode = fullMode // ArgumentOutOfRangeException
            });
            _cancellationTokenSource = new CancellationTokenSource();
            Completion = HandleMessage(callback, _cancellationTokenSource.Token);
        }

        /// <summary>
        /// Gets a <see cref="Task"/> that completes when no more data is to be handled or the operation is canceled.
        /// </summary>
        public Task Completion { get; }

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
                    _cancellationTokenSource.Cancel();
                    _channel.Writer.Complete(Completion.Exception);
                    _cancellationTokenSource.Dispose();
                }
                _disposedValue = true;
            }
        }
        /// <inheritdoc cref="ChannelWriter{T}.TryWrite(T)"/>
        public bool TryWrite(T item) => _channel.Writer.TryWrite(item);
        /// <inheritdoc cref="ChannelWriter{T}.WriteAsync(T, CancellationToken)"/>
        public ValueTask WriteAsync(T item, CancellationToken cancellationToken = default) => _channel.Writer.WriteAsync(item, cancellationToken);
        /// <summary>
        /// Processes messages through the handler.
        /// </summary>
        /// <param name="callback">The method that defines how to handle the input message.</param>
        /// <param name="cancellationToken">A cancellation token used to cancel the operation.</param>
        /// <returns>A <see cref="Task"/> that represents the asynchronous operation.</returns>
        [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Suppressing throwing exception while handle message")]
        private async Task HandleMessage(HandleMessage<T> callback, CancellationToken cancellationToken)
        {
            while (await _channel.Reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false))
            {
                while (!cancellationToken.IsCancellationRequested && _channel.Reader.TryRead(out var item))
                {
                    try
                    {
                        await callback.Invoke(item, cancellationToken).ConfigureAwait(false);
                    }
                    catch
                    {
                        // ignored
                    }
                }
            }
        }
    }
    /// <summary>
    /// Represents the method that defines how to handle the input message.
    /// </summary>
    /// <typeparam name="T">The type of the message to handle.</typeparam>
    /// <param name="message">The message to handle.</param>
    /// <param name="cancellationToken">A cancellation token used to cancel the operation.</param>
    /// <returns>A <see cref="ValueTask"/> that represents the asynchronous operation.</returns>
    public delegate ValueTask HandleMessage<in T>(T message, CancellationToken cancellationToken);
}