using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SylviaNG.Assets.Domain.Entities;

namespace SylviaNG.Assets.Infrastructure.Configurations
{
    public class EmployeeConfiguration : IEntityTypeConfiguration<Employee>
    {
        public void Configure(EntityTypeBuilder<Employee> builder)
        {
            builder.ToTable("Employees");
            builder.HasKey(e => e.EmployeeId);

            builder.Property(e => e.EmployeeName)
                .HasMaxLength(200);

            builder.Property(e => e.EmployeeCode)
                .HasMaxLength(50);

            // Indexes
            builder.HasIndex(e => e.EmployeeCode);
        }
    }
}
