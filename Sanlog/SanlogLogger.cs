﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace Sanlog
{
    /// <summary>
    /// Represents a type used to perform logging.
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of the <see cref="SanlogLogger"/> class with the specified category for messages produced by the logger, the function to transfer log entry, and the function to get the current logger configuration.
    /// </remarks>
    /// <param name="category">The category for messages produced by the logger.</param>
    /// <param name="provider">The function to transfer log entry.</param>
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
                var formattedLogValuesFormatter = new FormattedLogValuesFormatter(state is IReadOnlyList<KeyValuePair<string, object?>> list ? list.ToDictionary() : [])
                {
                    SensitiveConfiguration = options.SensitiveConfiguration
                };
                var logEntryId = Guid.NewGuid();
                var loggingEntry = new LoggingEntry
                {
                    TenantId = options.TenantId,
                    Id = logEntryId,
                    AppId = options.AppId,
                    Version = options.OnRetrieveVersion?.Invoke(),
                    DateTime = DateTime.Now,
                    LoggingLevelId = (int)logLevel,
                    Category = _category,
                    EventId = eventId.Id,
                    EventName = eventId.Name,
                    Message = formattedLogValuesFormatter.ContainsKey(FormattedLogValuesFormatter.OriginalFormat) ? formattedLogValuesFormatter.ToString() : formatter.Invoke(state, exception),
                    Properties = formattedLogValuesFormatter.ToStringDictionary(),
                    Scopes = GetScopeInformation(CultureInfo.InvariantCulture, state, logEntryId, options, _provider.ExternalScopeProvider),
                    Errors = exception is not null
                        ? exception is not AggregateException aggregateException
                            ? [GetErrorInformation(options, Guid.NewGuid(), exception, logEntryId, null)]
                            : aggregateException.Flatten().InnerExceptions.Select(innerException => GetErrorInformation(options, Guid.NewGuid(), innerException, logEntryId, null)).ToList()
                        : []
                };
                _provider.AddMessage(loggingEntry);
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
                    Type = exception.GetType().FullName!,
                    Message = exception.Message,
                    HResult = exception.HResult,
                    Data = ParseDictionary(exception.Data),
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

                static Dictionary<string, string?>? ParseDictionary(IDictionary dictionary)
                {
                    Debug.Assert(dictionary is not null);
                    if (dictionary.Count == 0)
                        return null;
                    var index = 0;
                    var userData = new Dictionary<string, string?>(dictionary.Count);
                    var formatter = new FormattedLogValuesFormatter(dictionary.Values.Cast<object?>());
                    foreach (DictionaryEntry entry in dictionary)
                    {
                        var newKey = entry.Key.ToString();
                        if (!string.IsNullOrEmpty(newKey))
                        {
                            var newValue = formatter.GetObjectAsString(index, true).Value;
                            userData.Add(newKey, newValue);
                        }
                        ++index;
                    }
                    return userData;
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
                            var formattedLogValuesFormatter = new FormattedLogValuesFormatter(scope is IReadOnlyList<KeyValuePair<string, object?>> list ? list.ToDictionary() : [])
                            {
                                SensitiveConfiguration = options.SensitiveConfiguration
                            };
                            var loggingScope = new LoggingScope
                            {
                                TenantId = options.TenantId,
                                Id = Guid.NewGuid(),
                                Type = scope.GetType().FullName!,
                                Message = formattedLogValuesFormatter.ContainsKey(FormattedLogValuesFormatter.OriginalFormat) ? formattedLogValuesFormatter.ToString() : Convert.ToString(scope, formatProvider),
                                LogEntryId = logEntryId,
                                Properties = formattedLogValuesFormatter.ToStringDictionary()
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