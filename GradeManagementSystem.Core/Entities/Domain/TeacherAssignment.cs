using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GradeManagementSystem.Core.Entities.Domain
{
    public class TeacherAssignment
    {
        [Key]
        public int TeacherAssignmentID { get; set; }

        [ForeignKey("Teacher")]
        public int? TeacherID { get; set; }

        [ForeignKey("Class")]
        public int? ClassID { get; set; }

        [ForeignKey("Subject")]
        public int? SubjectID { get; set; }

        [ForeignKey("AcademicYear")]
        public int? AcademicYearID { get; set; }

        public DateTime? AssignedAt { get; set; }

        [Required]
        public bool IsActive { get; set; }

        // Navigation Properties
        public virtual Teacher Teacher { get; set; }
        public virtual Class Class { get; set; }
        public virtual Subject Subject { get; set; }
        public virtual AcademicYear AcademicYear { get; set; }
    }
}
