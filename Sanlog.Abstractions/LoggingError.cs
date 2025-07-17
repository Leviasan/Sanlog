using System;
using System.Collections.Generic;

namespace Sanlog
{
    /// <summary>
    /// Represents the information about runtime error.
    /// </summary>
    public sealed record class LoggingError
    {
        /// <summary>
        /// Gets the object identifier.
        /// </summary>
        public Guid Id { get; init; }
        /// <summary>
        /// Gets the fully qualified name of the exception type.
        /// </summary>
        public string? Type { get; init; }
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
        public Dictionary<string, string?>? Data { get; init; }
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
        public Guid LogEntryId { get; init; }
        /// <summary>
        /// Gets the parent error identifier.
        /// </summary>
        public Guid? ParentExceptionId { get; init; }
        /// <summary>
        /// Gets the error instances that caused the current error.
        /// </summary>
        public IReadOnlyList<LoggingError>? InnerException { get; init; }
        /// <summary>
        /// Gets the tenant identifier.
        /// </summary>
        public Guid TenantId { get; init; }
    }
}