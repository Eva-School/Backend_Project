using GradeManagementSystem.Core.Entities.Domain;
using GradeManagementSystem.Core.Entities.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GradeManagementSystem.Repository.Data.Configurations;

public class AppNotificationReadConfiguration : IEntityTypeConfiguration<AppNotificationRead>
{
    public void Configure(EntityTypeBuilder<AppNotificationRead> builder)
    {
        builder.ToTable("NotificationReads");
        builder.HasKey(item => item.NotificationReadID);
        builder.HasIndex(item => new { item.NotificationID, item.UserID }).IsUnique();
        builder.HasIndex(item => item.UserID);
        builder.HasOne(item => item.Notification)
            .WithMany(item => item.Reads)
            .HasForeignKey(item => item.NotificationID)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(item => item.UserID)
            .HasPrincipalKey(item => item.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
