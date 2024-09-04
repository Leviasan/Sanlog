namespace Leviasan.Sanlog
{
    /// <summary>
    /// Represents the property of the external scope data.
    /// </summary>
    public sealed record class LoggingScopeProperty : LoggingBaseProperty
    {
        /// <summary>
        /// Gets the external scope data.
        /// </summary>
        public LoggingScope? Scope { get; init; }
    }
}