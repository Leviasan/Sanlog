using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Threading.Channels;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.Extensions.Logging;

namespace Sanlog
{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    public interface IMessageHandler<TMessage>
    {
        void Handle(TMessage message);
    }
    public interface IAsyncMessageHandler<TMessage> : IMessageHandler<TMessage>
    {
        Task HandleAsync(TMessage message, CancellationToken cancellationToken);
    }
    public interface IMessageWorker<TMessage>
    {
        Task RunAsync(IAsyncMessageHandler<TMessage> handler, CancellationToken cancellationToken);
    }
    public interface IMessageBroker<TMessage> : IDisposable
    {
        IAsyncMessageHandler<TMessage> Producer { get; }
    }
    public sealed class ChannelMessageBroker : IMessageBroker<LoggingEntry>
    {
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly Channel<LoggingEntry> _channel;
        private readonly ConsumerMessageWorker _worker;
        private bool _disposedValue;

        public ChannelMessageBroker(Channel<LoggingEntry> channel, IAsyncMessageHandler<LoggingEntry> consumer)
        {
            ArgumentNullException.ThrowIfNull(channel);
            ArgumentNullException.ThrowIfNull(consumer);

            _channel = channel;
            _worker = new ConsumerMessageWorker(_channel);
            _cancellationTokenSource = new CancellationTokenSource();
            _ = _worker.RunAsync(consumer, _cancellationTokenSource.Token);
            Producer = new ProducerMessageHandler(_channel);
        }

        public IAsyncMessageHandler<LoggingEntry> Producer { get; }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _channel.Writer.Complete();
                    _cancellationTokenSource.Cancel();
                    _cancellationTokenSource.Dispose();
                }
                _disposedValue = true;
            }
        }

        private sealed class ConsumerMessageWorker(ChannelReader<LoggingEntry> reader) : IMessageWorker<LoggingEntry>
        {
            [DebuggerBrowsable(DebuggerBrowsableState.Never)]
            private readonly ChannelReader<LoggingEntry> _reader = reader ?? throw new ArgumentNullException(nameof(reader));

            [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Suppressing throwing exception while handle message")]
            public async Task RunAsync(IAsyncMessageHandler<LoggingEntry> handler, CancellationToken cancellationToken)
            {
                ArgumentNullException.ThrowIfNull(handler);
                while (await _reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false))
                {
                    while (_reader.TryRead(out var message))
                    {
                        try
                        {
                            await handler.HandleAsync(message, cancellationToken).ConfigureAwait(false);
                        }
                        catch
                        {
                            // ignored
                        }
                    }
                }
            }
        }
        private sealed class ProducerMessageHandler(ChannelWriter<LoggingEntry> writer) : IAsyncMessageHandler<LoggingEntry>
        {
            [DebuggerBrowsable(DebuggerBrowsableState.Never)]
            private readonly ChannelWriter<LoggingEntry> _writer = writer ?? throw new ArgumentNullException(nameof(writer));

            public void Handle(LoggingEntry message) => _writer.TryWrite(message);
            public Task HandleAsync(LoggingEntry message, CancellationToken cancellationToken) => _writer.WriteAsync(message, cancellationToken).AsTask();
        }
    }

