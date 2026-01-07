using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GradeManagementSystem.Core.Entities.Domain
{
    public class Term
    {
        [Key]
        public int TermID { get; set; }

        [ForeignKey("AcademicYear")]
        public int? AcademicYearID { get; set; }

        [Required]
        [StringLength(100)]
        public string TermName { get; set; }

        public DateTime? StartDate { get; set; }

        public DateTime? EndDate { get; set; }

        // Navigation Properties
        public virtual AcademicYear AcademicYear { get; set; }
        public virtual ICollection<StudentSubjectTermResult> SubjectTermResults { get; set; } = new List<StudentSubjectTermResult>();
        public virtual ICollection<StudentAllResults> AllResults { get; set; } = new List<StudentAllResults>();
    }
}
