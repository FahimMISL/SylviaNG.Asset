using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RMS.Domain.Entities;

namespace RMS.Infrastructure.Data.Configurations;

public class RequisitionStatusHistoryConfiguration : IEntityTypeConfiguration<RequisitionStatusHistory>
{
    public void Configure(EntityTypeBuilder<RequisitionStatusHistory> builder)
    {
        builder.ToTable("RequisitionStatusHistories");
        builder.HasKey(h => h.Id);
        builder.Property(h => h.ActorName).HasMaxLength(200);
        builder.Property(h => h.ActorRole).HasMaxLength(50);
        builder.Property(h => h.Comment).HasMaxLength(2000);

        builder.HasIndex(h => h.RequisitionId);
    }
}
