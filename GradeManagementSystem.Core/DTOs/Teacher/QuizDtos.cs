using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace GradeManagementSystem.Core.DTOs.Teacher
{
    public class CreateQuizRequestDto
    {
        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [Range(0.01, 1000, ErrorMessage = "MaxScore must be > 0")]
        public decimal MaxScore { get; set; }

        public DateTime? QuizDate { get; set; }

        [Required]
        public int ClassId { get; set; }

        [Required]
        public int SubjectId { get; set; }

        [MaxLength(500)]
        public string? Description { get; set; }
    }

    public class UpdateQuizRequestDto
    {
        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [Range(0.01, 1000, ErrorMessage = "MaxScore must be > 0")]
        public decimal MaxScore { get; set; }

        public DateTime? QuizDate { get; set; }

        [MaxLength(500)]
        public string? Description { get; set; }
    }

    public class QuizDto
    {
        public int QuizId { get; set; }
        public string Title { get; set; } = string.Empty;
        public decimal MaxScore { get; set; }
        public DateTime QuizDate { get; set; }
        public int ClassId { get; set; }
        public int SubjectId { get; set; }
        public int AcademicYearId { get; set; }
        public string? Description { get; set; }
        public DateTime CreatedAt { get; set; }
        public int GradedStudentsCount { get; set; }
        public int TotalStudentsCount { get; set; }
    }

    public class StudentQuizGradeDto
    {
        public int StudentId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public string? StudentCode { get; set; }
        public decimal? Score { get; set; }
        public string? Notes { get; set; }
        public DateTime? GradedAt { get; set; }
    }

    public class QuizDetailDto
    {
        public QuizDto Quiz { get; set; } = null!;
        public List<StudentQuizGradeDto> Grades { get; set; } = new List<StudentQuizGradeDto>();
    }

    public class StudentQuizGradeInputDto
    {
        [Required]
        public int StudentId { get; set; }

        public decimal? Score { get; set; }

        [MaxLength(250)]
        public string? Notes { get; set; }
    }

    public class UpsertQuizGradesRequestDto
    {
        [Required]
        public List<StudentQuizGradeInputDto> Grades { get; set; } = new List<StudentQuizGradeInputDto>();
    }
}