#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member

    /// <summary>
    /// Represents a type used to perform logging.
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of the <see cref="SanlogLogger"/> class with the specified category for messages produced by the logger and logger provider.
    /// </remarks>
    /// <param name="category">The category for messages produced by the logger.</param>
    /// <param name="provider">The logger provider.</param>
    /// <exception cref="ArgumentNullException">The <paramref name="category"/> or <paramref name="provider"/> is <see langword="null"/>.</exception>
    internal sealed class SanlogLogger(string category, SanlogLoggerProvider provider) : ILogger
    {
        /// <summary>
        /// The category for messages produced by the logger.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly string _category = category ?? throw new ArgumentNullException(nameof(category));
        /// <summary>
        /// The logger provider.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly SanlogLoggerProvider _provider = provider ?? throw new ArgumentNullException(nameof(provider));

        /// <inheritdoc/>
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => _provider.ExternalScopeProvider?.Push(state);
        /// <inheritdoc/>
        public bool IsEnabled(LogLevel logLevel)
        {
            var options = _provider.Options;
            return logLevel != LogLevel.None && options.AppId != Guid.Empty && options.TenantId != Guid.Empty;
        }
        /// <inheritdoc/>
        /// <exception cref="ArgumentNullException">The <paramref name="formatter"/> is <see langword="null"/>.</exception>
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            ArgumentNullException.ThrowIfNull(formatter);
            if (IsEnabled(logLevel))
            {
                var options = _provider.Options;
                var formattedLogValuesFormatter = new FormattedLogValuesFormatter(state is IReadOnlyCollection<KeyValuePair<string, object?>> list ? list : [])
                {
                    SensitiveConfiguration = _provider.Options.SensitiveConfiguration,
                    FormattedConfiguration = _provider.Options.FormattedConfiguration
                };
                var logEntryId = Guid.NewGuid();
                var loggingEntry = new LoggingEntry
                {
                    TenantId = _provider.Options.TenantId,
                    Id = logEntryId,
                    AppId = _provider.Options.AppId,
                    Version = _provider.Options.OnRetrieveVersion?.Invoke(),
                    Timestamp = DateTime.Now,
                    LoggingLevelId = (int)logLevel,
                    Category = _category,
                    EventId = eventId.Id,
                    EventName = eventId.Name,
                    Message = formattedLogValuesFormatter.IndexOf(FormattedLogValuesFormatter.OriginalFormat) == -1 ? formatter.Invoke(state, exception) : formattedLogValuesFormatter.ToString(),
                    Properties = formattedLogValuesFormatter.Process(),
                    Scopes = GetScopeInformation(CultureInfo.InvariantCulture, state, logEntryId, options, _provider.ExternalScopeProvider),
                    Errors = exception is not null
                        ? exception is not AggregateException aggregateException
                            ? [GetErrorInformation(_provider.Options, Guid.NewGuid(), exception, logEntryId, null)]
                            : aggregateException.Flatten().InnerExceptions.Select(innerException => GetErrorInformation(_provider.Options, Guid.NewGuid(), innerException, logEntryId, null)).ToList()
                        : []
                };
                _provider.MessageBroker.Producer.Handle(loggingEntry);
            }

            [UnconditionalSuppressMessage("Trimming",
                "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code",
                Justification = "TargetSite metadata might be incomplete or removed")]
            static LoggingError GetErrorInformation(SanlogLoggerOptions options, Guid id, Exception exception, Guid logEntryId, Guid? parentErrorId)
            {
                return new LoggingError
                {
                    TenantId = options.TenantId,
                    Id = id,
                    Type = exception.GetType().FullName,
                    Message = exception.Message,
                    HResult = exception.HResult,
                    Data = ProcessIDictionary(exception.Data, options),
                    StackTrace = exception.StackTrace,
                    Source = exception.Source,
                    HelpLink = exception.HelpLink,
                    TargetSite = exception.TargetSite?.ToString(), // IL2026
                    LogEntryId = logEntryId,
                    ParentExceptionId = parentErrorId,
                    InnerException = exception.InnerException is not null
                        ? exception.InnerException is not AggregateException aggregateException
                            ? [GetErrorInformation(options, Guid.NewGuid(), exception.InnerException, logEntryId, id)]
                            : aggregateException.Flatten().InnerExceptions.Select(innerException => GetErrorInformation(options, Guid.NewGuid(), innerException, logEntryId, id)).ToList()
                        : []
                };

                static IReadOnlyList<KeyValuePair<string, string?>>? ProcessIDictionary(IDictionary dictionary, SanlogLoggerOptions options)
                {
                    if (dictionary.Count == 0)
                    {
                        return null;
                    }
                    var generic = new List<KeyValuePair<string, object?>>(dictionary.Count);
                    foreach (DictionaryEntry entry in dictionary)
                    {
                        var newKey = entry.Key.ToString();
                        if (!string.IsNullOrEmpty(newKey))
                            generic.Add(KeyValuePair.Create(newKey, entry.Value));
                    }
                    var formatter = new FormattedLogValuesFormatter(generic)
                    {
                        SensitiveConfiguration = options.SensitiveConfiguration,
                        FormattedConfiguration = options.FormattedConfiguration
                    };
                    return formatter.Process();
                }
            }
            static List<LoggingScope> GetScopeInformation(IFormatProvider? formatProvider, TState state, Guid logEntryId, SanlogLoggerOptions options, IExternalScopeProvider? externalScopeProvider)
            {
                var scopes = new List<LoggingScope>();
                if (options.IncludeScopes && externalScopeProvider is not null)
                {
                    externalScopeProvider.ForEachScope((scope, scopes) =>
                    {
                        if (scope is not null)
                        {
                            var formatter = new FormattedLogValuesFormatter(scope is IReadOnlyList<KeyValuePair<string, object?>> list ? list.ToDictionary() : [])
                            {
                                SensitiveConfiguration = options.SensitiveConfiguration,
                                FormattedConfiguration = options.FormattedConfiguration
                            };
                            var loggingScope = new LoggingScope
                            {
                                TenantId = options.TenantId,
                                Id = Guid.NewGuid(),
                                Type = scope.GetType().FullName,
                                Message = formatter.IndexOf(FormattedLogValuesFormatter.OriginalFormat) == -1 ? Convert.ToString(scope, formatProvider) : formatter.ToString(),
                                LogEntryId = logEntryId,
                                Properties = formatter.Process()
                            };
                            scopes.Add(loggingScope);
                        }
                    }, scopes);
                }
                return scopes;
            }
        }
    }
}