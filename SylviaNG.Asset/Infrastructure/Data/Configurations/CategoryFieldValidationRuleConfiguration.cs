using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RMS.Domain.Entities;

namespace RMS.Infrastructure.Data.Configurations;

public class CategoryFieldValidationRuleConfiguration : IEntityTypeConfiguration<CategoryFieldValidationRule>
{
    public void Configure(EntityTypeBuilder<CategoryFieldValidationRule> builder)
    {
        builder.ToTable("CategoryFieldValidationRules");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.RegexPattern).HasMaxLength(500);
        builder.Property(r => r.AllowedFileExtensions).HasMaxLength(200);
        builder.Property(r => r.MinValue).HasPrecision(18, 2);
        builder.Property(r => r.MaxValue).HasPrecision(18, 2);
        builder.HasIndex(r => r.FieldDefinitionId).IsUnique();
    }
}
