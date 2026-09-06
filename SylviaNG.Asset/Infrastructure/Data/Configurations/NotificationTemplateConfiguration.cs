using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RMS.Domain.Entities;

namespace RMS.Infrastructure.Data.Configurations;

public class NotificationTemplateConfiguration : IEntityTypeConfiguration<NotificationTemplate>
{
    public void Configure(EntityTypeBuilder<NotificationTemplate> builder)
    {
        builder.ToTable("NotificationTemplates");
        builder.HasKey(t => t.Id);
        builder.Property(t => t.EmailSubject).IsRequired().HasMaxLength(300);
        builder.Property(t => t.EmailBody).IsRequired();
        builder.Property(t => t.InAppMessage).IsRequired().HasMaxLength(500);
        builder.HasIndex(t => new { t.CompanyId, t.EventType }).IsUnique();
    }
}
