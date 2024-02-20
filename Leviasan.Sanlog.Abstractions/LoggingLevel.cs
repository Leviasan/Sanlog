using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace Leviasan.Sanlog
{
    /// <summary>
    /// Represents the logging level.
    /// </summary>
    public sealed class LoggingLevel
    {
        /// <summary>
        /// Gets the object identifier.
        /// </summary>
        public int Id { get; init; }
        /// <summary>
        /// Gets the logging level name.
        /// </summary>
        public LogLevel Name { get; init; }
        /// <summary>
        /// Gets the logging entries with the current logging level.
        /// </summary>
        public IReadOnlyList<LoggingEntry>? LogEntries { get; init; }
    }
}