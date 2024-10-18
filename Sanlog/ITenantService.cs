using System;

namespace Sanlog
{
    /// <summary>
    /// Provides a mechanism for retrieving details about the tenant.
    /// </summary>
    public interface ITenantService
    {
        /// <summary>
        /// Gets the application identifier.
        /// </summary>
        public Guid AppId { get; }
        /// <summary>
        /// Gets the tenant identifier.
        /// </summary>
        public Guid TenantId { get; }
    }
}