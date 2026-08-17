namespace RMS.Domain.Common;

public abstract class AuditableEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    // Defaulted like Id above: several call sites across Feature 1/2 construct entities
    // without setting this explicitly, which silently persisted DateTime.MinValue
    // (0001-01-01) instead of the real creation time - most visibly in the requisition
    // status timeline. This default makes that no longer possible; an explicit
    // `CreatedAtUtc = DateTime.UtcNow` elsewhere still simply overrides it.
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public Guid? CreatedByUserId { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
    public Guid? UpdatedByUserId { get; set; }
}
