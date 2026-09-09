using System;

namespace GradeManagementSystem.Core.DTOs.Admin.Account
{
    public class TeacherProfileDto
    {
        public int TeacherId { get; set; }
        public int? DepartmentId { get; set; }
        public string? DepartmentName { get; set; }
        public string? Qualifications { get; set; }
        public string? EmployeeCode { get; set; }
        public DateTime? HireDate { get; set; }
        public bool IsActive { get; set; }
    }

    public class StudentProfileDto
    {
        public int StudentId { get; set; }
        public string? StudentCode { get; set; }
        public string? NationalId { get; set; }
        public string? Gender { get; set; }
        public DateTime? EnrollmentDate { get; set; }
        public string? Status { get; set; }
        public int? DepartmentId { get; set; }
        public string? DepartmentName { get; set; }
        public int? CurrentAcademicYearId { get; set; }
        public string? AcademicYearName { get; set; }
        public int? ClassId { get; set; }
        public string? ClassName { get; set; }
        public string? Address { get; set; }
    }
}
