using System.ComponentModel.DataAnnotations;
using GradeManagementSystem.Core.DTOs.Notification;
using GradeManagementSystem.Core.DTOs.TeacherAssignment;
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

    [Theory]
    [InlineData("junior")]
    [InlineData("Wheeler")]
    [InlineData("SENIOR")]
    public void Teacher_assignment_accepts_a_supported_stage(string stage)
    {
        var request = ValidTeacherAssignment(stage);

        Assert.True(Validator.TryValidateObject(request, new ValidationContext(request), null, true));
    }

    [Fact]
    public void Teacher_assignment_rejects_a_missing_or_unknown_stage()
    {
        var request = ValidTeacherAssignment("primary");
        var results = new List<ValidationResult>();

        Assert.False(Validator.TryValidateObject(request, new ValidationContext(request), results, true));
        Assert.Contains(results, result => result.MemberNames.Contains(nameof(TeacherAssignmentRequestDTO.Stage)));
    }

    private static TeacherAssignmentRequestDTO ValidTeacherAssignment(string stage) => new()
    {
        TeacherId = "10",
        YearId = "2026-2027",
        Stage = stage,
        SubjectId = "3",
        ClassIds = [12]
    };
}
