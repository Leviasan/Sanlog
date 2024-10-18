using System;
using System.Diagnostics;

namespace Sanlog.EFCore
{
    /// <summary>
    /// Represents the service retrieving details about the tenant.
    /// </summary>
    internal sealed class TenantService : ITenantService
    {
        /// <summary>
        /// The application identifier.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly Guid _appId;
        /// <summary>
        /// The tenant identifier.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly Guid _tenantId;

        /// <summary>
        /// Initializes a new instance of the <see cref="TenantService"/> class with the specified app and tenant identifiers.
        /// </summary>
        /// <param name="appId">The application identifier.</param>
        /// <param name="tenantId">The tenant identifier.</param>
        public TenantService(Guid appId, Guid tenantId)
        {
            _appId = appId;
            _tenantId = tenantId;
        }

        /// <inheritdoc/>
        public Guid AppId => _appId;
        /// <inheritdoc/>
        public Guid TenantId => _tenantId;
    }
}