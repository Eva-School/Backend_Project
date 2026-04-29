using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace GradeManagementSystem.Core.DTOs.Vice
{
    public class ViceQuarterMaxGradesDto
    {
        public int Q1 { get; set; }
        public int Q2 { get; set; }
        public int Q3 { get; set; }
        public int Q4 { get; set; }
    }

    public class ViceSetQuarterMaxGradesRequestDTO
    {
        [Required]
        public ViceQuarterMaxGradesDto MaxQuarterGrades { get; set; } = new();
    }

    public class ViceQuarterStudentSheetRowDto
    {
        public string StudentId { get; set; } = string.Empty;
        public string StudentName { get; set; } = string.Empty;
        public decimal Q1 { get; set; }
        public decimal Q2 { get; set; }
        public decimal Q3 { get; set; }
        public decimal Q4 { get; set; }
    }

    public class ViceQuarterStudentsSheetResponseDto
    {
        public string Status { get; set; } = "draft";
        public ViceQuarterMaxGradesDto MaxQuarterGrades { get; set; } = new();
        public List<ViceQuarterStudentSheetRowDto> Students { get; set; } = new();
    }

    public class ViceUpsertQuarterGradeRowDto
    {
        [Required]
        public string StudentId { get; set; } = string.Empty;

        public decimal Q1 { get; set; }
        public decimal Q2 { get; set; }
        public decimal Q3 { get; set; }
        public decimal Q4 { get; set; }
    }

    public class ViceUpsertQuarterGradesRequestDTO
    {
        [Required]
        public string Level { get; set; } = string.Empty;

        [Required]
        public int SubjectId { get; set; }

        [Required]
        public string Department { get; set; } = string.Empty;

        [Required]
        public int ClassId { get; set; }

        [Required]
        public List<ViceUpsertQuarterGradeRowDto> Students { get; set; } = new();
    }
}

