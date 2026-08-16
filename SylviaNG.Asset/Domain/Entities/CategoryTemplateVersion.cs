namespace RMS.Domain.Entities;

/// <summary>
/// Immutable snapshot of a category's field schema at the moment it was
/// published, per US-001 AC6 ("in-progress requisitions retain the template
/// version at the time of submission"). SnapshotJson is a serialized copy of
/// the field definitions/options/validation rules at publish time.
/// </summary>
public class CategoryTemplateVersion
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CategoryId { get; set; }
    public RequisitionCategory? Category { get; set; }

    public int VersionNumber { get; set; }
    public string SnapshotJson { get; set; } = string.Empty;
    public DateTime PublishedAtUtc { get; set; } = DateTime.UtcNow;
    public Guid PublishedByUserId { get; set; }
}
