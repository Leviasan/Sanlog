using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Leviasan.Sanlog
{
    /// <summary>
    /// Represents a type used to perform logging.
    /// </summary>
    public sealed class SanlogLogger : ILogger, ISupportExternalScope
    {
        /// <summary>
        /// The message format that represents a null value.
        /// </summary>
        public const string NullFormat = "[null]";
        /// <summary>
        /// The name of the property that contains an exception that occurred during serialization.
        /// </summary>
        public const string DeserializeException = "{DeserializeException}";
        /// <summary>
        /// The structured logging message.
        /// </summary>
        public const string OriginalFormat = "{OriginalFormat}";

        /// <summary>
        /// The category name for messages produced by the logger.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly string _categoryName;
        /// <summary>
        /// The event writer.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly IEventWriter _eventWriter;
        /// <summary>
        /// The logger options.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private SanlogLoggerOptions _options;
        /// <summary>
        /// The external storage of the common scope data.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private IExternalScopeProvider? _externalScopeProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="SanlogLogger"/> class with the specified category name for messages produced by the logger, the event writer, and the logger options.
        /// </summary>
        /// <param name="categoryName">The category name for messages produced by the logger.</param>
        /// <param name="eventWriter">The event writer.</param>
        /// <param name="options">The logger options.</param>
        /// <exception cref="ArgumentNullException">The <paramref name="categoryName"/> or <paramref name="eventWriter"/> or <paramref name="options"/> is <see langword="null"/>.</exception>
        public SanlogLogger(string categoryName, IEventWriter eventWriter, SanlogLoggerOptions options)
        {
            _categoryName = categoryName ?? throw new ArgumentNullException(nameof(categoryName));
            _eventWriter = eventWriter ?? throw new ArgumentNullException(nameof(eventWriter));
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        /// <inheritdoc/>
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => _externalScopeProvider?.Push(state);
        /// <inheritdoc/>
        public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None && _options.AppId != Guid.Empty;
        /// <inheritdoc/>
        /// <exception cref="ArgumentNullException">The <paramref name="formatter"/> is <see langword="null"/>.</exception>
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            ArgumentNullException.ThrowIfNull(formatter);
            if (_options.SkipEnabledCheck || IsEnabled(logLevel))
            {
                var logEntryId = Guid.NewGuid();
                var loggingEntry = new LoggingEntry
                {
                    Id = logEntryId,
                    AppicationId = _options.AppId,
                    Version = _options.OnRetrieveVersion?.Invoke(),
                    DateTime = DateTime.UtcNow,
                    LogLevelId = (int)logLevel,
                    Category = _categoryName,
                    EventId = eventId.Id,
                    EventName = eventId.Name,
                    Message = formatter.Invoke(state, exception) is string message && message != NullFormat ? message : exception?.Message,
                    Properties = ExtractProperties(state, _options).Select(property => new LoggingEntryProperty
                    {
                        Type = property.Key,
                        Message = property.Value,
                        LogEntryId = logEntryId
                    }).ToList(),
                    Errors = exception is not null
                        ? exception is not AggregateException aggregateException
                            ? [GetErrorInformation(Guid.NewGuid(), exception, logEntryId, default, _options.JsonSerializerOptions)]
                            : aggregateException.Flatten().InnerExceptions.Select(innerException => GetErrorInformation(Guid.NewGuid(), innerException, logEntryId, default, _options.JsonSerializerOptions)).ToList()
                        : [],
                    Scopes = GetScopeInformation(state, logEntryId, _options, _externalScopeProvider)
                };
                if (SynchronizationContext.Current == null && TaskScheduler.Current == TaskScheduler.Default)
                {
                    try
                    {
                        var valueTask = _eventWriter.WriteAsync(loggingEntry, CancellationToken.None);
                        valueTask.ConfigureAwait(false).GetAwaiter().GetResult();
                    }
                    catch (Exception)
                    {
                        if (!_options.SuppressThrowing) throw;
                    }
                }
                else
                {
                    Task.Run(async () => await _eventWriter.WriteAsync(loggingEntry, CancellationToken.None).ConfigureAwait(false))
                        .ConfigureAwait(_options.SuppressThrowing ? ConfigureAwaitOptions.SuppressThrowing : ConfigureAwaitOptions.None)
                        .GetAwaiter()
                        .GetResult();
                }
            }

            static KeyValuePair<string, string?>[] ExtractProperties<TValue>(TValue state, SanlogLoggerOptions options)
            {
                return (state as IEnumerable<KeyValuePair<string, object?>>)?
                    .Select(x => new KeyValuePair<string, string?>(x.Key, x.Value?.ToString()))
                    .Where(x => !options.IgnorePropertyKeys?.Contains(x.Key) ?? true)
                    .ToArray() ?? [];
            }
            [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "The caught exception is written to the logger")]
            [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code",
                Justification = "TargetSize property has a remark that Exception.TargetSite metadata might be incomplete or removed")]
            static LoggingError GetErrorInformation(Guid id, Exception exception, Guid logEntryId, Guid? parentErrorId, JsonSerializerOptions jsonSerializerOptions)
            {
                return new LoggingError
                {
                    Id = id,
                    Type = exception.GetType().FullName!,
                    Message = exception.Message,
                    HResult = exception.HResult,
                    StackTrace = exception.StackTrace,
                    Source = exception.Source,
                    HelpLink = exception.HelpLink,
                    TargetSite = exception.TargetSite?.ToString(),
                    Data = SerializeToDictionary(exception.Data, jsonSerializerOptions),
                    Properties = SerializeToDictionary(exception, jsonSerializerOptions),
                    LogEntryId = logEntryId,
                    ParentExceptionId = parentErrorId,
                    InnerException = exception.InnerException is not null ? [GetErrorInformation(Guid.NewGuid(), exception.InnerException, logEntryId, id, jsonSerializerOptions)] : []
                };
                static IReadOnlyDictionary<string, string?> SerializeToDictionary<TValue>(TValue value, JsonSerializerOptions options)
                {
                    try
                    {
                        Debug.Assert(value is not null);
                        var typeinfo = options.GetTypeInfo(value.GetType());
                        var json = JsonSerializer.Serialize(value, typeinfo);
                        using var jsonDocument = JsonDocument.Parse(json);
                        return jsonDocument.ToStringDictionary();
                    }
                    catch (Exception innerException)
                    {
                        return new Dictionary<string, string?>() { { DeserializeException, $"{innerException.GetType().FullName!}: {innerException.Message}" } };
                    }
                }
            }
            static List<LoggingScope> GetScopeInformation(TState state, Guid logEntryId, SanlogLoggerOptions options, IExternalScopeProvider? externalScopeProvider)
            {
                var scopes = new List<LoggingScope>();
                if (options.IncludeScopes && externalScopeProvider is not null)
                {
                    externalScopeProvider.ForEachScope((scope, _) =>
                    {
                        if (scope is not null)
                        {
                            var loggingScope = new LoggingScope
                            {
                                Type = scope.GetType().FullName!,
                                Message = scope.ToString(),
                                LogEntryId = logEntryId,
                                Properties = ExtractProperties(scope, options).Select(property => new LoggingScopeProperty
                                {
                                    Type = property.Key,
                                    Message = property.Value
                                }).ToList()
                            };
                            scopes.Add(loggingScope);
                        }
                    }, state);
                }
                return scopes;
            }
        }
        /// <inheritdoc/>
        public void SetScopeProvider(IExternalScopeProvider? scopeProvider) => _externalScopeProvider = scopeProvider;
        /// <summary>
        /// Sets the logger options.
        /// </summary>
        /// <param name="options">The logger options.</param>
        /// <exception cref="ArgumentNullException">The <paramref name="options"/> is <see langword="null"/>.</exception>
        internal void SetLoggerOptions(SanlogLoggerOptions options) => _options = options ?? throw new ArgumentNullException(nameof(options));
    }
}