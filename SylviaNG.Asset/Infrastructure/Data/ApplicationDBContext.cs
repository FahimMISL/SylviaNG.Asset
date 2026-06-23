using Finbuckle.MultiTenant.Abstractions;
using Microsoft.EntityFrameworkCore;
using SylviaNG.Assets.Domain.Entities;
using SylviaNG.Assets.Domain.Events;

namespace SylviaNG.Assets.Infrastructure.Data
{
    public class ApplicationDBContext : DbContext
    {
        private readonly IMultiTenantContextAccessor<MultiTenancy.TenantInfo>? _multiTenantContextAccessor;
        private readonly IHttpContextAccessor? _httpContextAccessor;

        public ApplicationDBContext(
            DbContextOptions<ApplicationDBContext> options,
            IMultiTenantContextAccessor<MultiTenancy.TenantInfo>? multiTenantContextAccessor = null,
            IHttpContextAccessor? httpContextAccessor = null)
            : base(options)
        {
            _multiTenantContextAccessor = multiTenantContextAccessor;
            _httpContextAccessor = httpContextAccessor;
        }

        /// <summary>
        /// Gets the current tenant ID with priority order:
        /// 1. JWT claims from HttpContext (HTTP requests)
        /// 2. Finbuckle TenantInfo
        /// 3. Empty string fallback
        /// This property is evaluated at query execution time for runtime filtering.
        /// </summary>
        public string CurrentTenantId
        {
            get
            {
                // Priority 1: JWT claims from HTTP request
                if (_httpContextAccessor?.HttpContext?.User?.Identity?.IsAuthenticated == true)
                {
                    var tenantClaim = _httpContextAccessor.HttpContext.User.FindFirst("tenant_id");
                    if (tenantClaim != null && !string.IsNullOrEmpty(tenantClaim.Value))
                    {
                        return tenantClaim.Value;
                    }
                }

                // Priority 2: Finbuckle TenantInfo
                if (_multiTenantContextAccessor?.MultiTenantContext?.TenantInfo?.Identifier != null)
                {
                    return _multiTenantContextAccessor.MultiTenantContext.TenantInfo.Identifier;
                }

                // Priority 3: Fallback
                return string.Empty;
            }
        }

        #region Tables

        public DbSet<Asset> Assets { get; set; }
        public DbSet<Employee> Employees { get; set; }

        #endregion


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Apply configurations
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDBContext).Assembly);

            // Ignore DomainEvents collection (used for in-memory event handling, not database persistence)
            modelBuilder.Ignore<DomainEvent>();
        }
    }
}
