using System;

namespace Leviasan.Sanlog
{
    /// <summary>
    /// Represents the base class of the logging property.
    /// </summary>
    public abstract record class LoggingBaseProperty
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
        /// Gets the parent object identifier.
        /// </summary>
        public Guid ParentId { get; init; }
    }
}