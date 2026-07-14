using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GradeManagementSystem.Core.Entities.Domain;

public class AppNotificationRead
{
    [Key]
    public int NotificationReadID { get; set; }

    [ForeignKey(nameof(Notification))]
    public int NotificationID { get; set; }

    public int UserID { get; set; }

    public DateTime ReadAt { get; set; }

    public AppNotification Notification { get; set; } = null!;
}
