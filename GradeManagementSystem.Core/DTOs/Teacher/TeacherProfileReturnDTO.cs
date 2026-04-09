using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GradeManagementSystem.Core.DTOs.Teacher
{
    public class TeacherProfileReturnDTO
    {
        public string Name { get; set; }
        public string? subtitle { get; set; }
        public string? currentAcademicYear { get; set; }
    }
}
