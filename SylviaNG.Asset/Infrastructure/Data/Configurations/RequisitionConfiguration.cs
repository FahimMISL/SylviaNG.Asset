using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RMS.Domain.Entities;

namespace RMS.Infrastructure.Data.Configurations;

public class RequisitionConfiguration : IEntityTypeConfiguration<Requisition>
{
    public void Configure(EntityTypeBuilder<Requisition> builder)
    {
        builder.ToTable("Requisitions");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Justification).HasMaxLength(2000);
        builder.Property(r => r.UrgencyJustification).HasMaxLength(2000);
        builder.Property(r => r.EstimatedCost).HasColumnType("decimal(18,2)");
        builder.Property(r => r.RequisitionNumber).HasMaxLength(20);
        // Filtered so multiple Drafts (RequisitionNumber still null) don't collide under providers
        // that treat NULL as a value in unique indexes.
        builder.HasIndex(r => r.RequisitionNumber).IsUnique().HasFilter("\"RequisitionNumber\" IS NOT NULL");

        builder.HasMany(r => r.StatusHistory)
            .WithOne(h => h.Requisition!)
            .HasForeignKey(h => h.RequisitionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(r => r.Attachments)
            .WithOne(a => a.Requisition!)
            .HasForeignKey(a => a.RequisitionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(r => r.Company)
            .WithMany()
            .HasForeignKey(r => r.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.Category)
            .WithMany()
            .HasForeignKey(r => r.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.RequestedByUser)
            .WithMany()
            .HasForeignKey(r => r.RequestedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.CostCenter)
            .WithMany()
            .HasForeignKey(r => r.CostCenterId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(r => r.ProjectCode).HasMaxLength(100);

        builder.HasMany(r => r.Items)
            .WithOne(i => i.Requisition!)
            .HasForeignKey(i => i.RequisitionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(r => r.FieldValues)
            .WithOne(v => v.Requisition!)
            .HasForeignKey(v => v.RequisitionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
