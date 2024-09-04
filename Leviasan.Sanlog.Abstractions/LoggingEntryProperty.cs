using System;

namespace Leviasan.Sanlog
{
    /// <summary>
    /// Represents the message property of the logging entry.
    /// </summary>
    public sealed record class LoggingEntryProperty : LoggingBaseProperty
    {
        /// <summary>
        /// Gets the logging entry.
        /// </summary>
        public LoggingEntry? LogEntry { get; init; }
    }
}