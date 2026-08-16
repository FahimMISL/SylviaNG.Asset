using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RMS.Domain.Entities;

namespace RMS.Infrastructure.Data.Configurations;

public class CategoryFieldDefinitionConfiguration : IEntityTypeConfiguration<CategoryFieldDefinition>
{
    public void Configure(EntityTypeBuilder<CategoryFieldDefinition> builder)
    {
        builder.ToTable("CategoryFieldDefinitions");
        builder.HasKey(f => f.Id);
        builder.Property(f => f.Label).IsRequired().HasMaxLength(200);
        builder.Property(f => f.FieldType).HasConversion<string>().HasMaxLength(30);
        builder.Property(f => f.HelpText).HasMaxLength(500);

        builder.HasMany(f => f.Options)
            .WithOne(o => o.FieldDefinition)
            .HasForeignKey(o => o.FieldDefinitionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(f => f.ValidationRule)
            .WithOne(r => r!.FieldDefinition)
            .HasForeignKey<CategoryFieldValidationRule>(r => r.FieldDefinitionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
