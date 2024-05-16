using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
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
        /// The category name for messages produced by the logger.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly string _categoryName;
        /// <summary>
        /// The event writer.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly ILoggingWriter _eventWriter;
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
        public SanlogLogger(string categoryName, ILoggingWriter eventWriter, SanlogLoggerOptions options)
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
            if (IsEnabled(logLevel))
            {
                ArgumentNullException.ThrowIfNull(formatter);
                var keyValueData = new FormattedLogValuesFormatter(CultureInfo.InvariantCulture, state as IReadOnlyList<KeyValuePair<string, object?>>, () => formatter.Invoke(state, exception));
                keyValueData.RegisterSensitiveData(_options.SensitiveDataType);

                var logEntryId = Guid.NewGuid();
                var loggingEntry = new LoggingEntry
                {
                    Id = logEntryId,
                    ApplicationId = _options.AppId,
                    Version = _options.OnRetrieveVersion?.Invoke(),
                    DateTime = DateTime.UtcNow,
                    LogLevelId = (int)logLevel,
                    Category = _categoryName,
                    EventId = eventId.Id,
                    EventName = eventId.Name,
                    Message = keyValueData.ToString(),
                    Properties = keyValueData.Select(property => new LoggingEntryProperty
                    {
                        Id = Guid.NewGuid(),
                        Key = property.Key,
                        Value = property.Value,
                        LogEntryId = logEntryId
                    }).ToList(),
                    Errors = exception is not null
                        ? exception is not AggregateException aggregateException
                            ? [GetErrorInformation(Guid.NewGuid(), exception, logEntryId, null)]
                            : aggregateException.Flatten().InnerExceptions.Select(innerException => GetErrorInformation(Guid.NewGuid(), innerException, logEntryId, null)).ToList()
                        : [],
                    Scopes = GetScopeInformation(CultureInfo.InvariantCulture, state, logEntryId, _options, _externalScopeProvider)
                };
                if (SynchronizationContext.Current == null && TaskScheduler.Current == TaskScheduler.Default)
                {
                    _eventWriter.Write(loggingEntry);
                }
                else
                {
                    _ = Task.Run(() => _eventWriter.Write(loggingEntry));
                }
            }

            // Summary: Gets error information
            [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code",
                Justification = "TargetSize property has a remark that Exception.TargetSite metadata might be incomplete or removed")]
            static LoggingError GetErrorInformation(Guid id, Exception exception, Guid logEntryId, Guid? parentErrorId)
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
                    TargetSite = exception.TargetSite?.ToString(), // IL2026
                    LogEntryId = logEntryId,
                    ParentExceptionId = parentErrorId,
                    InnerException = exception.InnerException is not null ? [GetErrorInformation(Guid.NewGuid(), exception.InnerException, logEntryId, id)] : []
                };
            }
            // Summary: Gets scope information
            static List<LoggingScope> GetScopeInformation(IFormatProvider? formatProvider, TState state, Guid logEntryId, SanlogLoggerOptions options, IExternalScopeProvider? externalScopeProvider)
            {
                var scopes = new List<LoggingScope>();
                if (options.IncludeScopes && externalScopeProvider is not null)
                {
                    externalScopeProvider.ForEachScope((scope, _) =>
                    {
                        if (scope is not null)
                        {
                            var keyValueData = new FormattedLogValuesFormatter(formatProvider, scope as IReadOnlyList<KeyValuePair<string, object?>>, () => Convert.ToString(scope, formatProvider));
                            keyValueData.RegisterSensitiveData(options.SensitiveDataType);

                            var scopeId = Guid.NewGuid();
                            var loggingScope = new LoggingScope
                            {
                                Id = scopeId,
                                Type = scope.GetType().FullName!,
                                Message = keyValueData.ToString(),
                                LogEntryId = logEntryId,
                                Properties = keyValueData.Select(property => new LoggingScopeProperty
                                {
                                    Id = Guid.NewGuid(),
                                    Key = property.Key,
                                    Value = property.Value,
                                    ScopeId = scopeId
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