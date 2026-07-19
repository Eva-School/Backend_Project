using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace GradeManagementSystem.Core.DTOs.Vice
{
    public class ViceFinalStudentRowDto
    {
        public string StudentId { get; set; } = string.Empty;
        public string StudentName { get; set; } = string.Empty;
        public string ClassName { get; set; } = string.Empty;
        public decimal Score { get; set; }
    }

    public class ViceFinalStudentsTableResponseDto
    {
        public string Status { get; set; } = "draft"; // draft | submitted | approved
        public List<ViceFinalStudentRowDto> Students { get; set; } = new();
    }

    public class ViceUpsertFinalGradeRequestRowDto
    {
        [Required]
        public string StudentId { get; set; } = string.Empty;

        [Range(0, 1000000)]
        public decimal Score { get; set; }
    }

    public class ViceUpsertFinalGradesRequestDTO
    {
        [Required]
        public string Level { get; set; } = string.Empty;

        [Range(1, 2)]
        public int Semester { get; set; }

        [Required]
        public string Department { get; set; } = string.Empty;

        [Range(1, int.MaxValue)]
        public int ClassId { get; set; }

        [Required]
        [Range(1, int.MaxValue)]
        public int SubjectId { get; set; }

        [Required, MinLength(1)]
        public List<ViceUpsertFinalGradeRequestRowDto> Grades { get; set; } = new();
    }

    public class ViceSubmitFinalGradesRequestDTO
    {
        [Required]
        public string Level { get; set; } = string.Empty;

        [Required]
        public int Semester { get; set; }

        [Required]
        public string Department { get; set; } = string.Empty;

        public int? ClassId { get; set; }

        [Required]
        [Range(1, int.MaxValue)]
        public int SubjectId { get; set; }
    }

    public class ViceFinalApproveRequestDTO
    {
        [Required]
        public string Level { get; set; } = string.Empty;

        [Required]
        public int Semester { get; set; }

        [Required]
        public string Department { get; set; } = string.Empty;

        public string? ClassId { get; set; }

        [Required]
        [Range(1, int.MaxValue)]
        public int SubjectId { get; set; }
    }

    public class ViceFinalGradeHistoryItemDto
    {
        public int Id { get; set; }
        public string Action { get; set; } = string.Empty;
        public string? TeacherName { get; set; }
        public string? SubjectName { get; set; }
        public string? ClassName { get; set; }
        public string? Level { get; set; }
        public DateTime Timestamp { get; set; }
        public decimal? BeforeScore { get; set; }
        public decimal? AfterScore { get; set; }
    }
}
