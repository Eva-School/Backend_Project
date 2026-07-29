using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GradeManagementSystem.Core.Entities.Domain
{
    public class QuizGrade
    {
        [Key]
        public int QuizGradeID { get; set; }

        [ForeignKey("Quiz")]
        public int QuizID { get; set; }

        [ForeignKey("Student")]
        public int StudentID { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? Score { get; set; }

        [MaxLength(250)]
        public string? Notes { get; set; }

        public DateTime GradedAt { get; set; } = DateTime.UtcNow;

        // Navigation Properties
        public virtual Quiz Quiz { get; set; } = null!;
        public virtual Student Student { get; set; } = null!;
    }
}
