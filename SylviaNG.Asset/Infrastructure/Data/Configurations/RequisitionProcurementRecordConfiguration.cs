using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RMS.Domain.Entities;

namespace RMS.Infrastructure.Data.Configurations;

public class RequisitionProcurementRecordConfiguration : IEntityTypeConfiguration<RequisitionProcurementRecord>
{
    public void Configure(EntityTypeBuilder<RequisitionProcurementRecord> builder)
    {
        builder.ToTable("RequisitionProcurementRecords");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Comment).HasMaxLength(2000);
        builder.Property(r => r.ActorName).HasMaxLength(200);
        builder.Property(r => r.TotalProcurementAmount).HasColumnType("decimal(18,2)");

        builder.HasOne(r => r.Requisition)
            .WithMany(req => req.ProcurementRecords)
            .HasForeignKey(r => r.RequisitionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(r => r.LineItems)
            .WithOne(l => l.RequisitionProcurementRecord!)
            .HasForeignKey(l => l.RequisitionProcurementRecordId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
