using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

//namespace Sanlog.EFCore
//{
    /*
    /// <summary>
    /// Represents a mechanism to write events to the database storage.
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of the <see cref="EFCoreLoggingWriter"/> class with the specified factory for creating <see cref="SanlogDbContext"/> instances.
    /// </remarks>
    /// <param name="contextFactory">The factory for creating <see cref="SanlogDbContext"/> instances.</param>
    /// <param name="allowSynchronousContinuations">
    /// <see langword="true"/> if operations performed on a channel may synchronously invoke continuations subscribed to notifications of pending async operations;
    /// <see langword="false"/> if all continuations should be invoked asynchronously.
    /// </param>
    /// <exception cref="ArgumentNullException">The <paramref name="contextFactory"/> is <see langword="null"/>.</exception>
    [method: RequiresUnreferencedCode("EF Core isn't fully compatible with trimming, and running the application may generate unexpected runtime failures." +
            " Some specific coding pattern are usually required to make trimming work properly, see https://aka.ms/efcore-docs-trimming for more details.")]
    internal sealed class EFCoreLoggingWriter(IDbContextFactory<SanlogDbContext> contextFactory, bool allowSynchronousContinuations) : SanlogLoggingWriter(allowSynchronousContinuations)
    {
        /// <summary>
        /// The factory for creating <see cref="SanlogDbContext"/> instances.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly IDbContextFactory<SanlogDbContext> _contextFactory = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory)); // IL2026

        /// <inheritdoc/>
        protected override async Task<int> WriteAsync(LoggingEntry loggingEntry)
        {
            // Captured context is required for correct work efcore
            using var context = await _contextFactory.CreateDbContextAsync().ConfigureAwait(true);
            var addedItem = await context.LogEntries.AddAsync(loggingEntry).ConfigureAwait(true);
            return await context.SaveChangesAsync().ConfigureAwait(true);
        }
    }
    */
    /*
    /// <summary>
    /// 
    /// </summary>
    /// <param name="capacity"></param>
    /// <param name="fullMode"></param>
    /// <param name="optionsMonitor"></param>
    [ProviderAlias("SanlogLoggerProvider")]
    public sealed class EFCoreSanlogLoggerProvider(int capacity, BoundedChannelFullMode fullMode, IOptionsMonitor<SanlogLoggerOptions> optionsMonitor, IDbContextFactory<SanlogDbContext> contextFactory)
        : SanlogLoggerProvider(capacity, fullMode, optionsMonitor)
    {
        /// <summary>
        /// The factory for creating <see cref="SanlogDbContext"/> instances.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly IDbContextFactory<SanlogDbContext> _contextFactory = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory)); // IL2026

        /// <inheritdoc/>
        protected override async ValueTask WriteAsync(LoggingEntry item, CancellationToken cancellationToken)
        {
            // Captured context is required for correct work efcore
            using var context = await _contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(true);
            var addedItem = await context.LogEntries.AddAsync(item, cancellationToken).ConfigureAwait(true);
            _ = await context.SaveChangesAsync(cancellationToken).ConfigureAwait(true);
        }
    }
    */
//}