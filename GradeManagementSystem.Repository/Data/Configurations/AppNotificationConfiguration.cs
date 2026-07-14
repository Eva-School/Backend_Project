using GradeManagementSystem.Core.Entities.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GradeManagementSystem.Repository.Data.Configurations;

public class AppNotificationConfiguration : IEntityTypeConfiguration<AppNotification>
{
    public void Configure(EntityTypeBuilder<AppNotification> builder)
    {
        builder.ToTable("Notifications");
        builder.HasKey(item => item.NotificationID);
        builder.Property(item => item.Type).IsRequired().HasMaxLength(20);
        builder.Property(item => item.Title).IsRequired().HasMaxLength(160);
        builder.Property(item => item.Message).IsRequired().HasMaxLength(2000);
        builder.Property(item => item.Priority).IsRequired().HasMaxLength(20);
        builder.Property(item => item.TargetRole).HasMaxLength(100);
        builder.Property(item => item.CreatedAt).IsRequired().HasDefaultValueSql("GETUTCDATE()");
        builder.HasIndex(item => new { item.TargetRole, item.CreatedAt });
    }
}
