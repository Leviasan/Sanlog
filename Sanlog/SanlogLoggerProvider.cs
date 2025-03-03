using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using Microsoft.Extensions.Compliance.Redaction;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sanlog.Extensions.Hosting.Broker;
using Sanlog.Models;

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
        /// The message broker.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly IMessageBroker _messageBroker;
        /// <summary>
        /// To detect redundant calls Dispose method.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private bool _disposedValue;

        /// <summary>
        /// Initializes a new instance of the <see cref="SanlogLoggerProvider"/> class with the specified message broker, redactors provider, and logger options.
        /// </summary>
        /// <param name="messageBroker">The message broker.</param>
        /// <param name="redactorProvider">The redactors provider for different data classifications.</param>
        /// <param name="options">Used to retrieve configured <see cref="SanlogLoggerOptions"/> instances.</param>
        /// <exception cref="ArgumentNullException">The one of the parameters is <see langword="null"/>.</exception>
        protected SanlogLoggerProvider(IMessageBroker messageBroker, IRedactorProvider redactorProvider, IOptions<SanlogLoggerOptions> options)
        {
            ArgumentNullException.ThrowIfNull(messageBroker);
            ArgumentNullException.ThrowIfNull(redactorProvider);
            ArgumentNullException.ThrowIfNull(options);

            _messageBroker = messageBroker;
            _loggers = new ConcurrentDictionary<string, SanlogLogger>(StringComparer.OrdinalIgnoreCase);
            Options = options.Value;
            Formatter = new FormattedLogValuesFormatter(redactorProvider, Options.FormattedConfiguration ?? FormattedLogValuesFormatterOptions.Default);
        }

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
        /// Sends a message to handle.
        /// </summary>
        /// <param name="message">The message to handle.</param>
        /// <returns><see langword="true"/> if the message is accepted for handling; otherwise <see langword="false"/>.</returns>
        internal bool SendMessage(LoggingEntry message) => _messageBroker.SendMessage(GetType(), message);
    }
}