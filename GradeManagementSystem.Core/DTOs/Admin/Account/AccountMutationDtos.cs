using System.ComponentModel.DataAnnotations;

namespace GradeManagementSystem.Core.DTOs.Admin.Account
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter, AllowMultiple = false)]
    public class OptionalPhoneAttribute : ValidationAttribute
    {
        private static readonly PhoneAttribute _innerPhone = new();

        public override bool IsValid(object? value)
        {
            if (value == null) return true;
            if (value is string str)
            {
                var trimmed = str.Trim();
                if (string.IsNullOrEmpty(trimmed)) return true;
                return _innerPhone.IsValid(trimmed);
            }
            return false;
        }

        public override string FormatErrorMessage(string name)
        {
            return ErrorMessage ?? $"{name} is not a valid phone number.";
        }
    }

    public class CreateAccountDto
    {
        [StringLength(100, ErrorMessage = "Username cannot exceed 100 characters.")]
        [RegularExpression(@"^[a-zA-Z0-9._-]+$", ErrorMessage = "Username can only contain letters, numbers, dots, underscores, and hyphens.")]
        public string? Username { get; set; }

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Invalid email format.")]
        [StringLength(256)]
        public string Email { get; set; } = string.Empty;

        [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be at least 8 characters.")]
        public string? Password { get; set; }

        [Required(ErrorMessage = "First name is required.")]
        [StringLength(100, ErrorMessage = "First name cannot exceed 100 characters.")]
        public string FirstName { get; set; } = string.Empty;

        [StringLength(100, ErrorMessage = "Middle name cannot exceed 100 characters.")]
        public string? MiddleName { get; set; }

        [Required(ErrorMessage = "Last name is required.")]
        [StringLength(100, ErrorMessage = "Last name cannot exceed 100 characters.")]
        public string LastName { get; set; } = string.Empty;

        [OptionalPhone(ErrorMessage = "Invalid phone number.")]
        [StringLength(30)]
        public string? PhoneNumber { get; set; }

        [Required(ErrorMessage = "Role is required.")]
        public string Role { get; set; } = string.Empty;

        // Teacher-specific
        public int? DepartmentId { get; set; }
        [StringLength(500)]
        public string? Qualifications { get; set; }

        // Student-specific
        [StringLength(50)]
        public string? NationalId { get; set; }
        [StringLength(50)]
        public string? StudentCode { get; set; }
        [StringLength(20)]
        public string? Gender { get; set; }
        public int? AcademicYearId { get; set; }
        public int? ClassId { get; set; }
        [StringLength(250)]
        public string? Address { get; set; }
    }

    public class CreateAccountResultDto
    {
        public AccountDetailDto Account { get; set; } = null!;
        public string? GeneratedInitialPassword { get; set; }
    }

    public class UpdateAccountProfileDto
    {
        [Required(ErrorMessage = "First name is required.")]
        [StringLength(100, ErrorMessage = "First name cannot exceed 100 characters.")]
        public string FirstName { get; set; } = string.Empty;

        [StringLength(100, ErrorMessage = "Middle name cannot exceed 100 characters.")]
        public string? MiddleName { get; set; }

        [Required(ErrorMessage = "Last name is required.")]
        [StringLength(100, ErrorMessage = "Last name cannot exceed 100 characters.")]
        public string LastName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Invalid email format.")]
        [StringLength(256)]
        public string Email { get; set; } = string.Empty;

        [OptionalPhone(ErrorMessage = "Invalid phone number.")]
        [StringLength(30)]
        public string? PhoneNumber { get; set; }

        // Optional profile updates
        public int? DepartmentId { get; set; }
        [StringLength(500)]
        public string? Qualifications { get; set; }
        [StringLength(50)]
        public string? NationalId { get; set; }
        [StringLength(50)]
        public string? StudentCode { get; set; }
        [StringLength(20)]
        public string? Gender { get; set; }
        public int? AcademicYearId { get; set; }
        public int? ClassId { get; set; }
        [StringLength(250)]
        public string? Address { get; set; }
    }

    public class ChangeUserRoleDto
    {
        [Required(ErrorMessage = "New role is required.")]
        public string NewRole { get; set; } = string.Empty;

        // Target-role optional/required profile fields
        public int? DepartmentId { get; set; }
        [StringLength(500)]
        public string? Qualifications { get; set; }
        [StringLength(50)]
        public string? NationalId { get; set; }
        [StringLength(50)]
        public string? StudentCode { get; set; }
        [StringLength(20)]
        public string? Gender { get; set; }
        public int? AcademicYearId { get; set; }
        public int? ClassId { get; set; }
        [StringLength(250)]
        public string? Address { get; set; }
    }

    public class SetAccountStatusDto
    {
        [Required]
        public bool IsActive { get; set; }
    }

    public class ResetPasswordDto
    {
        [Required(ErrorMessage = "Password is required.")]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be at least 8 characters.")]
        public string NewPassword { get; set; } = string.Empty;
    }

    public class RoleOptionDto
    {
        public int RoleId { get; set; }
        public string RoleName { get; set; } = string.Empty;
        public string NormalizedName { get; set; } = string.Empty;
        public string? Description { get; set; }
    }

    public class AcademicYearOptionDto
    {
        public int AcademicYearId { get; set; }
        public string YearName { get; set; } = string.Empty;
        public string Stage { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }

    public class ClassOptionDto
    {
        public int ClassId { get; set; }
        public string ClassName { get; set; } = string.Empty;
        public int? AcademicYearId { get; set; }
        public string AcademicYearName { get; set; } = string.Empty;
        public string Stage { get; set; } = string.Empty;
        public int? DepartmentId { get; set; }
        public string? DepartmentName { get; set; }
        public int? Capacity { get; set; }
        public int CurrentStudentCount { get; set; }
    }

    public class DepartmentOptionDto
    {
        public int DepartmentId { get; set; }
        public string DepartmentName { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }

    public class AccountFormOptionsDto
    {
        public IReadOnlyList<RoleOptionDto> Roles { get; set; } = new List<RoleOptionDto>();
        public IReadOnlyList<AcademicYearOptionDto> AcademicYears { get; set; } = new List<AcademicYearOptionDto>();
        public IReadOnlyList<ClassOptionDto> Classes { get; set; } = new List<ClassOptionDto>();
        public IReadOnlyList<DepartmentOptionDto> Departments { get; set; } = new List<DepartmentOptionDto>();
    }
}
