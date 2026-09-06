using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RMS.Domain.Entities;

namespace RMS.Infrastructure.Data.Configurations;

public class RequisitionApprovalProcessConfiguration : IEntityTypeConfiguration<RequisitionApprovalProcess>
{
    public void Configure(EntityTypeBuilder<RequisitionApprovalProcess> builder)
    {
        builder.ToTable("RequisitionApprovalProcesses");
        builder.HasKey(p => p.Id);
        builder.HasIndex(p => p.RequisitionId).IsUnique();

        builder.HasOne(p => p.Requisition)
            .WithOne(r => r.ApprovalProcess!)
            .HasForeignKey<RequisitionApprovalProcess>(p => p.RequisitionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(p => p.ApprovalWorkflowVersion)
            .WithMany()
            .HasForeignKey(p => p.ApprovalWorkflowVersionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(p => p.StageInstances)
            .WithOne(s => s.RequisitionApprovalProcess!)
            .HasForeignKey(s => s.RequisitionApprovalProcessId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
