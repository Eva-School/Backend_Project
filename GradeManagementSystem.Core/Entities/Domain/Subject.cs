using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GradeManagementSystem.Core.Entities.Domain
{
    public class Subject
    {
        [Key]
        public int SubjectID { get; set; }

        [Required]
        [StringLength(100)]
        public string SubjectName { get; set; }

        [ForeignKey("AcademicYear")]
        public int? AcademicYearID { get; set; }

        public int? MaxFinalScore { get; set; }

        public int? MaxQuarterScore { get; set; }

        public int? MaxQuarterQ1Score { get; set; }

        public int? MaxQuarterQ2Score { get; set; }

        public int? MaxQuarterQ3Score { get; set; }

        public int? MaxQuarterQ4Score { get; set; }

        [Required]
        public bool IsActive { get; set; }

        // Navigation Properties
        public virtual AcademicYear? AcademicYear { get; set; }
        public virtual ICollection<TeacherAssignment> TeacherAssignments { get; set; } = new List<TeacherAssignment>();
        public virtual ICollection<StudentSubjectTermResult> SubjectTermResults { get; set; } = new List<StudentSubjectTermResult>();
        public virtual ICollection<StudentAllResults> AllResults { get; set; } = new List<StudentAllResults>();
    }
}
