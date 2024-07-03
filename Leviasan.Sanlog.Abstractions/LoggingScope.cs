using System;
using System.Collections.Generic;

namespace Leviasan.Sanlog
{
    /// <summary>
    /// Represents the external scope data.
    /// </summary>
    public sealed record class LoggingScope
    {
        /// <summary>
        /// Gets the object identifier.
        /// </summary>
        public Guid Id { get; init; }
        /// <summary>
        /// Gets the fully qualified name of the scope type.
        /// </summary>
        public required string Type { get; init; }
        /// <summary>
        /// Gets a message that describes the current scope.
        /// </summary>
        public string? Message { get; init; }
        /// <summary>
        /// Gets a collection that provides scope properties.
        /// </summary>
        public IReadOnlyList<LoggingScopeProperty>? Properties { get; init; }
        /// <summary>
        /// Gets the logging entry identifier.
        /// </summary>
        public Guid LogEntryId { get; init; }
        /// <summary>
        /// Gets the logging entry.
        /// </summary>
        public LoggingEntry? LogEntry { get; init; }
    }
}