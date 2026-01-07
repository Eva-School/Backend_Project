using GradeManagementSystem.Core.Entities.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GradeManagementSystem.Core.Entities.Domain
{
    public class StudentAllResults
    {
        [Key]
        public int AllResultID { get; set; }

        [ForeignKey("Student")]
        public int? StudentID { get; set; }

        [ForeignKey("Subject")]
        public int? SubjectID { get; set; }

        [ForeignKey("Term")]
        public int? TermID { get; set; }

        [ForeignKey("AcademicYear")]
        public int? AcademicYearID { get; set; }

        public decimal? FinalSubjectScore { get; set; }

        public decimal? TotalTermScore { get; set; }

        public SubjectStatus? SubjectStatus { get; set; }

        public OverallTermStatus? OverallTermStatus { get; set; }

        public DateTime? GeneratedAt { get; set; }

        public DateTime? ApprovedAt { get; set; }

        // Navigation Properties
        public virtual Student Student { get; set; }
        public virtual Subject Subject { get; set; }
        public virtual Term Term { get; set; }
        public virtual AcademicYear AcademicYear { get; set; }
        public virtual ResultApproval? ResultApproval { get; set; }
    }
}
