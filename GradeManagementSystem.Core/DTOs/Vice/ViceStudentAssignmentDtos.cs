using System.ComponentModel.DataAnnotations;

namespace GradeManagementSystem.Core.DTOs.Vice
{
    public class ViceStudentClassAssignmentRequestDTO
    {
        public int? ClassId { get; set; }
    }

    public class VicePromoteStudentsRequestDTO
    {
        [Required]
        public List<string> StudentIds { get; set; } = new();

        [Required]
        public string SourceLevel { get; set; } = string.Empty;

        [Required]
        public string TargetLevel { get; set; } = string.Empty;

        [Required]
        public string Department { get; set; } = string.Empty;
    }
}
