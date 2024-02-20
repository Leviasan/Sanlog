using System;
using System.Collections.Generic;

namespace Leviasan.Sanlog
{
    /// <summary>
    /// Represents the information about runtime error.
    /// </summary>
    public sealed class LoggingError
    {
        /// <summary>
        /// Gets the object identifier.
        /// </summary>
        public Guid Id { get; init; }
        /// <summary>
        /// Gets the fully qualified name of the exception type.
        /// </summary>
        public required string Type { get; init; }
        /// <summary>
        /// Gets a message that describes the current exception.
        /// </summary>
        public string? Message { get; init; }
        /// <summary>
        /// Gets the coded numerical value that is assigned to a specific exception.
        /// </summary>
        public int HResult { get; init; }
        /// <summary>
        /// Gets a string representation of the immediate frames on the call stack.
        /// </summary>
        public string? StackTrace { get; init; }
        /// <summary>
        /// Gets the name of the application or the object that causes the exception.
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
        /// Gets a collection of key/value pairs that provide additional user-defined information about the exception.
        /// </summary>
        public IReadOnlyDictionary<string, string?>? Data { get; init; }
        /// <summary>
        /// Gets a collection of key/value pairs that provide additional exception-defined information.
        /// </summary>
        public IReadOnlyDictionary<string, string?>? Properties { get; init; }
        /// <summary>
        /// Gets the logging entry identifier.
        /// </summary>
        public Guid LogEntryId { get; init; }
        /// <summary>
        /// Gets the logging entry.
        /// </summary>
        public LoggingEntry? LogEntry { get; init; }
        /// <summary>
        /// Gets the parent exception identifier.
        /// </summary>
        public Guid? ParentExceptionId { get; init; }
        /// <summary>
        /// Gets the parent exception.
        /// </summary>
        public LoggingError? ParentException { get; init; }
        /// <summary>
        /// Gets the error instances that caused the current error.
        /// </summary>
        public IReadOnlyList<LoggingError>? InnerException { get; init; }
    }
}