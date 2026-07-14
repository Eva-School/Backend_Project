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
}
