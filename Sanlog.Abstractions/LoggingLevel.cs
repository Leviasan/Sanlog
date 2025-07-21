using Microsoft.Extensions.Logging;

namespace Sanlog
{
    /// <summary>
    /// Represents the logging level.
    /// </summary>
    public sealed record class LoggingLevel
    {
        /// <summary>
        /// Gets the object identifier.
        /// </summary>
        public int Id { get; init; }
        /// <summary>
        /// Gets the logging level name.
        /// </summary>
        public LogLevel Name { get; init; }
    }
}