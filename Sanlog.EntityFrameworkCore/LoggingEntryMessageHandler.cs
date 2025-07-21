using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Sanlog.Brokers;

namespace Sanlog.EntityFrameworkCore
{
    /// <summary>
    /// Represents a handler for writing <see cref="LoggingEntry"/> to the storage.
    /// </summary>
    /// <param name="contextFactory">The factory for creating <see cref="SanlogDbContext"/> instances.</param>
    [SuppressMessage("Performance", "CA1812: Avoid uninstantiated internal classes", Justification = "Instantiated via reflection")]
    internal sealed class LoggingEntryMessageHandler(IDbContextFactory<SanlogDbContext> contextFactory) : IMessageHandler
    {
        /// <summary>
        /// The factory for creating <see cref="SanlogDbContext"/> instances.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly IDbContextFactory<SanlogDbContext> _contextFactory = contextFactory; // IL2026

        /// <inheritdoc/>
        public async ValueTask HandleAsync(object? message, CancellationToken cancellationToken)
        {
            if (message is LoggingEntry loggingEntry)
            {
                using SanlogDbContext context = await _contextFactory
                    .CreateDbContextAsync(cancellationToken)
                    .ConfigureAwait(true); // Captured context is required
                Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry<LoggingEntry> addedItem = await context
                    .LogEntries
                    .AddAsync(loggingEntry, cancellationToken)
                    .ConfigureAwait(true); // Captured context is required
                int added = await context
                    .SaveChangesAsync(cancellationToken)
                    .ConfigureAwait(true); // Captured context is required
            }
        }
    }
}