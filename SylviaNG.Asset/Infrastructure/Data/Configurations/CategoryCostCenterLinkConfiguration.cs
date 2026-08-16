using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RMS.Domain.Entities;

namespace RMS.Infrastructure.Data.Configurations;

public class CategoryCostCenterLinkConfiguration : IEntityTypeConfiguration<CategoryCostCenterLink>
{
    public void Configure(EntityTypeBuilder<CategoryCostCenterLink> builder)
    {
        builder.ToTable("CategoryCostCenterLinks");
        builder.HasKey(l => l.Id);
        builder.HasIndex(l => new { l.CategoryId, l.CostCenterId }).IsUnique();

        builder.HasOne(l => l.CostCenter)
            .WithMany()
            .HasForeignKey(l => l.CostCenterId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
