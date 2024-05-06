using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;

namespace Leviasan.Sanlog.EntityFrameworkCore
{
    /// <summary>
    /// Represents a mechanism to write events to the storage in async mode.
    /// </summary>
    [SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "The class is registered in an inversion of control container as part of the dependency injection pattern")]
    internal sealed class SanlogDbContextWriter : ILoggingWriter
    {
        /// <summary>
        /// The factory for creating <see cref="SanlogDbContext"/> instances.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly IDbContextFactory<SanlogDbContext> _contextFactory;

        /// <summary>
        /// Initializes a new instance of the <see cref="SanlogDbContextWriter"/> class with the specified factory for creating <see cref="SanlogDbContext"/> instances.
        /// </summary>
        /// <param name="contextFactory">The factory for creating <see cref="SanlogDbContext"/> instances.</param>
        /// <exception cref="ArgumentNullException">The <paramref name="contextFactory"/> is <see langword="null"/>.</exception>
        public SanlogDbContextWriter(IDbContextFactory<SanlogDbContext> contextFactory) => _contextFactory = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));

        /// <inheritdoc/>
        public void Write(LoggingEntry item)
        {
            using var context = _contextFactory.CreateDbContext();
            var addedItem = context.LogEntries.Add(item);
            var changes = context.SaveChanges();
        }
    }
}