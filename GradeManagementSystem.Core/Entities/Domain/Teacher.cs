using GradeManagementSystem.Core.Entities.Identity;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GradeManagementSystem.Core.Entities.Domain
{
    public class Teacher
    {
        [Key]
        public int TeacherID { get; set; }

        public int? UserID { get; set; }


        [StringLength(50)]
        public string EmployeeCode { get; set; }

        [ForeignKey("Department")]
        public int? DepartmentID { get; set; }

        public DateTime? HireDate { get; set; }

        public string Qualifications { get; set; }

        [Required]
        public bool IsActive { get; set; }

        // Navigation Properties
        public virtual Department? Department { get; set; }
        public ApplicationUser? User { get; set; }
        public virtual ICollection<TeacherAssignment> TeacherAssignments { get; set; } = new List<TeacherAssignment>();
        public virtual ICollection<CompetencyAttempt> EvaluatedCompetencies { get; set; } = new List<CompetencyAttempt>();
    }
}
