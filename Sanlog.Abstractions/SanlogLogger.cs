using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using Microsoft.Extensions.Logging;
using Sanlog.Formatters;

namespace Sanlog
{
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
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull
            => _provider.ExternalScopeProvider?.Push(state);
        /// <inheritdoc/>
        public bool IsEnabled(LogLevel logLevel)
            => logLevel != LogLevel.None && _provider.Options.AppId != Guid.Empty && _provider.Options.TenantId != Guid.Empty;
        /// <inheritdoc/>
        /// <exception cref="ArgumentNullException">The <paramref name="formatter"/> is <see langword="null"/>.</exception>
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            ArgumentNullException.ThrowIfNull(formatter);
            if (IsEnabled(logLevel))
            {
                Guid logEntryId = Guid.NewGuid();
                FormattedLogValues logValues = new(_provider.Formatter, state is IReadOnlyCollection<KeyValuePair<string, object?>> list ? list : []);
                LoggingEntry loggingEntry = new()
                {
                    TenantId = _provider.Options.TenantId,
                    Id = logEntryId,
                    AppId = _provider.Options.AppId,
                    Version = _provider.Options.OnRetrieveVersion?.Invoke(),
                    Timestamp = DateTimeOffset.Now,
                    LoggingLevelId = (int)logLevel,
                    Category = _category,
                    EventId = eventId.Id,
                    EventName = eventId.Name,
                    Message = logValues.HasOriginalFormat
                        ? logValues.ToString() // format message template
                        : formatter.Invoke(state, exception), // use default formatter
                    Properties = logValues.GroupByToDictionary(),
                    Scopes = GetScopeInformation(CultureInfo.InvariantCulture, state, logEntryId, _provider),
                    Errors = exception is not null
                        ? exception is not AggregateException aggregateException
                            ? [GetErrorInformation(exception, logEntryId, null, _provider)]
                            : aggregateException.Flatten().InnerExceptions.Select(innerException => GetErrorInformation(innerException, logEntryId, null, _provider)).ToList()
                        : []
                };
                _ = _provider.SendMessage(loggingEntry);
            }

            [UnconditionalSuppressMessage("Trimming",
                "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code",
                Justification = "TargetSite metadata might be incomplete or removed")]
            static LoggingError GetErrorInformation(Exception exception, Guid logEntryId, Guid? parentErrorId, SanlogLoggerProvider loggerProvider)
            {
                Guid id = Guid.NewGuid();
                return new LoggingError
                {
                    TenantId = loggerProvider.Options.TenantId,
                    Id = id,
                    Type = exception.GetType().FullName,
                    Message = exception.Message,
                    HResult = exception.HResult,
                    Data = GetExceptionDictionary(exception.Data, loggerProvider.Formatter),
                    StackTrace = exception.StackTrace,
                    Source = exception.Source,
                    HelpLink = exception.HelpLink,
                    TargetSite = exception.TargetSite?.ToString(), // IL2026
                    LogEntryId = logEntryId,
                    ParentExceptionId = parentErrorId,
                    InnerException = exception.InnerException is not null
                        ? exception.InnerException is not AggregateException aggregateException
                            ? [GetErrorInformation(exception.InnerException, logEntryId, id, loggerProvider)]
                            : aggregateException.Flatten().InnerExceptions.Select(innerException => GetErrorInformation(innerException, logEntryId, id, loggerProvider)).ToList()
                        : []
                };

                static Dictionary<string, string?>? GetExceptionDictionary(IDictionary dictionary, FormattedLogValuesFormatter formatter)
                {
                    if (dictionary.Count == 0)
                    {
                        return null;
                    }
                    List<KeyValuePair<string, object?>> collection = new(dictionary.Count);
                    foreach (DictionaryEntry entry in dictionary)
                    {
                        string? newKey = entry.Key.ToString();
                        if (!string.IsNullOrEmpty(newKey))
                            collection.Add(KeyValuePair.Create(newKey, entry.Value));
                    }
                    FormattedLogValues logValues = new(formatter, collection);
                    return logValues.GroupByToDictionary();
                }
            }
            static List<LoggingScope>? GetScopeInformation(IFormatProvider? formatProvider, TState state, Guid logEntryId, SanlogLoggerProvider loggerProvider)
            {
                if (loggerProvider.Options.IncludeScopes && loggerProvider.ExternalScopeProvider is not null)
                {
                    List<LoggingScope> scopes = [];
                    loggerProvider.ExternalScopeProvider.ForEachScope((scope, scopes) =>
                    {
                        if (scope is not null)
                        {
                            FormattedLogValues logValues = new(loggerProvider.Formatter, scope is IReadOnlyList<KeyValuePair<string, object?>> list ? list.ToDictionary() : []);
                            LoggingScope loggingScope = new()
                            {
                                TenantId = loggerProvider.Options.TenantId,
                                Id = Guid.NewGuid(),
                                Type = scope.GetType().FullName,
                                Message = logValues.HasOriginalFormat
                                    ? logValues.ToString() // format message template
                                    : Convert.ToString(scope, formatProvider), // use default formatter
                                LogEntryId = logEntryId,
                                Properties = logValues.GroupByToDictionary()
                            };
                            scopes.Add(loggingScope);
                        }
                    }, scopes);
                    return scopes;
                }
                return null;
            }
        }
    }
}