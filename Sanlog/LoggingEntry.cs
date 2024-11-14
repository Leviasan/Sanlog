using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Sanlog
{
    /// <summary>
    /// Holds the information for a single log entry.
    /// </summary>
    public sealed record class LoggingEntry
    {
        /// <summary>
        /// The tenant identifier.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly Guid _tenantId;
        /// <summary>
        /// The object identifier.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly Guid _id;
        /// <summary>
        /// The application identifier.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly Guid _appId;
        /// <summary>
        /// The date and time when the event occurred.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly DateTime _dateTime;
        /// <summary>
        /// The application version in which the event occurred.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly Version? _version;
        /// <summary>
        /// The logging level identifier.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly int _loggingLevelId;
        /// <summary>
        /// The logging category.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly string? _category;
        /// <summary>
        /// The event identifier.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly int _eventId;
        /// <summary>
        /// The event name.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly string? _eventName;
        /// <summary>
        /// The message that describes the current logging entry.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly string? _message;
        /// <summary>
        /// The collection that provides logging entry properties.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly IReadOnlyDictionary<string, string?>? _properties;
        /// <summary>
        /// The collection that provides external scope data.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly IReadOnlyList<LoggingScope>? _scopes;
        /// <summary>
        /// The exception list of the current logging entry.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly IReadOnlyList<LoggingError>? _errors;

        /// <summary>
        /// Gets the tenant identifier.
        /// </summary>
        /// <exception cref="ArgumentException">The setter value is <see cref="Guid.Empty"/>.</exception>
        public Guid TenantId
        {
            get => _tenantId;
            init
            {
                if (value == Guid.Empty)
                    throw new ArgumentException("The value is 00000000-0000-0000-0000-000000000000.", nameof(TenantId));
                _tenantId = value;
            }
        }
        /// <summary>
        /// Gets the object identifier.
        /// </summary>
        /// <exception cref="ArgumentException">The setter value is <see cref="Guid.Empty"/>.</exception>
        public Guid Id
        {
            get => _id;
            init
            {
                if (value == Guid.Empty)
                    throw new ArgumentException("The value is 00000000-0000-0000-0000-000000000000.", nameof(Id));
                _id = value;
            }
        }
        /// <summary>
        /// Gets the application identifier.
        /// </summary>
        /// <exception cref="ArgumentException">The setter value is <see cref="Guid.Empty"/>.</exception>
        public Guid AppId
        {
            get => _appId;
            init
            {
                if (value == Guid.Empty)
                    throw new ArgumentException("The value is 00000000-0000-0000-0000-000000000000.", nameof(AppId));
                _appId = value;
            }
        }
        /// <summary>
        /// Gets the date and time when the event occurred.
        /// </summary>
        public DateTime DateTime
        {
            get => _dateTime;
            init => _dateTime = value;
        }
        /// <summary>
        /// Gets the application version in which the event occurred.
        /// </summary>
        public Version? Version
        {
            get => _version;
            init => _version = value;
        }
        /// <summary>
        /// Gets the logging level identfier.
        /// </summary>
        /// <exception cref="ArgumentException">Passed <see cref="LogLevel.None"/> value that is not used for writing log messages. Specifies that a logging category should not write any messages.</exception>
        /// <exception cref="InvalidEnumArgumentException">The setter value is invalid enum.</exception>
        public int LoggingLevelId
        {
            get => _loggingLevelId;
            init
            {
                var objValue = (LogLevel)Enum.ToObject(typeof(LogLevel), value);
                if (objValue == LogLevel.None)
                    throw new ArgumentException("Not used for writing log messages. Specifies that a logging category should not write any messages.");
                if (!Enum.IsDefined(objValue))
                    throw new InvalidEnumArgumentException(nameof(LoggingLevelId), value, typeof(LogLevel));
                _loggingLevelId = value;
            }
        }
        /// <summary>
        /// Gets the logging category.
        /// </summary>
        /// <exception cref="ArgumentException">The setter value is empty string.</exception>
        /// <exception cref="ArgumentNullException">The setter value is <see langword="null"/>.</exception>
        public string Category
        {
            get => _category ?? string.Empty;
            init
            {
                ArgumentException.ThrowIfNullOrEmpty(value, nameof(Category));
                _category = value;
            }
        }
        /// <summary>
        /// Gets the event identifier.
        /// </summary>
        public int EventId
        {
            get => _eventId;
            init => _eventId = value;
        }
        /// <summary>
        /// Gets the name of the event.
        /// </summary>
        public string? EventName
        {
            get => _eventName;
            init => _eventName = value;
        }
        /// <summary>
        /// Gets a message that describes the current logging entry.
        /// </summary>
        public string? Message
        {
            get => _message;
            init => _message = value;
        }
        /// <summary>
        /// Gets a collection that provides logging entry properties.
        /// </summary>
        public IReadOnlyDictionary<string, string?>? Properties
        {
            get => _properties;
            init => _properties = value;
        }
        /// <summary>
        /// Gets a collection that provides external scope data.
        /// </summary>
        public IReadOnlyList<LoggingScope>? Scopes
        {
            get => _scopes;
            init => _scopes = value;
        }
        /// <summary>
        /// Gets the exception list of the current logging entry.
        /// </summary>
        public IReadOnlyList<LoggingError>? Errors
        {
            get => _errors;
            init => _errors = value;
        }
    }
}