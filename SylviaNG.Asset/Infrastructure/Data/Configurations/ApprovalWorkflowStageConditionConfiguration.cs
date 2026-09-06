using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RMS.Domain.Entities;

namespace RMS.Infrastructure.Data.Configurations;

public class ApprovalWorkflowStageConditionConfiguration : IEntityTypeConfiguration<ApprovalWorkflowStageCondition>
{
    public void Configure(EntityTypeBuilder<ApprovalWorkflowStageCondition> builder)
    {
        builder.ToTable("ApprovalWorkflowStageConditions");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.MinCost).HasColumnType("decimal(18,2)");
        builder.Property(c => c.MaxCost).HasColumnType("decimal(18,2)");

        builder.HasOne(c => c.Category)
            .WithMany()
            .HasForeignKey(c => c.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
