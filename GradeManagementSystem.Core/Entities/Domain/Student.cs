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

        [ForeignKey("Department")]
        public int? DepartmentID { get; set; }

        [ForeignKey("Class")]
        public int? ClassID { get; set; }

        [StringLength(50)]
        public string Status { get; set; }

        [Required]
        public Gender Gender { get; set; }

        [StringLength(250)]
        public string? Address { get; set; }

        [StringLength(50)]
        public string? StudentCode { get; set; }

        [StringLength(250)]
        public string? NameArabic { get; set; }

        [StringLength(250)]
        public string? NameEnglish { get; set; }

        [StringLength(100)]
        public string? Nationality { get; set; }

        public DateTime? DateOfBirth { get; set; }

        [StringLength(150)]
        public string? PlaceOfBirth { get; set; }

        [StringLength(250)]
        public string? AddressArabic { get; set; }

        [StringLength(250)]
        public string? Email { get; set; }

        [StringLength(100)]
        public string? Governorate { get; set; }

        [StringLength(200)]
        public string? FatherName { get; set; }

        [StringLength(30)]
        public string? FatherPhone { get; set; }

        [StringLength(150)]
        public string? FatherProfession { get; set; }

        [StringLength(200)]
        public string? MotherName { get; set; }

        [StringLength(30)]
        public string? MotherPhone { get; set; }

        [StringLength(150)]
        public string? MotherProfession { get; set; }

        [StringLength(200)]
        public string? RelativeName { get; set; }

        [StringLength(30)]
        public string? RelativePhone { get; set; }

        [StringLength(50)]
        public string? Religion { get; set; }

        [StringLength(30)]
        public string? StudentPhone { get; set; }

        [StringLength(500)]
        public string? HealthProblems { get; set; }

        [StringLength(500)]
        public string? MissingDocumentation { get; set; }

        public bool DocumentsDelivered { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? PreparatoryGrade { get; set; }

        public bool FeesPaid { get; set; }

        // Navigation Properties
        public virtual AcademicYear? CurrentAcademicYear { get; set; }
        public virtual Major? Major { get; set; }
        public virtual Department? Department { get; set; }
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
