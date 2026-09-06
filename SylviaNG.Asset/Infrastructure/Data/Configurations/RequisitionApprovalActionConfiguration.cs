using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RMS.Domain.Entities;

namespace RMS.Infrastructure.Data.Configurations;

public class RequisitionApprovalActionConfiguration : IEntityTypeConfiguration<RequisitionApprovalAction>
{
    public void Configure(EntityTypeBuilder<RequisitionApprovalAction> builder)
    {
        builder.ToTable("RequisitionApprovalActions");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Comment).HasMaxLength(2000);
        builder.Property(a => a.ActorName).HasMaxLength(200);
        builder.Property(a => a.CapturedEstimatedCost).HasColumnType("decimal(18,2)");

        builder.HasMany(a => a.PartialDecisions)
            .WithOne(d => d.RequisitionApprovalAction!)
            .HasForeignKey(d => d.RequisitionApprovalActionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
