using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;

namespace Leviasan.Sanlog.EntityFrameworkCore
{
    /// <summary>
    /// Represents the background service to write log entries to the database.
    /// </summary>
    [SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "The class is registered in an inversion of control container as part of the dependency injection pattern")]
    internal sealed class HostedSanlogDbContextWriter : BackgroundService
    {
        /// <summary>
        /// The queue of logs for writing to the storage.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly UnboundedSingleConsumerQueue _channel;
        /// <summary>
        /// The factory for creating <see cref="SanlogDbContext"/> instances.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly IDbContextFactory<SanlogDbContext> _contextFactory;

        /// <summary>
        /// Initializes a new instance of the <see cref="HostedSanlogDbContextWriter"/> class with the specified queue of logs for writing to the storage and factory for creating <see cref="SanlogDbContext"/> instances.
        /// </summary>
        /// <param name="channel">The queue of logs for writing to the storage.</param>
        /// <param name="contextFactory">The factory for creating <see cref="SanlogDbContext"/> instances.</param>
        /// <exception cref="ArgumentNullException">The <paramref name="channel"/> or <paramref name="contextFactory"/> is <see langword="null"/>.</exception>
        public HostedSanlogDbContextWriter(UnboundedSingleConsumerQueue channel, IDbContextFactory<SanlogDbContext> contextFactory)
        {
            _channel = channel ?? throw new ArgumentNullException(nameof(channel));
            _contextFactory = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));
        }

        /// <inheritdoc/>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var loggingEntry = await _channel.Reader.ReadAsync(stoppingToken).ConfigureAwait(false);
                using var context = await _contextFactory.CreateDbContextAsync(stoppingToken).ConfigureAwait(false);
                var addedItem = await context.LogEntries.AddAsync(loggingEntry, stoppingToken).ConfigureAwait(false);
                var changes = await context.SaveChangesAsync(stoppingToken).ConfigureAwait(false);
            }
        }
    }
}