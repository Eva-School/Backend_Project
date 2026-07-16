using GradeManagementSystem.Core.Entities.Enums;
using System.Collections.Generic;

namespace GradeManagementSystem.Core.DTOs.Teacher
{
    public class TeacherProfileDto
    {
        public string Name { get; set; } = string.Empty;
        public string Subtitle { get; set; } = "Teacher";
        public string CurrentAcademicYear { get; set; } = string.Empty;
    }

    public class TeacherSubjectDto
    {
        public int Id { get; set; }
        public string SubjectName { get; set; } = string.Empty;
    }

    public class TeacherSubjectYearGroupDto
    {
        public string Year { get; set; } = string.Empty;
        public string Stage { get; set; } = string.Empty;
        public List<TeacherSubjectDto> Subjects { get; set; } = new();
    }

    public class TeacherSubjectsResponseDto
    {
        public List<TeacherSubjectYearGroupDto> Groups { get; set; } = new();
    }

    public class TeacherStudentGradeDto
    {
        public int StudentId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public int SubjectId { get; set; }
        public string SubjectName { get; set; } = string.Empty;
        public decimal? Q1 { get; set; }
        public decimal? Q2 { get; set; }
        public decimal? Q3 { get; set; }
        public decimal? Q4 { get; set; }
        public decimal? FinalGrade { get; set; }
        public decimal? MaxQ1 { get; set; }
        public decimal? MaxQ2 { get; set; }
        public decimal? MaxQ3 { get; set; }
        public decimal? MaxQ4 { get; set; }
        public string Status { get; set; } = "InProgress";
    }

    public class TeacherStudentsResponseDto
    {
        public List<TeacherStudentGradeDto> Students { get; set; } = new();
    }

    public class TeacherGradeUpdateRequestDTO
    {
        public int ClassId { get; set; }
        public int StudentId { get; set; }
        public int SubjectId { get; set; }
        public decimal? Q1 { get; set; }
        public decimal? Q2 { get; set; }
        public decimal? Q3 { get; set; }
        public decimal? Q4 { get; set; }
    }

    public class TeacherGradeUpdateResponseDto
    {
        public int ClassId { get; set; }
        public int StudentId { get; set; }
        public int SubjectId { get; set; }
        public decimal? Q1 { get; set; }
        public decimal? Q2 { get; set; }
        public decimal? Q3 { get; set; }
        public decimal? Q4 { get; set; }
        public string Status { get; set; } = SubjectStatus.InProgress.ToString();
    }
}
