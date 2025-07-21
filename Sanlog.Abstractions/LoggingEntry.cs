using System;
using System.Collections.Generic;

namespace Sanlog
{
    /// <summary>
    /// Holds the information for a single log entry.
    /// </summary>
    public sealed record class LoggingEntry
    {
        /// <summary>
        /// Gets the object identifier.
        /// </summary>
        public Guid Id { get; init; }
        /// <summary>
        /// Gets the application identifier.
        /// </summary>
        public Guid AppId { get; init; }
        /// <summary>
        /// Gets the date and time when the event occurred.
        /// </summary>
        public DateTimeOffset Timestamp { get; init; }
        /// <summary>
        /// Gets the application version in which the event occurred.
        /// </summary>
        public Version? Version { get; init; }
        /// <summary>
        /// Gets the logging level identfier.
        /// </summary>
        public int LoggingLevelId { get; init; }
        /// <summary>
        /// Gets the logging category.
        /// </summary>
        public string? Category { get; init; }
        /// <summary>
        /// Gets the event identifier.
        /// </summary>
        public int EventId { get; init; }
        /// <summary>
        /// Gets the name of the event.
        /// </summary>
        public string? EventName { get; init; }
        /// <summary>
        /// Gets a message that describes the current logging entry.
        /// </summary>
        public string? Message { get; init; }
        /// <summary>
        /// Gets a collection that provides logging entry properties.
        /// </summary>
        public Dictionary<string, string?>? Properties { get; init; }
        /// <summary>
        /// Gets a collection that provides external scope data.
        /// </summary>
        public IReadOnlyList<LoggingScope>? Scopes { get; init; }
        /// <summary>
        /// Gets the exception list of the current logging entry.
        /// </summary>
        public IReadOnlyList<LoggingError>? Errors { get; init; }
        /// <summary>
        /// Gets the tenant identifier.
        /// </summary>
        public Guid TenantId { get; init; }
    }
}