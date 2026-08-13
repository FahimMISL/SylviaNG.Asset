using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RMS.Domain.Entities;

namespace RMS.Infrastructure.Data.Configurations;

public class RequisitionFieldValueConfiguration : IEntityTypeConfiguration<RequisitionFieldValue>
{
    public void Configure(EntityTypeBuilder<RequisitionFieldValue> builder)
    {
        builder.ToTable("RequisitionFieldValues");
        builder.HasKey(v => v.Id);
        builder.Property(v => v.Value).HasMaxLength(5000);

        builder.HasOne(v => v.FieldDefinition)
            .WithMany()
            .HasForeignKey(v => v.FieldDefinitionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
