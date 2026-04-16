using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GradeManagementSystem.Core.DTOs.Student;

namespace GradeManagementSystem.Core.DTOs.Class
{
    public class ClassStudentsResponseDto
    {

        public int ClassId { get; set; }
        public string ClassName { get; set; }

        public List<StudentDto> Students { get; set; }
    }
}
