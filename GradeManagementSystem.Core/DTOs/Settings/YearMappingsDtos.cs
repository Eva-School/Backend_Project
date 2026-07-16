using System.ComponentModel.DataAnnotations;

namespace GradeManagementSystem.Core.DTOs.Settings
{
    public class YearMappingsDto
    {
        public string Junior { get; set; } = string.Empty;
        public string Wheeler { get; set; } = string.Empty;
        public string Senior { get; set; } = string.Empty;
    }

public class UpdateYearMappingsRequestDto
    {
        [Required]
        public string Junior { get; set; } = string.Empty;

        [Required]
        public string Wheeler { get; set; } = string.Empty;

        [Required]
    public string Senior { get; set; } = string.Empty;
}

public class AcademicYearOptionDto
{
    public string YearName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public class CreateAcademicYearRequestDto
{
    [Required]
    [RegularExpression(@"^\d{4}-\d{4}$", ErrorMessage = "Year name must use the format YYYY-YYYY.")]
    public string YearName { get; set; } = string.Empty;

    [RegularExpression(@"^\d{4}-\d{4}$", ErrorMessage = "Source year must use the format YYYY-YYYY.")]
    public string? CopyFromYearName { get; set; }

    public bool CopyTerms { get; set; }
    public bool CopySubjects { get; set; }
    public bool CopyClasses { get; set; }
    public bool CopyTeacherAssignments { get; set; }

    // Students are moved to the newly-created year; their identity and prior
    // grades are retained rather than duplicated.
    public bool CarryStudents { get; set; }
    public bool ActivateImmediately { get; set; }
}

public class AcademicYearRolloverResultDto
{
    public string YearName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public int TermsCopied { get; set; }
    public int SubjectsCopied { get; set; }
    public int ClassesCopied { get; set; }
    public int TeacherAssignmentsCopied { get; set; }
    public int StudentsCarried { get; set; }
}
}
