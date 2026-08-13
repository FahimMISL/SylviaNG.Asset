using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RMS.Domain.Entities;

namespace RMS.Infrastructure.Data.Configurations;

public class RequisitionItemConfiguration : IEntityTypeConfiguration<RequisitionItem>
{
    public void Configure(EntityTypeBuilder<RequisitionItem> builder)
    {
        builder.ToTable("RequisitionItems");
        builder.HasKey(i => i.Id);
        builder.Property(i => i.ItemName).IsRequired().HasMaxLength(300);

        builder.HasOne(i => i.CategoryItem)
            .WithMany()
            .HasForeignKey(i => i.CategoryItemId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
