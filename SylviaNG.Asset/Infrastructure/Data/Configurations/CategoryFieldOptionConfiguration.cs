using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RMS.Domain.Entities;

namespace RMS.Infrastructure.Data.Configurations;

public class CategoryFieldOptionConfiguration : IEntityTypeConfiguration<CategoryFieldOption>
{
    public void Configure(EntityTypeBuilder<CategoryFieldOption> builder)
    {
        builder.ToTable("CategoryFieldOptions");
        builder.HasKey(o => o.Id);
        builder.Property(o => o.Label).IsRequired().HasMaxLength(200);
        builder.Property(o => o.Value).IsRequired().HasMaxLength(200);
    }
}
