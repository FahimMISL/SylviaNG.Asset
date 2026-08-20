using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RMS.Domain.Entities;

namespace RMS.Infrastructure.Data.Configurations;

public class EligibilityPolicyCriterionConfiguration : IEntityTypeConfiguration<EligibilityPolicyCriterion>
{
    public void Configure(EntityTypeBuilder<EligibilityPolicyCriterion> builder)
    {
        builder.ToTable("EligibilityPolicyCriteria");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.AllowedValue).IsRequired().HasMaxLength(100);
    }
}
