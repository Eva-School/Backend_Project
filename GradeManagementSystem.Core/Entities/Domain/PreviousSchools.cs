using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GradeManagementSystem.Core.Entities.Domain
{
    public class PreviousSchools
    {
        [Key]
        public int PreviousSchoolID { get; set; }

        [ForeignKey("Student")]
        public int? StudentID { get; set; }

        [StringLength(200)]
        public string SchoolName { get; set; }

        // Navigation Properties
        public virtual Student Student { get; set; }
    }
}
