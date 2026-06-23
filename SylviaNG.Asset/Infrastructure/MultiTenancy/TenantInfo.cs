using Finbuckle.MultiTenant.Abstractions;

namespace SylviaNG.Assets.Infrastructure.MultiTenancy
{
    /// <summary>
    /// Represents tenant information for multi-tenancy support.
    /// Used by Finbuckle.MultiTenant to resolve and store tenant context.
    /// </summary>
    public class TenantInfo : ITenantInfo
    {
        /// <summary>
        /// Unique identifier for the tenant.
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Unique identifier/code used to resolve the tenant.
        /// </summary>
        public string Identifier { get; set; } = string.Empty;

        /// <summary>
        /// Display name for the tenant.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Optional connection string for per-tenant database (if using separate databases).
        /// </summary>
        public string ConnectionString { get; set; } = string.Empty;
    }
}
