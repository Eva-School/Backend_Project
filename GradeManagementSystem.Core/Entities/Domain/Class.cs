using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GradeManagementSystem.Core.Entities.Domain
{
    public class Class
    {
        [Key]
        public int ClassID { get; set; }

        [Required]
        [StringLength(100)]
        public string ClassName { get; set; }

        [ForeignKey("AcademicYear")]
        public int? AcademicYearID { get; set; }

        [ForeignKey("Department")]
        public int? DepartmentID { get; set; }

        public int? Capacity { get; set; }

        [Required]
        public bool IsActive { get; set; }

        // Navigation Properties
        public virtual AcademicYear? AcademicYear { get; set; }
        public virtual Department? Department { get; set; }
        public virtual ICollection<Student> Students { get; set; } = new List<Student>();
        public virtual ICollection<TeacherAssignment> TeacherAssignments { get; set; } = new List<TeacherAssignment>();
    }
}
