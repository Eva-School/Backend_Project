using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GradeManagementSystem.Core.Entities.Enums;

namespace GradeManagementSystem.Core.DTOs.Teacher
{
    public class TeacherSubmitGradeDto
    {

        public int StudentID { get; set; }
        public int ClassID { get; set; }

        public decimal Score { get; set; }
       

    }
}
