using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using Microsoft.Extensions.Compliance.Redaction;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sanlog.Brokers;
using Sanlog.Formatters;

namespace Sanlog
{
    /// <summary>
    /// Represents a logger provider that can create instances of <see cref="SanlogLogger"/> and consume external scope information.
    /// </summary>
    public abstract class SanlogLoggerProvider : ILoggerProvider, ISupportExternalScope
    {
        /// <summary>
        /// The cache-collection of the created loggers.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly ConcurrentDictionary<string, SanlogLogger> _loggers;
        /// <summary>
        /// The message receiver.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly IMessageReceiver _receiver;
        /// <summary>
        /// To detect redundant calls Dispose method.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private bool _disposedValue;

        /// <summary>
        /// Initializes a new instance of the <see cref="SanlogLoggerProvider"/> class with the specified message broker receiver, redactor provider and logger options.
        /// </summary>
        /// <param name="receiver">The message broker receiver.</param>
        /// <param name="redactorProvider">The redactors provider for different data classifications.</param>
        /// <param name="options">The configuration of the <see cref="SanlogLoggerProvider"/>.</param>
        /// <exception cref="ArgumentNullException">One of the parameters is <see langword="null"/>.</exception>
        protected SanlogLoggerProvider(IMessageReceiver receiver, IRedactorProvider redactorProvider, IOptions<SanlogLoggerOptions> options)
        {
            ArgumentNullException.ThrowIfNull(receiver);
            ArgumentNullException.ThrowIfNull(redactorProvider);
            ArgumentNullException.ThrowIfNull(options);

            _receiver = receiver;
            _loggers = new ConcurrentDictionary<string, SanlogLogger>(StringComparer.OrdinalIgnoreCase);
            Options = options.Value;
            Formatter = new FormattedLogValuesFormatter(redactorProvider, options.Value.FormattedOptions ?? LoggerFormatterOptions.Default);
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="SanlogLoggerProvider"/> class with the specified message broker receiver, <see cref="NullRedactorProvider.Instance"/> and logger options.
        /// </summary>
        /// <param name="receiver">The message broker receiver.</param>
        /// <param name="options">The configuration of the <see cref="SanlogLoggerProvider"/>.</param>
        /// <exception cref="ArgumentNullException">One of the parameters is <see langword="null"/>.</exception>
        protected SanlogLoggerProvider(IMessageReceiver receiver, IOptions<SanlogLoggerOptions> options)
            : this(receiver, NullRedactorProvider.Instance, options) { }

        /// <summary>
        /// Gets the external storage of the common scope data.
        /// </summary>
        internal IExternalScopeProvider? ExternalScopeProvider { get; private set; }
        /// <summary>
        /// Gets the log values formatter.
        /// </summary>
        internal FormattedLogValuesFormatter Formatter { get; }
        /// <summary>
        /// Gets the logger options.
        /// </summary>
        internal SanlogLoggerOptions Options { get; }

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
            return new SanlogLogger(category, this); // ArgumentNullException
        });
        /// <inheritdoc/>
        public void SetScopeProvider(IExternalScopeProvider? scopeProvider) => ExternalScopeProvider = scopeProvider;
        /// <summary>
        /// Sends a message to service that handles the specified logger provider.
        /// </summary>
        /// <param name="message">The message to handle.</param>
        /// <returns><see langword="true"/> if the message is accepted for handling; otherwise <see langword="false"/>.</returns>
        internal bool SendMessage(LoggingEntry message) => _receiver.SendMessage(GetType(), message);
    }
}