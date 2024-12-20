﻿using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Sanlog
{
    /// <summary>
    /// Provides a mechanism for writing logging entries to storage. This class is abstract.
    /// </summary>
    public abstract class SanlogLoggingWriter : IAsyncDisposable, IDisposable
    {
        /// <summary>
        /// The underlying channel.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly Channel<LoggingEntry> _channel;
        /// <summary>
        /// The source of the cancellation token of the reading operation.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly CancellationTokenSource _cancellationTokenSource;
        /// <summary>
        /// The task is completed when there is no data to write from the completed channel.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly Task _completion;
        /// <summary>
        /// To detect redundant calls Dispose method.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private bool _disposedValue;

        /// <summary>
        /// Initializes a new instance of the <see cref="SanlogLoggingWriter"/> class with the specified state of the channel.
        /// </summary>
        /// <param name="allowSynchronousContinuations">
        /// <see langword="true"/> if operations performed on a channel may synchronously invoke continuations subscribed to notifications of pending async operations; <see langword="false"/> if all continuations should be invoked asynchronously.
        /// </param>
        protected SanlogLoggingWriter(bool allowSynchronousContinuations)
        {
            _cancellationTokenSource = new CancellationTokenSource();
            _channel = Channel.CreateUnbounded<LoggingEntry>(new UnboundedChannelOptions
            {
                SingleReader = true,
                SingleWriter = false,
                AllowSynchronousContinuations = allowSynchronousContinuations
            });
            _completion = Task.Run(WaitToWriteAsync, CancellationToken.None);

            async Task WaitToWriteAsync()
            {
                while (!_cancellationTokenSource.IsCancellationRequested && await _channel.Reader.WaitToReadAsync(_cancellationTokenSource.Token).ConfigureAwait(false))
                {
                    while (_channel.Reader.TryRead(out var loggingEntry))
                    {
                        _ = await WriteAsync(loggingEntry).ConfigureAwait(false);
                    }
                }
            }
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
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _channel.Writer.Complete(null);
                    _channel.Reader.Completion.GetAwaiter().GetResult();
                    _cancellationTokenSource.Cancel();
                    _completion.GetAwaiter().GetResult();
                    _cancellationTokenSource.Dispose();
                    _completion.Dispose();
                }
                _disposedValue = true;
            }
        }
        /// <inheritdoc/>
        public virtual async ValueTask DisposeAsync()
        {
            _channel.Writer.Complete(null);
            await _channel.Reader.Completion.ConfigureAwait(false);
            await _cancellationTokenSource.CancelAsync().ConfigureAwait(false);
            await _completion.ConfigureAwait(false);
            _cancellationTokenSource.Dispose();
            _completion.Dispose();
            Dispose(false);
            GC.SuppressFinalize(this);
        }
        /// <summary>
        /// Attempts to write the specified item to the channel.
        /// </summary>
        /// <param name="item">The item to write.</param>
        /// <returns><see langword="true"/> if the item was written; otherwise, <see langword="false"/>.</returns>
        public bool Enqueue(LoggingEntry item) => _channel.Writer.TryWrite(item);
        /// <summary>
        /// Writes the specified logging entry to the storage.
        /// </summary>
        /// <param name="loggingEntry">The logging entry.</param>
        /// <returns>A task that represents the asynchronous save operation. The task result contains the number of entries written to the store.</returns>
        protected abstract Task<int> WriteAsync(LoggingEntry loggingEntry);
    }
}