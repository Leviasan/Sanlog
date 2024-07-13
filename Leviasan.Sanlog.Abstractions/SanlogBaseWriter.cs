using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Leviasan.Sanlog
{
    public abstract class SanlogBaseWriter : IDisposable, IAsyncDisposable
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly Channel<LoggingEntry> _channel;
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly CancellationTokenSource _cancellationTokenSourceReader;
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly Task _completion;
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private bool _disposedValue;

        protected SanlogBaseWriter()
        {
            _cancellationTokenSourceReader = new CancellationTokenSource();
            _channel = Channel.CreateUnbounded<LoggingEntry>(new UnboundedChannelOptions
            {
                SingleReader = true,
            });
            _completion = Task.Run(async () =>
            {
                while (!_cancellationTokenSourceReader.Token.IsCancellationRequested && !_channel.Reader.Completion.IsCompleted)
                {
                    var loggingEntry = await _channel.Reader.ReadAsync(_cancellationTokenSourceReader.Token).ConfigureAwait(false);
                    await WriteToStorageAsync(loggingEntry, CancellationToken.None).ConfigureAwait(false);
                }
            }, CancellationToken.None);
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _channel.Writer.Complete(null);
                    _channel.Reader.Completion.GetAwaiter().GetResult();
                    _cancellationTokenSourceReader.Cancel();
                    _completion.GetAwaiter().GetResult();
                    _cancellationTokenSourceReader.Dispose();
                    _completion.Dispose();
                }
                _disposedValue = true;
            }
        }
        public async ValueTask DisposeAsync()
        {
            await DisposeAsyncCore().ConfigureAwait(false);
            Dispose(false);
            GC.SuppressFinalize(this);
        }
        protected async virtual ValueTask DisposeAsyncCore()
        {
            _channel.Writer.Complete(null);
            await _channel.Reader.Completion.ConfigureAwait(false);
            await _cancellationTokenSourceReader.CancelAsync().ConfigureAwait(false);
            await _completion.ConfigureAwait(false);
            _cancellationTokenSourceReader.Dispose();
            _completion.Dispose();
        }
        public bool Enqueue(LoggingEntry item)
        {
            return _channel.Writer.TryWrite(item);
        }
        protected abstract Task WriteToStorageAsync(LoggingEntry loggingEntry, CancellationToken cancellationToken);
    }
}