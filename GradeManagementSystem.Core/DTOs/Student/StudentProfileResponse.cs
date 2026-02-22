using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GradeManagementSystem.Core.DTOs.Student
{
    public class StudentProfileResponse
    {
        public string Name { get; set; }
        public string? Year { get; set; }
        public string? Subtitle { get; set; }
        public string? CurrentAcademicYear { get; set; }
    }
}
