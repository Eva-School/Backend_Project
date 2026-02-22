using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GradeManagementSystem.Core.DTOs.Student
{
    public class GradesResponse<T>
    {
        public List<T> Grades { get; set; } = new List<T>();
        public string? Year { get; set; }
    }
}
