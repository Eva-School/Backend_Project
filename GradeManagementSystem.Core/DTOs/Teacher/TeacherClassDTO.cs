using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GradeManagementSystem.Core.DTOs.Teacher
{
    public class TeacherClassDTO
    {
        public int Id { get; set; }         
        public string ClassName { get; set; } 
        public int? StudentCount { get; set; } 
 
    }
}
