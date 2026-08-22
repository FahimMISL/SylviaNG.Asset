using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RMS.Domain.Entities;

namespace RMS.Infrastructure.Data.Configurations;

public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("Notifications");
        builder.HasKey(n => n.Id);
        builder.Property(n => n.Subject).IsRequired().HasMaxLength(300);
        builder.Property(n => n.Message).IsRequired();

        // The inbox query is always "this recipient's rows, newest/unread first".
        builder.HasIndex(n => new { n.RecipientUserId, n.IsRead });
    }
}
