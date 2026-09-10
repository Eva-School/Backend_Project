using System.ComponentModel.DataAnnotations;

namespace GradeManagementSystem.Core.DTOs.Class
{
    public class UpdateClassRequestDTO
    {
        [Required(ErrorMessage = "ClassName is required")]
        [StringLength(100)]
        public string ClassName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Department is required")]
        [StringLength(100)]
        public string Department { get; set; } = string.Empty;

        [Required(ErrorMessage = "Capacity is required")]
        [Range(1, 500, ErrorMessage = "Capacity must be between 1 and 500")]
        public int Capacity { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
