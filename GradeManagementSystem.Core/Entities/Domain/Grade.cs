using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GradeManagementSystem.Core.Entities.Enums;

namespace GradeManagementSystem.Core.Entities.Domain
{
    public class Grade
    {
        [Key]
        public int GradeID { get; set; }

        
        [ForeignKey("Student")]
        public int StudentID { get; set; }

        [ForeignKey("Class")]
        public int ClassID { get; set; }

        public decimal Score { get; set; }


        public virtual Student Student { get; set; }
        public virtual Class Class { get; set; }
        
    }
}
