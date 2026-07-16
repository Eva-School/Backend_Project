using System.ComponentModel.DataAnnotations;

namespace GradeManagementSystem.Core.DTOs.Vice
{
    public class ViceStudentDto
    {
        public string Id { get; set; } = string.Empty;
        public int ClassId { get; set; }
        public string StudentCode { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string MiddleName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public string ClassName { get; set; } = string.Empty;
        public string Year { get; set; } = string.Empty;
        public string AcademicYearName { get; set; } = string.Empty;
    }

    public class ViceCreateStudentRequestDTO
    {
        [Required(ErrorMessage = "firstName is required")]
        public string FirstName { get; set; } = string.Empty;

        public string? MiddleName { get; set; }

        [Required(ErrorMessage = "lastName is required")]
        public string LastName { get; set; } = string.Empty;

        [Required(ErrorMessage = "studentCode is required")]
        public string StudentCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "email is required")]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Phone(ErrorMessage = "phone is invalid")]
        public string? Phone { get; set; }

        [Required(ErrorMessage = "department is required")]
        public string Department { get; set; } = string.Empty;

        [Required(ErrorMessage = "year is required")]
        public string Year { get; set; } = string.Empty;

        public string? AcademicYearName { get; set; }

        public int? ClassId { get; set; }
    }

    public class ViceUpdateStudentRequestDTO : ViceCreateStudentRequestDTO
    {
    }
}
