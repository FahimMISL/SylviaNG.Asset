using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RMS.Domain.Entities;

namespace RMS.Infrastructure.Data.Configurations;

public class CategoryItemConfiguration : IEntityTypeConfiguration<CategoryItem>
{
    public void Configure(EntityTypeBuilder<CategoryItem> builder)
    {
        builder.ToTable("CategoryItems");
        builder.HasKey(i => i.Id);
        builder.Property(i => i.Name).IsRequired().HasMaxLength(200);
    }
}
