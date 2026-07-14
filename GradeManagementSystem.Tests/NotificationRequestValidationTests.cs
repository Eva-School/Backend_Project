using System.ComponentModel.DataAnnotations;
using GradeManagementSystem.Core.DTOs.Notification;
using Xunit;

namespace GradeManagementSystem.Tests;

public class NotificationRequestValidationTests
{
    [Fact]
    public void Accepts_a_supported_notification_type_and_priority()
    {
        var request = new CreateNotificationRequestDto
        {
            Type = "announcement",
            Title = "Exam timetable",
            Message = "The timetable is available.",
            Priority = "high",
            TargetRole = "Student"
        };

        var isValid = Validator.TryValidateObject(request, new ValidationContext(request), null, true);

        Assert.True(isValid);
    }

    [Fact]
    public void Rejects_an_unsupported_notification_type()
    {
        var request = new CreateNotificationRequestDto
        {
            Type = "invalid",
            Title = "Title",
            Message = "Message",
            Priority = "medium"
        };

        var results = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(request, new ValidationContext(request), results, true);

        Assert.False(isValid);
        Assert.Contains(results, result => result.MemberNames.Contains(nameof(CreateNotificationRequestDto.Type)));
    }
}
