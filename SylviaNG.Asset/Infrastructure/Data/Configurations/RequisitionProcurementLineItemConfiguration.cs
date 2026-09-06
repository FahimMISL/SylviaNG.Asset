using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RMS.Domain.Entities;

namespace RMS.Infrastructure.Data.Configurations;

public class RequisitionProcurementLineItemConfiguration : IEntityTypeConfiguration<RequisitionProcurementLineItem>
{
    public void Configure(EntityTypeBuilder<RequisitionProcurementLineItem> builder)
    {
        builder.ToTable("RequisitionProcurementLineItems");
        builder.HasKey(l => l.Id);
        builder.Property(l => l.UnitPrice).HasColumnType("decimal(18,2)");

        builder.HasOne(l => l.RequisitionItem)
            .WithMany()
            .HasForeignKey(l => l.RequisitionItemId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
