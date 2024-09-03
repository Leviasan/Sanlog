using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace Leviasan.Sanlog
{
    /// <summary>
    /// Represents a type used to perform logging.
    /// </summary>
    public sealed class SanlogLogger : ILogger, ISupportExternalScope
    {
        /// <summary>
        /// The category for messages produced by the logger.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly string _category;
        /// <summary>
        /// The writer service.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly SanlogLoggerProcessor _processor;
        /// <summary>
        /// The function to get the current logger configuration.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly Func<SanlogLoggerOptions> _configure;
        /// <summary>
        /// The external storage of the common scope data.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private IExternalScopeProvider? _externalScopeProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="SanlogLogger"/> class with the specified category for messages produced by the logger, the writer service, and the function to get the current logger configuration.
        /// </summary>
        /// <param name="category">The category for messages produced by the logger.</param>
        /// <param name="processor">The writer service. The caller is responsible for disposing of the writer.</param>
        /// <param name="configure">The function to get the current logger configuration.</param>
        /// <exception cref="ArgumentNullException">One of the parameters is <see langword="null"/>.</exception>
        public SanlogLogger(string category, SanlogLoggerProcessor processor, Func<SanlogLoggerOptions> configure)
        {
            _category = category ?? throw new ArgumentNullException(nameof(category));
            _processor = processor ?? throw new ArgumentNullException(nameof(processor));
            _configure = configure ?? throw new ArgumentNullException(nameof(configure));
        }

        /// <inheritdoc/>
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => _externalScopeProvider?.Push(state);
        /// <inheritdoc/>
        public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None && _configure.Invoke().AppId != Guid.Empty;
        /// <inheritdoc/>
        /// <exception cref="ArgumentNullException">The <paramref name="formatter"/> is <see langword="null"/>.</exception>
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (IsEnabled(logLevel))
            {
                /*
                ArgumentNullException.ThrowIfNull(formatter);
                var options = _configure.Invoke();
                var formattedLogValuesFormatter = new FormattedLogValuesFormatter(state as IReadOnlyList<KeyValuePair<string, object?>>, CultureInfo.InvariantCulture);
                _ = options.SensitiveData.CopyTo(formattedLogValuesFormatter.SensitiveConfiguration);

                var logEntryId = Guid.NewGuid();
                var loggingEntry = new LoggingEntry
                {
                    Id = logEntryId,
                    ApplicationId = options.AppId,
                    Version = options.OnRetrieveVersion?.Invoke(),
                    DateTime = DateTime.UtcNow,
                    LogLevelId = (int)logLevel,
                    Category = _category,
                    EventId = eventId.Id,
                    EventName = eventId.Name,
                    Message = formattedLogValuesFormatter.ContainsKey(FormattedLogValuesFormatter.OriginalFormat) ? formattedLogValuesFormatter.ToString() : formatter.Invoke(state, exception),
                    Properties = formattedLogValuesFormatter.Select(property => new LoggingEntryProperty
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
                    Scopes = GetScopeInformation(CultureInfo.InvariantCulture, state, logEntryId, options, _externalScopeProvider)
                };
                _ = _processor.Enqueue(loggingEntry);
                */
            }

            // Summary: Gets error information.
            // Param (id): The identifier of the new object.
            // Param (exception): The exception to get error information.
            // Param (logEntryId): The parent logging entry identifier.
            // Param (parentErrorId): The parent error identifier.
            // Returns: The error information.
            [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "TargetSite metadata might be incomplete or removed")]
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
            // Summary: Gets scope information.
            // Param (formatProvider): An object that supplies culture-specific formatting information.
            // Param (state): An object that describes scope.
            // Param (logEntryId): The parent logging entry identifier.
            // Param (options): The logger options.
            // Param (externalScopeProvider): The external storage of the common scope data.
            // Returns: An array of the scope data.
#pragma warning disable CS8321 // Local function is declared but never used
            static List<LoggingScope> GetScopeInformation(IFormatProvider? formatProvider, TState state, Guid logEntryId, SanlogLoggerOptions options, IExternalScopeProvider? externalScopeProvider)
            {
                var scopes = new List<LoggingScope>();
                if (options.IncludeScopes && externalScopeProvider is not null)
                {
                    externalScopeProvider.ForEachScope((scope, __) =>
                    {
                        if (scope is not null)
                        {
                            /*
                            var formattedLogValuesFormatter = new FormattedLogValuesFormatter(scope as IReadOnlyList<KeyValuePair<string, object?>>, formatProvider);
                            _ = options.SensitiveData.CopyTo(formattedLogValuesFormatter.SensitiveConfiguration);

                            var scopeId = Guid.NewGuid();
                            var loggingScope = new LoggingScope
                            {
                                Id = scopeId,
                                Type = scope.GetType().FullName!,
                                Message = formattedLogValuesFormatter.ContainsKey(FormattedLogValuesFormatter.OriginalFormat) ? formattedLogValuesFormatter.ToString() : Convert.ToString(scope, formatProvider),
                                LogEntryId = logEntryId,
                                Properties = formattedLogValuesFormatter.Select(property => new LoggingScopeProperty
                                {
                                    Id = Guid.NewGuid(),
                                    Key = property.Key,
                                    Value = property.Value,
                                    ScopeId = scopeId
                                }).ToList()
                            };
                            scopes.Add(loggingScope);
                            */
                        }
                    }, state);
                }
                return scopes;
            }
#pragma warning restore CS8321 // Local function is declared but never used
        }
        /// <inheritdoc/>
        public void SetScopeProvider(IExternalScopeProvider? scopeProvider) => _externalScopeProvider = scopeProvider;
    }
}