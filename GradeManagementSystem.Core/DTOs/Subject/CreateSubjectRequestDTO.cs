using System.ComponentModel.DataAnnotations;

namespace GradeManagementSystem.Core.DTOs.Subject
{
    public class CreateSubjectRequestDTO
    {
        [Required(ErrorMessage = "Subject name is required")]
        public string SubjectName { get; set; }

        [Required(ErrorMessage = "Academic year is required")]
        public string YearName { get; set; }

        [Required(ErrorMessage = "Stage is required")]
        public string Stage { get; set; }
    }
}
