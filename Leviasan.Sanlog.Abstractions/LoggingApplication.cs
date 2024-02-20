using System;
using System.Collections.Generic;

namespace Leviasan.Sanlog
{
    /// <summary>
    /// Represents information about the application.
    /// </summary>
    public sealed class LoggingApplication
    {
        /// <summary>
        /// Gets the object identifier.
        /// </summary>
        public Guid Id { get; init; }
        /// <summary>
        /// Gets the application name.
        /// </summary>
        public required string Application { get; init; }
        /// <summary>
        /// Gets the application environment.
        /// </summary>
        public required string Environment { get; init; }
        /// <summary>
        /// Gets the logging entries with the current application.
        /// </summary>
        public IReadOnlyList<LoggingEntry>? LogEntries { get; init; }
    }
}