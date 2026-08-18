using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RMS.Domain.Entities;
using RMS.Domain.Enums;

namespace RMS.Infrastructure.Data.Configurations;

public class RequisitionApprovalConfiguration : IEntityTypeConfiguration<RequisitionApproval>
{
    public void Configure(EntityTypeBuilder<RequisitionApproval> builder)
    {
        builder.ToTable("RequisitionApprovals");
        builder.HasKey(a => a.Id);

        // Only one *active* instance per stage at a time - a SentBack -> Resubmit creates a fresh
        // instance for the same StageOrder once the old one is terminal, per the Feature 3 plan.
        builder.HasIndex(a => new { a.RequisitionApprovalProcessId, a.StageOrder })
            .IsUnique()
            .HasFilter($"\"Status\" IN ({(int)RequisitionApprovalStatus.Pending}, {(int)RequisitionApprovalStatus.InProgress}, {(int)RequisitionApprovalStatus.ClarificationRequested})");

        builder.HasOne(a => a.ApprovalWorkflowStage)
            .WithMany()
            .HasForeignKey(a => a.ApprovalWorkflowStageId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(a => a.Assignments)
            .WithOne(x => x.RequisitionApproval!)
            .HasForeignKey(x => x.RequisitionApprovalId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(a => a.Actions)
            .WithOne(x => x.RequisitionApproval!)
            .HasForeignKey(x => x.RequisitionApprovalId)
            .OnDelete(DeleteBehavior.Cascade);

        // Npgsql system column, zero migration overhead - the concurrency mechanism for
        // double-action protection (point 30). A violation surfaces as DbUpdateConcurrencyException.
        // UseXminAsConcurrencyToken() shorthand isn't available in this Npgsql provider version;
        // this shadow-property form is the documented equivalent the provider's
        // NpgsqlPostgresModelFinalizingConvention.ProcessRowVersionProperty recognizes.
        builder.Property<uint>("xmin").IsRowVersion();
    }
}
