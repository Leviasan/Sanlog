using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Leviasan.Sanlog
{
    /// <summary>
    /// Represents a writer that can write a logging entry to storage. This class is abstract.
    /// </summary>
    public abstract class StorageWriter : IDisposable, IAsyncDisposable
    {
        /// <summary>
        /// The underlying channel.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly Channel<LoggingEntry> _channel;
        /// <summary>
        /// The cancellation token source of the reading operation.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly CancellationTokenSource _cancellationTokenSource;
        /// <summary>
        /// The task is completed when no more data to write to storage from the completed channel.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly Task _completion;
        /// <summary>
        /// To detect redundant calls Dispose method.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private bool _disposedValue;

        /// <summary>
        /// Initializes a new instance of the <see cref="StorageWriter"/> class.
        /// </summary>
        protected StorageWriter()
        {
            _cancellationTokenSource = new CancellationTokenSource();
            _channel = Channel.CreateUnbounded<LoggingEntry>(new UnboundedChannelOptions
            {
                SingleReader = true
            });
            _completion = Task.Run(async () =>
            {
                while (!_cancellationTokenSource.Token.IsCancellationRequested && !_channel.Reader.Completion.IsCompleted)
                {
                    var loggingEntry = await _channel.Reader.ReadAsync(_cancellationTokenSource.Token).ConfigureAwait(false);
                    await WriteToStorageAsync(loggingEntry, CancellationToken.None).ConfigureAwait(false);
                }
            }, CancellationToken.None);
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
        public async ValueTask DisposeAsync()
        {
            await DisposeAsyncCore().ConfigureAwait(false);
            Dispose(false);
            GC.SuppressFinalize(this);
        }
        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources asynchronously.
        /// </summary>
        protected async virtual ValueTask DisposeAsyncCore()
        {
            _channel.Writer.Complete(null);
            await _channel.Reader.Completion.ConfigureAwait(false);
            await _cancellationTokenSource.CancelAsync().ConfigureAwait(false);
            await _completion.ConfigureAwait(false);
            _cancellationTokenSource.Dispose();
            _completion.Dispose();
        }
        /// <summary>
        /// Attempts to write the specified item to the channel.
        /// </summary>
        /// <param name="item">The item to write.</param>
        /// <returns><see langword="true"/> if the item was written; otherwise, <see langword="false"/>.</returns>
        public bool Enqueue(LoggingEntry item)
        {
            return _channel.Writer.TryWrite(item);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="loggingEntry"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        protected abstract Task WriteToStorageAsync(LoggingEntry loggingEntry, CancellationToken cancellationToken);
    }
}