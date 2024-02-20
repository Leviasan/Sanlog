using System;

namespace Leviasan.Sanlog
{
    /// <summary>
    /// Represents the message property of the logging entry.
    /// </summary>
    public sealed class LoggingEntryProperty
    {
        /// <summary>
        /// Gets the object identifier.
        /// </summary>
        public Guid Id { get; init; }
        /// <summary>
        /// Gets the property key.
        /// </summary>
        public required string Type { get; init; }
        /// <summary>
        /// Gets the property value.
        /// </summary>
        public string? Message { get; init; }
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