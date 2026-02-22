using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GradeManagementSystem.Core.DTOs.Student
{
    public class FinalGradeRow
    {
        public string Subject { get; set; }
        public decimal? YourGrade { get; set; }
        public decimal? QuarterGrade { get; set; }
    }
}
