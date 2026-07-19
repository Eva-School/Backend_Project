using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using GradeManagementSystem.Core.DTOs.Vice;
using Xunit;

namespace GradeManagementSystem.Tests;

public class FinalGradesTests
{
    [Fact]
    public void ViceUpsertFinalGradesRequestDTO_Rejects_Invalid_SubjectId()
    {
        var request = new ViceUpsertFinalGradesRequestDTO
        {
            Level = "junior",
            Semester = 1,
            Department = "om",
            ClassId = 1,
            SubjectId = 0, // Invalid
            Grades = new List<ViceUpsertFinalGradeRequestRowDto>
            {
                new() { StudentId = "12", Score = 85m }
            }
        };

        var results = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(request, new ValidationContext(request), results, true);

        Assert.False(isValid);
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(ViceUpsertFinalGradesRequestDTO.SubjectId)));
    }

    [Fact]
    public void ViceUpsertFinalGradesRequestDTO_Accepts_Valid_SubjectId()
    {
        var request = new ViceUpsertFinalGradesRequestDTO
        {
            Level = "junior",
            Semester = 1,
            Department = "om",
            ClassId = 1,
            SubjectId = 5, // Valid
            Grades = new List<ViceUpsertFinalGradeRequestRowDto>
            {
                new() { StudentId = "12", Score = 85m }
            }
        };

        var isValid = Validator.TryValidateObject(request, new ValidationContext(request), null, true);

        Assert.True(isValid);
    }

    [Fact]
    public void ViceSubmitFinalGradesRequestDTO_Rejects_Invalid_SubjectId()
    {
        var request = new ViceSubmitFinalGradesRequestDTO
        {
            Level = "senior",
            Semester = 2,
            Department = "sd",
            ClassId = 3,
            SubjectId = -1 // Invalid
        };

        var results = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(request, new ValidationContext(request), results, true);

        Assert.False(isValid);
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(ViceSubmitFinalGradesRequestDTO.SubjectId)));
    }

    [Fact]
    public void ViceFinalApproveRequestDTO_Rejects_Invalid_SubjectId()
    {
        var request = new ViceFinalApproveRequestDTO
        {
            Level = "wheeler",
            Semester = 1,
            Department = "om",
            ClassId = "2",
            SubjectId = 0 // Invalid
        };

        var results = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(request, new ValidationContext(request), results, true);

        Assert.False(isValid);
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(ViceFinalApproveRequestDTO.SubjectId)));
    }
}
