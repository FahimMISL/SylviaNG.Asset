using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RMS.Domain.Entities;

namespace RMS.Infrastructure.Data.Configurations;

public class PartialApprovalDecisionConfiguration : IEntityTypeConfiguration<PartialApprovalDecision>
{
    public void Configure(EntityTypeBuilder<PartialApprovalDecision> builder)
    {
        builder.ToTable("PartialApprovalDecisions");
        builder.HasKey(d => d.Id);
        builder.Property(d => d.DeclineReason).HasMaxLength(1000);

        builder.HasOne(d => d.RequisitionItem)
            .WithMany()
            .HasForeignKey(d => d.RequisitionItemId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
