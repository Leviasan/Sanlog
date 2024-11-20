using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Sanlog
{
    /// <summary>
    /// Represents the information about runtime error.
    /// </summary>
    public sealed record class LoggingError
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
        /// The fully qualified name of the exception type.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly string? _type;
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
        /// Gets the fully qualified name of the exception type.
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
        /// Gets a message that describes the current exception.
        /// </summary>
        public string? Message { get; init; }
        /// <summary>
        /// Gets the coded numerical value that is assigned to a specific exception.
        /// </summary>
        public int HResult { get; init; }
        /// <summary>
        /// Gets a collection that provides additional user-defined information about the exception.
        /// </summary>
        public IReadOnlyDictionary<string, string?>? Data { get; init; }
        /// <summary>
        /// Gets a string representation of the immediate frames on the call stack.
        /// </summary>
        public string? StackTrace { get; init; }
        /// <summary>
        /// Gets the application's name or the object that causes the exception.
        /// </summary>
        public string? Source { get; init; }
        /// <summary>
        /// Gets a link to the help file associated with this exception.
        /// </summary>
        public string? HelpLink { get; init; }
        /// <summary>
        /// Gets the method that throws the current exception.
        /// </summary>
        /// <remarks><see cref="Exception.TargetSite"/> metadata might be incomplete or removed.</remarks>
        public string? TargetSite { get; init; }
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
        /// <summary>
        /// Gets the parent error identifier.
        /// </summary>
        public Guid? ParentExceptionId { get; init; }
        /// <summary>
        /// Gets the error instances that caused the current error.
        /// </summary>
        public IReadOnlyList<LoggingError>? InnerException { get; init; }
    }
}