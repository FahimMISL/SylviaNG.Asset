using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RMS.Domain.Entities;

namespace RMS.Infrastructure.Data.Configurations;

public class ApprovalWorkflowConfiguration : IEntityTypeConfiguration<ApprovalWorkflow>
{
    public void Configure(EntityTypeBuilder<ApprovalWorkflow> builder)
    {
        builder.ToTable("ApprovalWorkflows");
        builder.HasKey(w => w.Id);
        builder.Property(w => w.Name).IsRequired().HasMaxLength(200);
        builder.Property(w => w.Description).HasMaxLength(2000);
        builder.HasIndex(w => new { w.CompanyId, w.Name }).IsUnique();

        builder.HasOne(w => w.Company)
            .WithMany()
            .HasForeignKey(w => w.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(w => w.Versions)
            .WithOne(v => v.ApprovalWorkflow!)
            .HasForeignKey(v => v.ApprovalWorkflowId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
