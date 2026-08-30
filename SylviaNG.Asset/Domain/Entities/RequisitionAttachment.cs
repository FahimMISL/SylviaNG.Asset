using RMS.Domain.Common;

namespace RMS.Domain.Entities;

/// <summary>
/// US-007: a supporting document on a requisition. Minimal local-disk storage
/// for this pass (StoragePath is a relative path under the app's upload root) -
/// real object storage (MinIO, etc.) is Feature 16's job; this keeps the same
/// shape (file reference + metadata, no blob in the DB) so swapping the
/// storage backend later doesn't change this entity.
/// </summary>
public class RequisitionAttachment : AuditableEntity
{
    public Guid RequisitionId { get; set; }
    public Requisition? Requisition { get; set; }

    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public string StoragePath { get; set; } = string.Empty;

    public Guid UploadedByUserId { get; set; }
    public string UploadedByName { get; set; } = string.Empty;
}
