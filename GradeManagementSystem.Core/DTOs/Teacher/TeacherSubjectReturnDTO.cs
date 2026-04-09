using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GradeManagementSystem.Core.DTOs.Teacher
{
    public class TeacherSubjectReturnDTO
    {
            public int Id { get; set; }          // Subject ID
            public string Title { get; set; }    // Academic Year Title
            public string SubjectName { get; set; }
            public string Year { get; set; }     // junior | wheeler | senior
            public string Route { get; set; } // Optional default route
       
    }
}
