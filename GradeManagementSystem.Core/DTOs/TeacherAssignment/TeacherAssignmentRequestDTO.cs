using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GradeManagementSystem.Core.DTOs.TeacherAssignment
{
    public class TeacherAssignmentRequestDTO
    {
        [Required(ErrorMessage = "TeacherId is required")]
        public string TeacherId { get; set; }

        [Required(ErrorMessage = "YearId is required")]
        public string YearId { get; set; }

        [Required(ErrorMessage = "SubjectId is required")]
        public string SubjectId { get; set; }

        [Required(ErrorMessage = "ClassIds are required")]
        [MinLength(1, ErrorMessage = "ClassIds must not be empty")]
        public List<int> ClassIds { get; set; }
    }
}
