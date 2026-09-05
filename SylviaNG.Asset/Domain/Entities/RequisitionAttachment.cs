using RMS.Domain.Enums;
using RMS.Domain.Common;

namespace RMS.Domain.Entities;

/// <summary>
/// US-007 / Feature 13: a document on a requisition. Minimal local-disk storage for this pass
/// (StoragePath is a relative path under the app's upload root) - real object storage (MinIO, etc.)
/// was never actually built anywhere in this project; this keeps the same shape (file reference +
/// metadata, no blob in the DB) so swapping the storage backend later doesn't change this entity -
/// only a new IFileStorageService implementation would be needed.
///
/// Feature 13: DocumentType + Version group attachments into a lineage (same RequisitionId +
/// DocumentType = versions of "the same document"); the highest Version among non-deleted rows in a
/// lineage is the latest (computed at read time, see RequisitionAttachmentDto.FromEntity - not
/// stored, so it can never drift out of sync). IsDeleted is a soft-delete: the row and physical file
/// are both preserved (never destroyed) so audit history for this document stays traceable, but it's
/// excluded from normal listing and no longer counts toward the requisition's attachment size cap.
/// </summary>
public class RequisitionAttachment : AuditableEntity
{
    public Guid RequisitionId { get; set; }
    public Requisition? Requisition { get; set; }

    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public string StoragePath { get; set; } = string.Empty;

    public DocumentType DocumentType { get; set; } = DocumentType.SupportingDocument;
    /// <summary>1-based, per (RequisitionId, DocumentType) lineage - see class remarks.</summary>
    public int Version { get; set; } = 1;

    public bool IsDeleted { get; set; }
    public DateTime? DeletedAtUtc { get; set; }
    public Guid? DeletedByUserId { get; set; }

    public Guid UploadedByUserId { get; set; }
    public string UploadedByName { get; set; } = string.Empty;
}
