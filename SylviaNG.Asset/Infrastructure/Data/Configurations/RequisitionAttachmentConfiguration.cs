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

        builder.HasIndex(a => a.RequisitionId);
    }
}
