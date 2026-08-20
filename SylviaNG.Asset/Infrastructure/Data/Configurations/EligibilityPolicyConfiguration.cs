using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RMS.Domain.Entities;

namespace RMS.Infrastructure.Data.Configurations;

public class EligibilityPolicyConfiguration : IEntityTypeConfiguration<EligibilityPolicy>
{
    public void Configure(EntityTypeBuilder<EligibilityPolicy> builder)
    {
        builder.ToTable("EligibilityPolicies");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Name).IsRequired().HasMaxLength(200);
        builder.Property(p => p.Description).HasMaxLength(2000);

        // Uniqueness of at most one ACTIVE policy per (CompanyId, CategoryId, CategoryItemId) is
        // enforced in the application layer (CreateEligibilityPolicyCommandHandler/
        // UpdateEligibilityPolicyCommandHandler), not here - a DB-level filtered unique index would
        // need to special-case IsActive=false, which EF's fluent API can't express portably across
        // the Postgres/SqlServer/Oracle providers this codebase already switches between (see
        // DependencyInjection.NormalizeDatabaseProvider). Plain non-unique index for lookup speed.
        builder.HasIndex(p => new { p.CompanyId, p.CategoryId, p.CategoryItemId });

        builder.HasOne(p => p.Company)
            .WithMany()
            .HasForeignKey(p => p.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.Category)
            .WithMany()
            .HasForeignKey(p => p.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.CategoryItem)
            .WithMany()
            .HasForeignKey(p => p.CategoryItemId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(p => p.Criteria)
            .WithOne(c => c.EligibilityPolicy!)
            .HasForeignKey(c => c.EligibilityPolicyId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(p => p.ReplacementRule)
            .WithOne(r => r.EligibilityPolicy!)
            .HasForeignKey<EligibilityPolicyReplacementRule>(r => r.EligibilityPolicyId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
