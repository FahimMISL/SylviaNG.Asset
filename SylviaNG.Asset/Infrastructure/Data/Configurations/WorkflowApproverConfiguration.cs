using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RMS.Domain.Entities;

namespace RMS.Infrastructure.Data.Configurations;

public class WorkflowApproverConfiguration : IEntityTypeConfiguration<WorkflowApprover>
{
    public void Configure(EntityTypeBuilder<WorkflowApprover> builder)
    {
        builder.ToTable("WorkflowApprovers");
        builder.HasKey(a => a.Id);

        builder.HasOne(a => a.ApproverUser)
            .WithMany()
            .HasForeignKey(a => a.ApproverUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.FallbackApproverUser)
            .WithMany()
            .HasForeignKey(a => a.FallbackApproverUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
