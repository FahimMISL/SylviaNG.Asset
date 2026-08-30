using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RMS.Domain.Entities;

namespace RMS.Infrastructure.Data.Configurations;

public class ApprovalWorkflowStageConfiguration : IEntityTypeConfiguration<ApprovalWorkflowStage>
{
    public void Configure(EntityTypeBuilder<ApprovalWorkflowStage> builder)
    {
        builder.ToTable("ApprovalWorkflowStages");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Name).IsRequired().HasMaxLength(200);
        builder.HasIndex(s => new { s.ApprovalWorkflowVersionId, s.StageOrder }).IsUnique();

        builder.HasMany(s => s.Approvers)
            .WithOne(a => a.ApprovalWorkflowStage!)
            .HasForeignKey(a => a.ApprovalWorkflowStageId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(s => s.Conditions)
            .WithOne(c => c.ApprovalWorkflowStage!)
            .HasForeignKey(c => c.ApprovalWorkflowStageId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(s => s.Sla)
            .WithOne(sla => sla.ApprovalWorkflowStage!)
            .HasForeignKey<ApprovalWorkflowSlaConfiguration>(sla => sla.ApprovalWorkflowStageId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
