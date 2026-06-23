using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SylviaNG.Assets.Domain.Entities;
using SylviaNG.Assets.Domain.Enums;

namespace SylviaNG.Assets.Infrastructure.Configurations
{
    public class AssetConfiguration : IEntityTypeConfiguration<Asset>
    {
        public void Configure(EntityTypeBuilder<Asset> builder)
        {
            builder.ToTable("Assets");
            builder.HasKey(a => a.AssetId);

            builder.Property(a => a.AssetCode)
                .IsRequired()
                .HasMaxLength(50);

            builder.Property(a => a.Name)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(a => a.Description)
                .HasColumnType("text");

            builder.Property(a => a.Category)
                .HasConversion<string>()
                .HasMaxLength(50);

            builder.Property(a => a.Status)
                .HasConversion<string>()
                .HasMaxLength(50);

            builder.Property(a => a.PurchaseValue)
                .HasColumnType("decimal(18,2)");

            builder.HasIndex(a => a.SiteId);
            builder.HasIndex(a => a.Status);
            builder.HasIndex(a => new { a.SiteId, a.AssetCode }).IsUnique();

            builder.HasData(
                new
                {
                    AssetId = 1L,
                    SiteId = 1L,
                    DepartmentId = (long?)1,
                    AssetCode = "IT-LAP-0001",
                    Name = "Dell Latitude 5540",
                    Description = "14-inch business laptop assigned to engineering.",
                    Category = AssetCategoryEnum.IT,
                    Status = AssetStatusEnum.Assigned,
                    AssignedToEmployeeId = (long?)1,
                    PurchaseValue = 1450.00m,
                    AcquiredDate = new DateTime(2024, 3, 1, 0, 0, 0, DateTimeKind.Utc),
                    IsActive = true,
                    TenantId = "default_tenant",
                    Remarks = (string?)null,
                    CreatedAt = new DateTime(2024, 3, 1, 0, 0, 0, DateTimeKind.Utc),
                    CreatedBy = 1L,
                    UpdatedAt = (DateTime?)null,
                    UpdatedBy = (long?)null,
                    DeletedAt = (DateTime?)null,
                    DeletedBy = (long?)null,
                    AuditStatus = 1
                },
                new
                {
                    AssetId = 2L,
                    SiteId = 1L,
                    DepartmentId = (long?)null,
                    AssetCode = "FUR-CHR-0102",
                    Name = "Ergonomic Office Chair",
                    Description = "Adjustable ergonomic chair, common area.",
                    Category = AssetCategoryEnum.Furniture,
                    Status = AssetStatusEnum.Available,
                    AssignedToEmployeeId = (long?)null,
                    PurchaseValue = 320.00m,
                    AcquiredDate = new DateTime(2023, 11, 15, 0, 0, 0, DateTimeKind.Utc),
                    IsActive = true,
                    TenantId = "default_tenant",
                    Remarks = (string?)null,
                    CreatedAt = new DateTime(2023, 11, 15, 0, 0, 0, DateTimeKind.Utc),
                    CreatedBy = 1L,
                    UpdatedAt = (DateTime?)null,
                    UpdatedBy = (long?)null,
                    DeletedAt = (DateTime?)null,
                    DeletedBy = (long?)null,
                    AuditStatus = 1
                }
            );
        }
    }
}