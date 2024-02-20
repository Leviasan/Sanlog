using System;

namespace Leviasan.Sanlog
{
    /// <summary>
    /// Represents the property of the external scope data.
    /// </summary>
    public sealed class LoggingScopeProperty
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
        /// Gets the scope identifier.
        /// </summary>
        public Guid ScopeId { get; init; }
        /// <summary>
        /// Gets the external scope data.
        /// </summary>
        public LoggingScope? Scope { get; init; }
    }
}