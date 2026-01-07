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
    public class Student
    {
        [Key]
        public int StudentID { get; set; }

        public int? UserID { get; set; }

        [StringLength(50)]
        public string NationalID { get; set; }

        public DateTime? EnrollmentDate { get; set; }

        [ForeignKey("CurrentAcademicYear")]
        public int? CurrentAcademicYearID { get; set; }

        [ForeignKey("Major")]
        public int? MajorID { get; set; }

        [ForeignKey("Class")]
        public int? ClassID { get; set; }

        [StringLength(50)]
        public string Status { get; set; }

        [Required]
        public Gender Gender { get; set; }

        // Navigation Properties
        public virtual AcademicYear? CurrentAcademicYear { get; set; }
        public virtual Major? Major { get; set; }
        public virtual Class? Class { get; set; }
        public virtual ICollection<Guardian> Guardians { get; set; } = new List<Guardian>();
        public virtual ICollection<PreviousSchools> PreviousSchools { get; set; } = new List<PreviousSchools>();
        public virtual ICollection<StudentCompetencyStatus> StudentCompetencyStatuses { get; set; } = new List<StudentCompetencyStatus>();
        public virtual ICollection<CompetencyAttempt> CompetencyAttempts { get; set; } = new List<CompetencyAttempt>();
        public virtual ICollection<StudentSubjectTermResult> SubjectTermResults { get; set; } = new List<StudentSubjectTermResult>();
        public virtual ICollection<StudentAllResults> AllResults { get; set; } = new List<StudentAllResults>();
        public virtual ICollection<StudentPromotion> PromotionsFrom { get; set; } = new List<StudentPromotion>();
    }
}
