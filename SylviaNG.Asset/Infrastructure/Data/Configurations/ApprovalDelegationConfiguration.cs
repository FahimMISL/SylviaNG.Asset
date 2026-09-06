using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RMS.Domain.Entities;

namespace RMS.Infrastructure.Data.Configurations;

public class ApprovalDelegationConfiguration : IEntityTypeConfiguration<ApprovalDelegation>
{
    public void Configure(EntityTypeBuilder<ApprovalDelegation> builder)
    {
        builder.ToTable("ApprovalDelegations");
        builder.HasKey(d => d.Id);
        builder.Property(d => d.Reason).IsRequired().HasMaxLength(500);

        builder.HasOne(d => d.Company)
            .WithMany()
            .HasForeignKey(d => d.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(d => d.DelegatorUser)
            .WithMany()
            .HasForeignKey(d => d.DelegatorUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(d => d.DelegateUser)
            .WithMany()
            .HasForeignKey(d => d.DelegateUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(d => new { d.DelegatorUserId, d.StartDate, d.EndDate });
    }
}
