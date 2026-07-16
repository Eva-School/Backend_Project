using System;
using System.ComponentModel.DataAnnotations;

namespace GradeManagementSystem.Core.DTOs.Teacher
{
    public class TeacherRegisterRequest
    {
        [Required]
        public DateTime HireDate { get; set; }

        [Required]
        public string Department { get; set; }

        [Required]
        public string Qualifications { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        public string Role { get; set; }

        [Required]
        [StringLength(15, MinimumLength = 8)]
        [RegularExpression(@"^\d+$", ErrorMessage = "Phone number must be digits only")]
        public string Phone { get; set; }

        [Required]
        public FullNameDto FullName { get; set; }
    }

    public class FullNameDto
    {
        [Required]
        public string FirstName { get; set; }
        public string? MiddleName { get; set; }
        [Required]
        public string LastName { get; set; }
    }
}
