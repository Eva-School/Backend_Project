using System.ComponentModel.DataAnnotations;

namespace GradeManagementSystem.Core.DTOs.Notification;

public class CreateNotificationRequestDto
{
    [Required, RegularExpression("^(grade|announcement|system|reminder)$")]
    public string Type { get; set; } = "system";

    [Required, StringLength(160)]
    public string Title { get; set; } = string.Empty;

    [Required, StringLength(2000)]
    public string Message { get; set; } = string.Empty;

    [Required, RegularExpression("^(low|medium|high)$")]
    public string Priority { get; set; } = "medium";

    [StringLength(100)]
    public string? TargetRole { get; set; }
}

public class UpdateNotificationReadRequestDto
{
    public int? Id { get; set; }
    public bool MarkAllRead { get; set; }
}
