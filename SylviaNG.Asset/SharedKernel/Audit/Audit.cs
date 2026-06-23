using SylviaNG.Assets.Domain.Events;

namespace SylviaNG.Assets.SharedKernel.Audit
{
    /// <summary>
    /// Base class for all auditable entities with multi-tenancy support.
    /// TenantId is automatically set from JWT claims during SaveChanges.
    /// </summary>
    public class Audit
    {
        /// <summary>
        /// Tenant identifier for multi-tenancy support.
        /// Automatically populated from Keycloak JWT claim 'tenant_id'.
        /// </summary>
        public string TenantId { get; set; } = "default_tenant";
        public string? Remarks { get; set; }
        public DateTime? CreatedAt { get; set; }
        public long? CreatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public long? UpdatedBy { get; set; }
        public DateTime? DeletedAt { get; set; }
        public long? DeletedBy { get; set; }
        public int Status { get; set; }

        // Domain Events for event-driven architecture
        private readonly List<DomainEvent> _domainEvents = new();
        public IReadOnlyCollection<DomainEvent> DomainEvents => _domainEvents.AsReadOnly();

        public void AddDomainEvent(DomainEvent domainEvent)
        {
            _domainEvents.Add(domainEvent);
        }

        public void ClearDomainEvents()
        {
            _domainEvents.Clear();
        }
    }
}
