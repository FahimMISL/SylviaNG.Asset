using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RMS.Domain.Entities;

namespace RMS.Infrastructure.Data.Configurations;

public class RequisitionItemConfiguration : IEntityTypeConfiguration<RequisitionItem>
{
    public void Configure(EntityTypeBuilder<RequisitionItem> builder)
    {
        builder.ToTable("RequisitionItems");
        builder.HasKey(i => i.Id);
        builder.Property(i => i.ItemName).IsRequired().HasMaxLength(300);

        builder.HasOne(i => i.CategoryItem)
            .WithMany()
            .HasForeignKey(i => i.CategoryItemId)
            .OnDelete(DeleteBehavior.SetNull);

        // Feature 5: FulfilledQuantity is mutated directly on this row by RecordFulfillment - without
        // its own concurrency token, two racing fulfillment requests each reading the same stale
        // FulfilledQuantity could both pass validation and the second SaveChangesAsync would silently
        // overwrite the first's update (a lost update), letting the combined total exceed the approved
        // ceiling with no error. Same zero-migration-overhead Npgsql system-column pattern already used
        // on RequisitionConfiguration/RequisitionApprovalConfiguration - RecordFulfillmentCommandHandler
        // already catches DbUpdateConcurrencyException and returns a clean "refresh and retry" error.
        builder.Property<uint>("xmin").IsRowVersion();
    }
}
