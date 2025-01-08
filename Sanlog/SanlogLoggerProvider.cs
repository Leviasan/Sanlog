using System;
using System.Collections.Concurrent;
using System.Diagnostics;
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
        /// The message broker.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly IMessageBroker _messageBroker;
        /// <summary>
        /// The cache value of the current type to prevent multi-call <see cref="object.GetType()"/> method.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly Type _type;
        /// <summary>
        /// To detect redundant calls Dispose method.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private bool _disposedValue;

        /// <summary>
        /// Initializes a new instance of the <see cref="SanlogLoggerProvider"/> class with the specified message broker and logger options monitor.
        /// </summary>
        /// <param name="messageBroker">The message broker. The caller is responsible for the release.</param>
        /// <param name="optionsMonitor">Used for notifications when <see cref="SanlogLoggerOptions"/> instances change.</param>
        /// <exception cref="ArgumentNullException">The <paramref name="messageBroker"/> or <paramref name="optionsMonitor"/> is <see langword="null"/>.</exception>
        protected SanlogLoggerProvider(IMessageBroker messageBroker, IOptionsMonitor<SanlogLoggerOptions> optionsMonitor)
        {
            ArgumentNullException.ThrowIfNull(messageBroker);
            ArgumentNullException.ThrowIfNull(optionsMonitor);

            _type = GetType();
            _messageBroker = messageBroker;
            _changeTokenRegistration = optionsMonitor.OnChange((options) => Options = options);
            _loggers = new ConcurrentDictionary<string, SanlogLogger>(StringComparer.OrdinalIgnoreCase);
            Options = optionsMonitor.CurrentValue;
        }

        /// <summary>
        /// Gets the external storage of the common scope data.
        /// </summary>
        public IExternalScopeProvider? ExternalScopeProvider { get; private set; }
        /// <summary>
        /// Gets the logger options.
        /// </summary>
        public SanlogLoggerOptions Options { get; private set; }

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
            return new SanlogLogger(category, this);
        });
        /// <inheritdoc/>
        public void SetScopeProvider(IExternalScopeProvider? scopeProvider) => ExternalScopeProvider = scopeProvider;
        /// <summary>
        /// Sends a message to handle.
        /// </summary>
        /// <param name="message">The message to handle.</param>
        /// <returns><see langword="true"/> if the message is accepted for handling; otherwise <see langword="false"/>.</returns>
        public bool SendMessage(LoggingEntry message) => _messageBroker.SendMessage(_type, message);
    }
}