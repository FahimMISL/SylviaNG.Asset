using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RMS.Domain.Entities;

namespace RMS.Infrastructure.Data.Configurations;

public class ApprovalWorkflowSlaConfigurationConfiguration : IEntityTypeConfiguration<ApprovalWorkflowSlaConfiguration>
{
    public void Configure(EntityTypeBuilder<ApprovalWorkflowSlaConfiguration> builder)
    {
        builder.ToTable("ApprovalWorkflowSlaConfigurations");
        builder.HasKey(s => s.Id);
        builder.HasIndex(s => s.ApprovalWorkflowStageId).IsUnique();

        builder.HasOne(s => s.EscalationApproverUser)
            .WithMany()
            .HasForeignKey(s => s.EscalationApproverUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
