using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Leviasan.Sanlog
{
    /// <summary>
    /// Represents a logger provider the can create instances of <see cref="SanlogLogger"/> and can consume external scope information.
    /// </summary>
    [ProviderAlias(nameof(SanlogLoggerProvider))]
    public sealed class SanlogLoggerProvider : ILoggerProvider, ISupportExternalScope, IAsyncDisposable
    {
        /// <summary>
        /// The <see cref="ServiceDescriptor.ServiceKey"/> of the service.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly object? _serviceKey;
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
        /// The writer service.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly SanlogLoggerProcessor _writer;
        /// <summary>
        /// The external storage of the common scope data.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private IExternalScopeProvider? _externalScopeProvider;
        /// <summary>
        /// The logger options.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private SanlogLoggerOptions _options;
        /// <summary>
        /// To detect redundant calls Dispose method.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private bool _disposedValue;

        /// <summary>
        /// Initializes a new instance of the <see cref="SanlogLoggerProvider"/> class with the specified writer service and logger options.
        /// </summary>
        /// <param name="serviceKey">The <see cref="ServiceDescriptor.ServiceKey"/> of the service.</param>
        /// <param name="writer">The writer service. The callee is responsible for disposing of the writer.</param>
        /// <param name="optionsMonitor">Used for notifications when <see cref="SanlogLoggerOptions"/> instances change.</param>
        /// <exception cref="ArgumentNullException">One of the parameters is <see langword="null"/>.</exception>
        [ActivatorUtilitiesConstructor]
        public SanlogLoggerProvider([ServiceKey] object? serviceKey, SanlogLoggerProcessor writer, IOptionsMonitor<SanlogLoggerOptions> optionsMonitor)
            : this(writer, optionsMonitor?.CurrentValue ?? throw new ArgumentNullException(nameof(optionsMonitor)))
        {
            _serviceKey = serviceKey;
            _changeTokenRegistration = optionsMonitor.OnChange(OnChangeOptions);
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="SanlogLoggerProvider"/> class with the specified writer service amd logger options.
        /// </summary>
        /// <param name="writer">The writer service. The caller is responsible for disposing of the writer.</param>
        /// <param name="options">The logger options.</param>
        /// <exception cref="ArgumentNullException">One of the parameters is <see langword="null"/>.</exception>
        public SanlogLoggerProvider(SanlogLoggerProcessor writer, SanlogLoggerOptions options)
        {
            _writer = writer ?? throw new ArgumentNullException(nameof(writer));
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _loggers = new ConcurrentDictionary<string, SanlogLogger>(StringComparer.OrdinalIgnoreCase);
        }

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
        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _loggers.Clear();
                    _changeTokenRegistration?.Dispose();
                    _writer.Dispose();
                }
                _disposedValue = true;
            }
        }
        /// <inheritdoc/>
        public async ValueTask DisposeAsync()
        {
            _loggers.Clear();
            _changeTokenRegistration?.Dispose();
            await _writer.DisposeAsync().ConfigureAwait(false);
            Dispose(false);
            GC.SuppressFinalize(this);
        }
        /// <inheritdoc/>
        /// <exception cref="ArgumentNullException">The <paramref name="categoryName"/> is <see langword="null"/>.</exception>
        /// <exception cref="ObjectDisposedException">The logger provider is disposed.</exception>
        public ILogger CreateLogger(string categoryName) => _loggers.GetOrAdd(categoryName, category =>
        {
            ObjectDisposedException.ThrowIf(_disposedValue, this);
            var logger = new SanlogLogger(category, _writer, () => _options);
            logger.SetScopeProvider(_externalScopeProvider);
            return logger;
        });
        /// <summary>
        /// The action to be invoked when <see cref="SanlogLoggerOptions"/> has changed.
        /// </summary>
        /// <param name="options">The changed logger options.</param>
        /// <param name="name">The name of the options.</param>
        /// <exception cref="ArgumentNullException">The <paramref name="options"/> is <see langword="null"/>.</exception>
        private void OnChangeOptions(SanlogLoggerOptions options, string? name)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }
        /// <inheritdoc/>
        public void SetScopeProvider(IExternalScopeProvider? scopeProvider)
        {
            _externalScopeProvider = scopeProvider;
            foreach (var logger in _loggers.Values) logger.SetScopeProvider(_externalScopeProvider);
        }
    }
}