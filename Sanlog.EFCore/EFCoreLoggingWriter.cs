﻿using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace Sanlog.EFCore
{
    /// <summary>
    /// Represents a mechanism to write events to the storage in sync mode.
    /// </summary>
    internal sealed class EFCoreLoggingWriter : SanlogLoggingWriter
    {
        /// <summary>
        /// The factory for creating <see cref="SanlogDbContext"/> instances.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly IDbContextFactory<SanlogDbContext> _contextFactory; // IL2026

        /// <summary>
        /// Initializes a new instance of the <see cref="EFCoreLoggingWriter"/> class with the specified factory for creating <see cref="SanlogDbContext"/> instances.
        /// </summary>
        /// <param name="contextFactory">The factory for creating <see cref="SanlogDbContext"/> instances.</param>
        /// <param name="allowSynchronousContinuations">
        /// <see langword="true"/> if operations performed on a channel may synchronously invoke continuations subscribed to notifications of pending async operations; <see langword="false"/> if all continuations should be invoked asynchronously.
        /// </param>
        /// <exception cref="ArgumentNullException">The <paramref name="contextFactory"/> is <see langword="null"/>.</exception>
        [RequiresUnreferencedCode("EF Core isn't fully compatible with trimming, and running the application may generate unexpected runtime failures." +
            " Some specific coding pattern are usually required to make trimming work properly, see https://aka.ms/efcore-docs-trimming for more details.")]
        public EFCoreLoggingWriter(IDbContextFactory<SanlogDbContext> contextFactory, bool allowSynchronousContinuations) : base(allowSynchronousContinuations)
            => _contextFactory = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));

        /// <inheritdoc/>
        protected override async Task<int> WriteAsync(LoggingEntry loggingEntry)
        {
            // Captured context is required for correct work efcore
            using var context = await _contextFactory.CreateDbContextAsync().ConfigureAwait(true);
            var addedItem = await context.LogEntries.AddAsync(loggingEntry).ConfigureAwait(true);
            return await context.SaveChangesAsync().ConfigureAwait(true);
        }
    }
}