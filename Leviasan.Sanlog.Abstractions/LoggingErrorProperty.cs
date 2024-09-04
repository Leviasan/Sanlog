namespace Leviasan.Sanlog
{
    /// <summary>
    /// 
    /// </summary>
    public sealed record class LoggingErrorProperty : LoggingBaseProperty
    {
        /// <summary>
        /// Gets the logging error.
        /// </summary>
        public LoggingError? Error { get; init; }
    }
}