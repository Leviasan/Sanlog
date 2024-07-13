using System.Diagnostics;
using System;
using System.Threading;
using System.Threading.Tasks;
//using Leviasan.Sanlog.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Leviasan.Sanlog
{
    /*
    /// <summary>
    /// Represents the background service to write log entries to the file storage.
    /// </summary>
    internal sealed class HostedFileLoggingWriter : BackgroundService
    {
        /// <summary>
        /// The queue of logs for writing to the storage.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly UnboundedSingleConsumerChannel _channel;
        /// <summary>
        /// The logging writer to the file storage.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly FileLoggingWriter _loggingWriter;

        /// <summary>
        /// Initializes a new instance of the <see cref="HostedFileLoggingWriter"/> class with the specified queue of logs for writing to the storage and logging writer to the file storage.
        /// </summary>
        /// <param name="channel">The queue of logs for writing to the storage.</param>
        /// <param name="loggingWriter">he logging writer to the file storage.</param>
        /// <exception cref="ArgumentNullException">The <paramref name="channel"/> or <paramref name="loggingWriter"/> is <see langword="null"/>.</exception>
        public HostedFileLoggingWriter([FromKeyedServices(nameof(HostedFileLoggingWriter))] UnboundedSingleConsumerChannel channel, FileLoggingWriter loggingWriter)
        {
            _channel = channel ?? throw new ArgumentNullException(nameof(channel));
            _loggingWriter = loggingWriter ?? throw new ArgumentNullException(nameof(loggingWriter));
        }

        /// <inheritdoc/>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // TODO: Write exception
            while (!stoppingToken.IsCancellationRequested)
            {
                var loggingEntry = await _channel.Reader.ReadAsync(stoppingToken).ConfigureAwait(false);
                _loggingWriter.Write(loggingEntry);


                using var context = await _contextFactory.CreateDbContextAsync(stoppingToken).ConfigureAwait(false);
                var addedItem = await context.LogEntries.AddAsync(loggingEntry, stoppingToken).ConfigureAwait(false);
                var changes = await context.SaveChangesAsync(stoppingToken).ConfigureAwait(false);
            }
        }
    }
    */
}