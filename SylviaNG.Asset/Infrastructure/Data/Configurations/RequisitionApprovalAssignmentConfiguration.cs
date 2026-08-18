using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RMS.Domain.Entities;

namespace RMS.Infrastructure.Data.Configurations;

public class RequisitionApprovalAssignmentConfiguration : IEntityTypeConfiguration<RequisitionApprovalAssignment>
{
    public void Configure(EntityTypeBuilder<RequisitionApprovalAssignment> builder)
    {
        builder.ToTable("RequisitionApprovalAssignments");
        builder.HasKey(a => a.Id);

        builder.HasOne(a => a.AssignedUser)
            .WithMany()
            .HasForeignKey(a => a.AssignedUserId)
            .OnDelete(DeleteBehavior.Restrict);

        // See RequisitionApprovalConfiguration for why this shadow-property form is used instead of
        // the UseXminAsConcurrencyToken() shorthand.
        builder.Property<uint>("xmin").IsRowVersion();
    }
}
