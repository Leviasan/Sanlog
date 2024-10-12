using System;
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
    public sealed class SanlogLogger : ILogger
    {
        /// <summary>
        /// The category for messages produced by the logger.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly string _category;
        /// <summary>
        /// The service retrieving details about the tenancy.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly ITenantService _tenantService;
        /// <summary>
        /// The logging entry writer service.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly SanlogLoggingWriter _writer;
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
        /// Initializes a new instance of the <see cref="SanlogLogger"/> class with the specified category for messages produced by the logger, the service for retrieving details about the tenancy, the writer service, and the function to get the current logger configuration.
        /// </summary>
        /// <param name="category">The category for messages produced by the logger.</param>
        /// <param name="tenantService">The service for retrieving details about the tenancy.</param>
        /// <param name="writer">The logging entry writer service. The caller is responsible for disposing of the writer.</param>
        /// <param name="configure">The function to get the current logger configuration.</param>
        /// <exception cref="ArgumentNullException">The <paramref name="category"/> or <paramref name="tenantService"/> or <paramref name="writer"/> or <paramref name="configure"/> is <see langword="null"/>.</exception>
        public SanlogLogger(string category, ITenantService tenantService, SanlogLoggingWriter writer, Func<SanlogLoggerOptions> configure)
        {
            _category = category ?? throw new ArgumentNullException(nameof(category));
            _tenantService = tenantService ?? throw new ArgumentNullException(nameof(tenantService));
            _writer = writer ?? throw new ArgumentNullException(nameof(writer));
            _configure = configure ?? throw new ArgumentNullException(nameof(configure));
        }

        /// <inheritdoc/>
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => _externalScopeProvider?.Push(state);
        /// <inheritdoc/>
        public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None && _tenantService.AppId != Guid.Empty && _tenantService.TenantId != Guid.Empty;
        /// <inheritdoc/>
        /// <exception cref="ArgumentNullException">The <paramref name="formatter"/> is <see langword="null"/>.</exception>
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            ArgumentNullException.ThrowIfNull(formatter);
            if (IsEnabled(logLevel))
            {
                var options = _configure.Invoke();
                var formattedLogValuesFormatter = new FormattedLogValuesFormatter(
                    dictionary: state is IReadOnlyList<KeyValuePair<string, object?>> list ? list.ToDictionary() : [],
                    configuration: options.SensitiveConfiguration);

                var logEntryId = Guid.NewGuid();
                var loggingEntry = new LoggingEntry
                {
                    TenantId = _tenantService.TenantId,
                    Id = logEntryId,
                    AppId = _tenantService.AppId,
                    Version = options.OnRetrieveVersion?.Invoke(),
                    DateTime = DateTime.UtcNow,
                    LogLevelId = logLevel,
                    Category = _category,
                    EventId = eventId.Id,
                    EventName = eventId.Name,
                    Message = formattedLogValuesFormatter.ContainsKey(FormattedLogValuesFormatter.OriginalFormat) ? formattedLogValuesFormatter.ToMessage() : formatter.Invoke(state, exception),
                    Properties = formattedLogValuesFormatter.GetProperties(),
                    Scopes = GetScopeInformation(_tenantService, CultureInfo.InvariantCulture, state, logEntryId, options, _externalScopeProvider),
                    Errors = exception is not null
                        ? exception is not AggregateException aggregateException
                            ? [GetErrorInformation(_tenantService, Guid.NewGuid(), exception, logEntryId, null)]
                            : aggregateException.Flatten().InnerExceptions.Select(innerException => GetErrorInformation(_tenantService, Guid.NewGuid(), innerException, logEntryId, null)).ToList()
                        : []
                };
                _ = _writer.Enqueue(loggingEntry);
            }
            [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "TargetSite metadata might be incomplete or removed")]
            static LoggingError GetErrorInformation(ITenantService tenantService, Guid id, Exception exception, Guid logEntryId, Guid? parentErrorId)
            {
                return new LoggingError
                {
                    TenantId = tenantService.TenantId,
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
                    InnerException = exception.InnerException is not null
                        ? exception.InnerException is not AggregateException aggregateException
                            ? [GetErrorInformation(tenantService, Guid.NewGuid(), exception.InnerException, logEntryId, id)]
                            : aggregateException.Flatten().InnerExceptions.Select(innerException => GetErrorInformation(tenantService, Guid.NewGuid(), innerException, logEntryId, id)).ToList()
                        : []
                };
            }
            static List<LoggingScope> GetScopeInformation(ITenantService tenantService, IFormatProvider? formatProvider, TState state, Guid logEntryId, SanlogLoggerOptions options, IExternalScopeProvider? externalScopeProvider)
            {
                var scopes = new List<LoggingScope>();
                if (options.IncludeScopes && externalScopeProvider is not null)
                {
                    externalScopeProvider.ForEachScope((scope, __) =>
                    {
                        if (scope is not null)
                        {
                            var formattedLogValuesFormatter = new FormattedLogValuesFormatter(
                                dictionary: scope is IReadOnlyList<KeyValuePair<string, object?>> list ? list.ToDictionary() : [],
                                configuration: options.SensitiveConfiguration);

                            var loggingScope = new LoggingScope
                            {
                                TenantId = tenantService.TenantId,
                                Id = Guid.NewGuid(),
                                Type = scope.GetType().FullName!,
                                Message = formattedLogValuesFormatter.ContainsKey(FormattedLogValuesFormatter.OriginalFormat) ? formattedLogValuesFormatter.ToMessage() : Convert.ToString(scope, formatProvider),
                                LogEntryId = logEntryId,
                                Properties = formattedLogValuesFormatter.GetProperties()
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
    }
}