using System;

namespace Sanlog.EFCore
{
    public interface ITenantService
    {
        Guid TenantId { get; }
        //void SetTenant(string tenant);
        //string[] GetTenants();
        // event TenantChangedEventHandler OnTenantChanged;
    }
}