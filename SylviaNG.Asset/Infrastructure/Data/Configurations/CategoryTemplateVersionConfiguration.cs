using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RMS.Domain.Entities;

namespace RMS.Infrastructure.Data.Configurations;

public class CategoryTemplateVersionConfiguration : IEntityTypeConfiguration<CategoryTemplateVersion>
{
    public void Configure(EntityTypeBuilder<CategoryTemplateVersion> builder)
    {
        builder.ToTable("CategoryTemplateVersions");
        builder.HasKey(v => v.Id);
        builder.Property(v => v.SnapshotJson).IsRequired();
        builder.HasIndex(v => new { v.CategoryId, v.VersionNumber }).IsUnique();
    }
}
