using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RMS.Domain.Entities;

namespace RMS.Infrastructure.Data.Configurations;

public class ApprovalWorkflowCategoryLinkConfiguration : IEntityTypeConfiguration<ApprovalWorkflowCategoryLink>
{
    public void Configure(EntityTypeBuilder<ApprovalWorkflowCategoryLink> builder)
    {
        builder.ToTable("ApprovalWorkflowCategoryLinks");
        builder.HasKey(l => l.Id);
        builder.HasIndex(l => new { l.ApprovalWorkflowVersionId, l.RequisitionCategoryId }).IsUnique();

        builder.HasOne(l => l.RequisitionCategory)
            .WithMany()
            .HasForeignKey(l => l.RequisitionCategoryId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
