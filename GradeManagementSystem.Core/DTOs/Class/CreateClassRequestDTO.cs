using System.ComponentModel.DataAnnotations;

namespace GradeManagementSystem.Core.DTOs.Class
{
    public class CreateClassRequestDTO
    {
        [Required]
        [StringLength(100)]
        public string YearId { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string Department { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string ClassName { get; set; } = string.Empty;

        [Range(1, 500)]
        public int? Capacity { get; set; }
    }
}
