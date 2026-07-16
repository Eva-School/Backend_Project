using GradeManagementSystem.Core.DTOs.Class;
using System.Collections.Generic;

namespace GradeManagementSystem.Core.DTOs.TeacherAssignment
{
    public class TeacherAssignmentDashboardYearDto
    {
        public string YearId { get; set; } = string.Empty;
        public List<ClassResponseDTO> Classes { get; set; } = new();
    }

    public class TeacherAssignmentListItemDto
    {
        public int TeacherId { get; set; }
        public string TeacherName { get; set; } = string.Empty;
        public int AcademicYearId { get; set; }
        public string YearName { get; set; } = string.Empty;
        public string Stage { get; set; } = string.Empty;
        public int SubjectId { get; set; }
        public string SubjectName { get; set; } = string.Empty;
        public int ClassId { get; set; }
        public string ClassName { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime? AssignedAt { get; set; }
    }

    public class TeacherAssignmentStatusRequestDto
    {
        public int TeacherId { get; set; }
        public int AcademicYearId { get; set; }
        public int SubjectId { get; set; }
        public int ClassId { get; set; }
        public bool IsActive { get; set; }
    }
}
