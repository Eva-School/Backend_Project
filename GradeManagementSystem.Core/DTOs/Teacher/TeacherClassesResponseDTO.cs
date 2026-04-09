using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GradeManagementSystem.Core.DTOs.Teacher
{
    public class TeacherClassesResponseDTO
    {
        public List<TeacherClassDTO> Classes { get; set; }
        public string Year { get; set; }
        public string SubjectName { get; set; }
    }
}
