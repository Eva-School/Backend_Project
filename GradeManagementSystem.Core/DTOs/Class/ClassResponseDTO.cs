using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GradeManagementSystem.Core.DTOs.Class
{
    public class ClassResponseDTO
    {
        public int ClassId { get; set; }
        public string ClassName { get; set; } = string.Empty;
        public int? DepartmentId { get; set; }
        public string? DepartmentName { get; set; }
        public int? Capacity { get; set; }
        public int StudentCount { get; set; }
        public bool IsActive { get; set; }
        public int? AcademicYearId { get; set; }
        public string? AcademicYearName { get; set; }
        public string? Stage { get; set; }
    }
}
