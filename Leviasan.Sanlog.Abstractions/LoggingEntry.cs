using System;
using System.Collections.Generic;

namespace Leviasan.Sanlog
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
        /// Gets the date and time when the event occurred.
        /// </summary>
        public DateTime DateTime { get; init; }
        /// <summary>
        /// Gets the application identifier.
        /// </summary>
        public Guid ApplicationId { get; init; }
        /// <summary>
        /// Gets the application information.
        /// </summary>
        public LoggingApplication? Application { get; init; }
        /// <summary>
        /// Gets the application version in which the event occurred.
        /// </summary>
        public Version? Version { get; init; }
        /// <summary>
        /// Gets the logging level.
        /// </summary>
        public int LogLevelId { get; init; }
        /// <summary>
        /// Gets the logging level information.
        /// </summary>
        public LoggingLevel? LogLevel { get; init; }
        /// <summary>
        /// Gets the logging category.
        /// </summary>
        public required string Category { get; init; }
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
        public IReadOnlyDictionary<string, string?>? Properties { get; init; }
        /// <summary>
        /// Gets a collection that provides external scope data.
        /// </summary>
        public IReadOnlyList<LoggingScope>? Scopes { get; init; }
        /// <summary>
        /// Gets the exception list of the current logging entry.
        /// </summary>
        public IReadOnlyList<LoggingError>? Errors { get; init; }
    }
}