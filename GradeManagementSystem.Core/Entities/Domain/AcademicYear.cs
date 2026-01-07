using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace GradeManagementSystem.Core.Entities.Domain
{
    public class AcademicYear
    {
        [Key]
        public int AcademicYearID { get; set; }

        [Required]
        [StringLength(100)]
        public string YearName { get; set; }

        [Required]
        public int OrderNumber { get; set; }

        [Required]
        public bool IsActive { get; set; }

        // Navigation Properties
        public virtual ICollection<Term> Terms { get; set; } = new List<Term>();
        public virtual ICollection<Subject> Subjects { get; set; } = new List<Subject>();
        public virtual ICollection<Class> Classes { get; set; } = new List<Class>();
        public virtual ICollection<Student> Students { get; set; } = new List<Student>();
        public virtual ICollection<TeacherAssignment> TeacherAssignments { get; set; } = new List<TeacherAssignment>();
        public virtual ICollection<StudentSubjectTermResult> SubjectTermResults { get; set; } = new List<StudentSubjectTermResult>();
        public virtual ICollection<StudentAllResults> AllResults { get; set; } = new List<StudentAllResults>();
        public virtual ICollection<StudentPromotion> PromotionsFrom { get; set; } = new List<StudentPromotion>();
        public virtual ICollection<StudentPromotion> PromotionsTo { get; set; } = new List<StudentPromotion>();
    }
}
