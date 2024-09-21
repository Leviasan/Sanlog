using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace Leviasan.Sanlog.EntityFrameworkCore
{
    /// <summary>
    /// Represents a mechanism to write events to the storage in sync mode.
    /// </summary>
    [SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "The class is registered in an inversion of control container as part of the dependency injection pattern")]
    internal sealed class EFCoreProcessor : SanlogLoggerProcessor
    {
        /// <summary>
        /// The factory for creating <see cref="SanlogDbContext"/> instances.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly IDbContextFactory<SanlogDbContext> _contextFactory; // IL2026

        /// <summary>
        /// Initializes a new instance of the <see cref="EFCoreProcessor"/> class with the specified factory for creating <see cref="SanlogDbContext"/> instances.
        /// </summary>
        /// <param name="contextFactory">The factory for creating <see cref="SanlogDbContext"/> instances.</param>
        /// <exception cref="ArgumentNullException">The <paramref name="contextFactory"/> is <see langword="null"/>.</exception>
        [RequiresUnreferencedCode("EF Core isn't fully compatible with trimming, and running the application may generate unexpected runtime failures. Some specific coding pattern are usually required to make trimming work properly, see https://aka.ms/efcore-docs-trimming for more details.")]
        public EFCoreProcessor(IDbContextFactory<SanlogDbContext> contextFactory, bool allowSynchronousContinuations) : base(allowSynchronousContinuations)
        {
            _contextFactory = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));
        }

        /// <inheritdoc/>
        protected override async Task WriteToStorageAsync(LoggingEntry loggingEntry)
        {
            using var context = await _contextFactory.CreateDbContextAsync().ConfigureAwait(true);
            var addedItem = await context.LogEntries.AddAsync(loggingEntry).ConfigureAwait(true);
            var changes = await context.SaveChangesAsync().ConfigureAwait(true);
        }
    }
}