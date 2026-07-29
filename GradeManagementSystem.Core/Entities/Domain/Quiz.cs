using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GradeManagementSystem.Core.Entities.Domain
{
    public class Quiz
    {
        [Key]
        public int QuizID { get; set; }

        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal MaxScore { get; set; }

        public DateTime QuizDate { get; set; } = DateTime.UtcNow;

        [ForeignKey("Class")]
        public int ClassID { get; set; }

        [ForeignKey("Subject")]
        public int SubjectID { get; set; }

        [ForeignKey("AcademicYear")]
        public int AcademicYearID { get; set; }

        [ForeignKey("Teacher")]
        public int CreatedByTeacherID { get; set; }

        [MaxLength(500)]
        public string? Description { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation Properties
        public virtual Class Class { get; set; } = null!;
        public virtual Subject Subject { get; set; } = null!;
        public virtual AcademicYear AcademicYear { get; set; } = null!;
        public virtual Teacher Teacher { get; set; } = null!;
        public virtual ICollection<QuizGrade> QuizGrades { get; set; } = new List<QuizGrade>();
    }
}
