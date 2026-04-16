using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GradeManagementSystem.Core.DTOs.Student
{
    public class StudentDto
    {
        public int Id { get; set; }
        public string Name { get; set; }

        public int? QuarterGrade { get; set; }
        public int? TeacherGrade { get; set; }
        public int? FinalGrade { get; set; }

        public string Status { get; set; }
    }
}
