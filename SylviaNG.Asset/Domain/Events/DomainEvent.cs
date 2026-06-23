namespace SylviaNG.Assets.Domain.Events;

public abstract class DomainEvent
{
    public Guid EventId { get; set; } = Guid.NewGuid();
    public string TenantId { get; set; } = string.Empty;
    public string ServiceName { get; set; } = "sylviang-asset";
    public string EntityName { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
    public long? PerformedBy { get; set; }
    public string? CorrelationId { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
}
