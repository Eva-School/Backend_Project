using GradeManagementSystem.Core.DTOs.Class;
using System.Collections.Generic;

namespace GradeManagementSystem.Core.DTOs.TeacherAssignment
{
    public class TeacherAssignmentDashboardYearDto
    {
        public string YearId { get; set; } = string.Empty;
        public List<ClassResponseDTO> Classes { get; set; } = new();
    }
}

