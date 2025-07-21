using System;

namespace Sanlog
{
    /// <summary>
    /// Represents information about the application.
    /// </summary>
    public sealed record class LoggingApplication
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
        /// Gets the environment name.
        /// </summary>
        public required string Environment { get; init; }
        /// <summary>
        /// Gets the tenant identifier.
        /// </summary>
        public Guid TenantId { get; init; }
    }
}