﻿using System;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Sanlog
{
    /// <summary>
    /// Represents a logger provider that can create instances of <see cref = "SanlogLogger" /> and consume external scope information.
    /// </summary>
    public abstract class SanlogLoggerProvider : ILoggerProvider, ISupportExternalScope, IDisposable
    {
        /// <summary>
        /// The cache-collection of the created loggers.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly ConcurrentDictionary<string, SanlogLogger> _loggers;
        /// <summary>
        /// The listener to be called whenever a <see cref="SanlogLoggerOptions"/> changes.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly IDisposable? _changeTokenRegistration;
        /// <summary>
        /// To detect redundant calls Dispose method.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private bool _disposedValue;

        /// <summary>
        /// Initializes a new instance of the <see cref="SanlogLoggerProvider"/> class with the specified maximum number of items the bounded channel may store,
        /// behavior incurred by write operations when the channel is full and moninor of the logger options.
        /// </summary>
        /// <param name="capacity">The maximum number of items the bounded channel may store.</param>
        /// <param name="fullMode">The behavior incurred by write operations when the channel is full.</param>
        /// <param name="optionsMonitor">Used for notifications when <see cref="SanlogLoggerOptions"/> instances change.</param>
        /// <exception cref="ArgumentNullException">The <paramref name="optionsMonitor"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentOutOfRangeException">The <paramref name="capacity"/> is less then 1.</exception>
        /// <exception cref="InvalidEnumArgumentException">Passed an invalid <paramref name="fullMode"/> value.</exception>
        protected SanlogLoggerProvider(int capacity, BoundedChannelFullMode fullMode, IOptionsMonitor<SanlogLoggerOptions> optionsMonitor)
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(capacity, 1);
            if (!Enum.IsDefined(fullMode))
                throw new InvalidEnumArgumentException(nameof(fullMode), (int)fullMode, typeof(BoundedChannelFullMode));
            ArgumentNullException.ThrowIfNull(optionsMonitor);

            _changeTokenRegistration = optionsMonitor.OnChange((options) => Options = options);
            _loggers = new ConcurrentDictionary<string, SanlogLogger>(StringComparer.OrdinalIgnoreCase);
            Handler = new MessageHandler<LoggingEntry>(capacity, fullMode, WriteAsync, null);
            Options = optionsMonitor.CurrentValue;
        }

        /// <summary>
        /// Gets the external storage of the common scope data.
        /// </summary>
        internal IExternalScopeProvider? ExternalScopeProvider { get; private set; }
        /// <summary>
        /// Gets the logger options.
        /// </summary>
        internal SanlogLoggerOptions Options { get; private set; }
        /// <summary>
        /// Gets the message handler.
        /// </summary>
        internal MessageHandler<LoggingEntry> Handler { get; }

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
                    _loggers.Clear();
                    _changeTokenRegistration?.Dispose();
                    Handler.Dispose();
                }
                _disposedValue = true;
            }
        }
        /// <inheritdoc/>
        /// <exception cref="ArgumentNullException">The <paramref name="categoryName"/> is <see langword="null"/>.</exception>
        /// <exception cref="ObjectDisposedException">The logger provider is disposed.</exception>
        public ILogger CreateLogger(string categoryName) => _loggers.GetOrAdd(categoryName, category => // ArgumentNullException
        {
            ObjectDisposedException.ThrowIf(_disposedValue, this);
            var logger = new SanlogLogger(category, this);
            return logger;
        });
        /// <inheritdoc/>
        public void SetScopeProvider(IExternalScopeProvider? scopeProvider) => ExternalScopeProvider = scopeProvider;
        /// <summary>
        /// Asynchronously writes the message to the storage.
        /// </summary>
        /// <param name="item">The message to store.</param>
        /// <param name="cancellationToken">A cancellation token used to cancel the operation.</param>
        /// <returns>A <see cref="ValueTask"/> that represents the asynchronous operation.</returns>
        protected abstract ValueTask WriteAsync(LoggingEntry item, CancellationToken cancellationToken);
    }
}