﻿using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Sanlog.Brokers;
using Sanlog.Models;
using Sanlog.Storage;

namespace Sanlog
{
    /// <summary>
    /// Represents a handler for writing <see cref="LoggingEntry"/> to the storage.
    /// </summary>
    /// <param name="contextFactory">The factory for creating <see cref="SanlogDbContext"/> instances.</param>
    internal sealed class EntityFrameworkCoreMessageHandler(IDbContextFactory<SanlogDbContext> contextFactory) : IMessageHandler
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
                using var context = await _contextFactory
                    .CreateDbContextAsync(cancellationToken)
                    .ConfigureAwait(true); // Captured context is required
                var addedItem = await context
                    .LogEntries
                    .AddAsync(loggingEntry, cancellationToken)
                    .ConfigureAwait(true); // Captured context is required
                var added = await context
                    .SaveChangesAsync(cancellationToken)
                    .ConfigureAwait(true); // Captured context is required
            }
        }
    }
}