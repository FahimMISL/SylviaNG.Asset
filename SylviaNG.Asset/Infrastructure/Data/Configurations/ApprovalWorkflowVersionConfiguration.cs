using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RMS.Domain.Entities;

namespace RMS.Infrastructure.Data.Configurations;

public class ApprovalWorkflowVersionConfiguration : IEntityTypeConfiguration<ApprovalWorkflowVersion>
{
    public void Configure(EntityTypeBuilder<ApprovalWorkflowVersion> builder)
    {
        builder.ToTable("ApprovalWorkflowVersions");
        builder.HasKey(v => v.Id);
        builder.Property(v => v.Notes).HasMaxLength(2000);
        builder.HasIndex(v => new { v.ApprovalWorkflowId, v.VersionNumber }).IsUnique();

        builder.HasMany(v => v.Stages)
            .WithOne(s => s.ApprovalWorkflowVersion!)
            .HasForeignKey(s => s.ApprovalWorkflowVersionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(v => v.CategoryLinks)
            .WithOne(l => l.ApprovalWorkflowVersion!)
            .HasForeignKey(l => l.ApprovalWorkflowVersionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
