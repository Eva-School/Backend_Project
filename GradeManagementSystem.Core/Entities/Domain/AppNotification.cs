using System.ComponentModel.DataAnnotations;

namespace GradeManagementSystem.Core.Entities.Domain;

public class AppNotification
{
    [Key]
    public int NotificationID { get; set; }

    [Required, StringLength(20)]
    public string Type { get; set; } = "system";

    [Required, StringLength(160)]
    public string Title { get; set; } = string.Empty;

    [Required, StringLength(2000)]
    public string Message { get; set; } = string.Empty;

    [Required, StringLength(20)]
    public string Priority { get; set; } = "medium";

    [StringLength(100)]
    public string? TargetRole { get; set; }

    public int? CreatedByUserID { get; set; }

    public DateTime CreatedAt { get; set; }

    public ICollection<AppNotificationRead> Reads { get; set; } = new List<AppNotificationRead>();
}
