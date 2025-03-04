using System;

namespace Sanlog
{
    /// <summary>
    /// Represents the tenant client.
    /// </summary>
    public sealed record class LoggingTenant
    {
        /// <summary>
        /// Gets the object identifier.
        /// </summary>
        public Guid Id { get; init; }
        /// <summary>
        /// Gets the client name.
        /// </summary>
        public required string ClientName { get; init; }
        /// <summary>
        /// Gets the client description.
        /// </summary>
        public string? ClientDescription { get; init; }
    }
}