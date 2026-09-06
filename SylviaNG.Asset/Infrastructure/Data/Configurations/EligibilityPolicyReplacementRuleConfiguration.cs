using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RMS.Domain.Entities;

namespace RMS.Infrastructure.Data.Configurations;

public class EligibilityPolicyReplacementRuleConfiguration : IEntityTypeConfiguration<EligibilityPolicyReplacementRule>
{
    public void Configure(EntityTypeBuilder<EligibilityPolicyReplacementRule> builder)
    {
        builder.ToTable("EligibilityPolicyReplacementRules");
        builder.HasKey(r => r.Id);
        builder.HasIndex(r => r.EligibilityPolicyId).IsUnique();
    }
}
