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
    public class StudentSubjectTermResult
    {
        [Key]
        public int ResultID { get; set; }

        [ForeignKey("Student")]
        public int? StudentID { get; set; }

        [ForeignKey("Subject")]
        public int? SubjectID { get; set; }

        [ForeignKey("Term")]
        public int? TermID { get; set; }

        [ForeignKey("AcademicYear")]
        public int? AcademicYearID { get; set; }


        public decimal? Quarter1Score { get; set; }

        public decimal? Quarter3Score { get; set; }

        public decimal? Quarter2Score { get; set; }

        public decimal? Quarter4Score { get; set; }

        public decimal? FinalExamScore { get; set; }


        public decimal? TermTotal { get; set; }

        public SubjectStatus? Status { get; set; }

        public DateTime? CreatedAt { get; set; }

        public DateTime? LastUpdatedAt { get; set; }

        // Navigation Properties
        public virtual Student Student { get; set; }
        public virtual Subject Subject { get; set; }
        public virtual Term Term { get; set; }
        public virtual AcademicYear AcademicYear { get; set; }
    }
}
