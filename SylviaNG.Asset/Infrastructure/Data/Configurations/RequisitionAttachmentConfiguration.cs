using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RMS.Domain.Entities;

namespace RMS.Infrastructure.Data.Configurations;

public class RequisitionAttachmentConfiguration : IEntityTypeConfiguration<RequisitionAttachment>
{
    public void Configure(EntityTypeBuilder<RequisitionAttachment> builder)
    {
        builder.ToTable("RequisitionAttachments");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.FileName).HasMaxLength(300).IsRequired();
        builder.Property(a => a.ContentType).HasMaxLength(150).IsRequired();
        builder.Property(a => a.StoragePath).HasMaxLength(500).IsRequired();
        builder.Property(a => a.UploadedByName).HasMaxLength(200);

        // Feature 13: explicit DB-level defaults, not just the C# property initializers - EF's
        // migration generator doesn't infer a column default from a C# initializer on its own (it
        // would otherwise default Version to 0, silently backfilling every existing row as "version
        // 0" instead of "version 1").
        builder.Property(a => a.DocumentType).HasDefaultValue(RMS.Domain.Enums.DocumentType.SupportingDocument);
        builder.Property(a => a.Version).HasDefaultValue(1);
        builder.Property(a => a.IsDeleted).HasDefaultValue(false);

        builder.HasIndex(a => a.RequisitionId);
        // Feature 13: version-lineage lookups (compute next version, find latest) are always scoped
        // to one requisition's one document type.
        builder.HasIndex(a => new { a.RequisitionId, a.DocumentType });
    }
}
