using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Leviasan.Sanlog
{
    /// <summary>
    /// Represents the information about runtime error.
    /// </summary>
    public sealed record class LoggingError
    {
        /// <summary>
        /// The multitenant identifier.
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
        /// The message that describes the current exception.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly string? _message;
        /// <summary>
        /// The coded numerical value that is assigned to a specific exception.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly int _hResult;
        /// <summary>
        /// The tring representation of the immediate frames on the call stack.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly string? _stackTrace;
        /// <summary>
        /// The name of the application or the object that causes the exception.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly string? _source;
        /// <summary>
        /// The link to the help file associated with this exception.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly string? _helpLink;
        /// <summary>
        /// The method that throws the current exception.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly string? _targetSite;
        /// <summary>
        /// The logging entry identifier.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly Guid _logEntryId;
        /// <summary>
        /// The parent exception identifier.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly Guid? _parentExceptionId;
        /// <summary>
        /// The error instances that caused the current error.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private IReadOnlyList<LoggingError>? _innerException;

        /// <summary>
        /// Gets the multitenant identifier.
        /// </summary>
        public Guid TenantId => _tenantId;
        /// <summary>
        /// Gets the object identifier.
        /// </summary>
        /// <exception cref="ArgumentException">The setter value is <see cref="Guid.Empty"/>.</exception>
        public Guid Id
        {
            get
            {
                return _id;
            }
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
            get
            {
                return _type ?? string.Empty;
            }
            init
            {
                ArgumentException.ThrowIfNullOrEmpty(value, nameof(Type));
                _type = value;
            }
        }
        /// <summary>
        /// Gets a message that describes the current exception.
        /// </summary>
        public string? Message
        {
            get => _message;
            init => _message = value;
        }
        /// <summary>
        /// Gets the coded numerical value that is assigned to a specific exception.
        /// </summary>
        public int HResult
        {
            get => _hResult;
            init => _hResult = value;
        }
        /// <summary>
        /// Gets a string representation of the immediate frames on the call stack.
        /// </summary>
        public string? StackTrace
        {
            get => _stackTrace;
            init => _stackTrace = value;
        }
        /// <summary>
        /// Gets the name of the application or the object that causes the exception.
        /// </summary>
        public string? Source
        {
            get => _source;
            init => _source = value;
        }
        /// <summary>
        /// Gets a link to the help file associated with this exception.
        /// </summary>
        public string? HelpLink 
        {
            get => _helpLink;
            init => _helpLink = value; 
        }
        /// <summary>
        /// Gets the method that throws the current exception.
        /// </summary>
        /// <remarks><see cref="Exception.TargetSite"/> metadata might be incomplete or removed.</remarks>
        public string? TargetSite
        {
            get => _targetSite;
            init => _targetSite = value;
        }
        /// <summary>
        /// Gets the logging entry identifier.
        /// </summary>
        /// <exception cref="ArgumentException">The setter value is <see cref="Guid.Empty"/>.</exception>
        public Guid LogEntryId
        {
            get
            {
                return _logEntryId;
            }
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
        public Guid? ParentExceptionId
        {
            get => _parentExceptionId;
            init => _parentExceptionId = value;
        }
        /// <summary>
        /// Gets the error instances that caused the current error.
        /// </summary>
        public IReadOnlyList<LoggingError>? InnerException
        {
            get => _innerException;
            init => _innerException = value;
        }
    }
}