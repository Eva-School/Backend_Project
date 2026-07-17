using System;
using System.ComponentModel.DataAnnotations;

namespace GradeManagementSystem.Core.DTOs.Teacher
{
    public class TeacherRegisterRequest
    {
        [Required]
        public DateTime HireDate { get; set; }

        public string Department { get; set; } = string.Empty;

        [Required]
        public string Qualifications { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [StringLength(15, MinimumLength = 8)]
        [RegularExpression(@"^\d+$", ErrorMessage = "Phone number must be digits only")]
        public string Phone { get; set; } = string.Empty;

        [Required]
        public FullNameDto FullName { get; set; } = new();
    }

    public class FullNameDto
    {
        [Required]
        public string FirstName { get; set; } = string.Empty;
        public string? MiddleName { get; set; }
        [Required]
        public string LastName { get; set; } = string.Empty;
    }
}
