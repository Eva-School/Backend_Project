using System;
using System.Collections.Generic;

namespace GradeManagementSystem.Core.DTOs.Class
{
    public class ClassDetailsDTO
    {
        public int ClassId { get; set; }
        public string ClassName { get; set; } = string.Empty;
        public int? DepartmentId { get; set; }
        public string? DepartmentName { get; set; }
        public int? Capacity { get; set; }
        public int StudentCount { get; set; }
        public bool IsActive { get; set; }
        public int? AcademicYearId { get; set; }
        public string? AcademicYearName { get; set; }
        public string? Stage { get; set; }
        public List<ClassTeacherAssignmentDTO> Teachers { get; set; } = new();
        public List<ClassStudentDTO> Students { get; set; } = new();
    }

    public class ClassTeacherAssignmentDTO
    {
        public int TeacherId { get; set; }
        public string TeacherName { get; set; } = string.Empty;
        public int SubjectId { get; set; }
        public string SubjectName { get; set; } = string.Empty;
    }

    public class ClassStudentDTO
    {
        public int StudentId { get; set; }
        public int? UserId { get; set; }
        public string StudentCode { get; set; } = string.Empty;
        public string NationalId { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string MiddleName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Gender { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime? EnrollmentDate { get; set; }
    }
}
