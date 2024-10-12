using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Sanlog
{
    /// <summary>
    /// Represents the external scope data.
    /// </summary>
    public sealed record class LoggingScope
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
        /// The fully qualified name of the scope type.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly string? _type;
        /// <summary>
        /// The message that describes the current scope.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly string? _message;
        /// <summary>
        /// The collection that provides scope properties.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly IReadOnlyDictionary<string, string?>? _properties;
        /// <summary>
        /// The logging entry identifier.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly Guid _logEntryId;

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
        /// Gets the fully qualified name of the scope type.
        /// </summary>
        /// <exception cref="ArgumentException">The setter value is empty string.</exception>
        /// <exception cref="ArgumentNullException">The setter value is <see langword="null"/>.</exception>
        public string Type
        {
            get => _type ?? string.Empty;
            init
            {
                ArgumentException.ThrowIfNullOrEmpty(value, nameof(Type));
                _type = value;
            }
        }
        /// <summary>
        /// Gets a message that describes the current scope.    
        /// </summary>
        public string? Message
        {
            get => _message;
            init => _message = value;
        }
        /// <summary>
        /// Gets a collection that provides scope properties.
        /// </summary>
        public IReadOnlyDictionary<string, string?>? Properties
        {
            get => _properties;
            init => _properties = value;
        }
        /// <summary>
        /// Gets the logging entry identifier.
        /// </summary>
        /// <exception cref="ArgumentException">The setter value is <see cref="Guid.Empty"/>.</exception>
        public Guid LogEntryId
        {
            get => _logEntryId;
            init
            {
                if (value == Guid.Empty)
                    throw new ArgumentException("The value is 00000000-0000-0000-0000-000000000000.", nameof(LogEntryId));
                _logEntryId = value;
            }
        }
    }
}