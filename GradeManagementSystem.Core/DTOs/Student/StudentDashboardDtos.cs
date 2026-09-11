using System;
using System.Collections.Generic;

namespace GradeManagementSystem.Core.DTOs.Student
{
    public class StudentCardDto
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Route { get; set; } = string.Empty;
    }

    public class StudentProfileDto
    {
        public int StudentId { get; set; }
        public int? UserId { get; set; }
        public string StudentCode { get; set; } = string.Empty;
        public string NationalId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? NameArabic { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string Year { get; set; } = string.Empty;
        public string CurrentAcademicYear { get; set; } = string.Empty;
        public string AcademicYearName { get; set; } = string.Empty;
        public string ClassName { get; set; } = string.Empty;
        public string? Section { get; set; }
        public string DepartmentName { get; set; } = string.Empty;
        public string MajorName { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string? AddressArabic { get; set; }
        public string? FatherName { get; set; }
        public string? FatherPhone { get; set; }
        public string? RelativeName { get; set; }
        public string? RelativePhone { get; set; }
        public DateTime? EnrollmentDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public string Subtitle { get; set; } = "Your academic overview";
        public int TotalEnrolledSubjects { get; set; }
        public int CompletedCompetencies { get; set; }
        public int TotalCompetencies { get; set; }
        public decimal? OverallPercentage { get; set; }
    }

    public class UpdateStudentContactDto
    {
        public string? Phone { get; set; }
        public string? Address { get; set; }
        public string? AddressArabic { get; set; }
        public string? RelativeName { get; set; }
        public string? RelativePhone { get; set; }
    }

    public class StudentYearOptionDto
    {
        public string Id { get; set; } = string.Empty;
        public string Number { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
    }

    public class StudentQuizItemDto
    {
        public int QuizId { get; set; }
        public string Title { get; set; } = string.Empty;
        public decimal? Score { get; set; }
        public decimal MaxScore { get; set; }
        public DateTime QuizDate { get; set; }
        public string? Notes { get; set; }
    }

    public class StudentQuarterGradeItemDto
    {
        public int SubjectId { get; set; }
        public string Subject { get; set; } = string.Empty;
        public string? SubjectArabic { get; set; }
        public string? SubjectCode { get; set; }
        public decimal? Quarter1 { get; set; }
        public decimal? Quarter2 { get; set; }
        public decimal? Quarter3 { get; set; }
        public decimal? Quarter4 { get; set; }
        public decimal? MaxQ1 { get; set; }
        public decimal? MaxQ2 { get; set; }
        public decimal? MaxQ3 { get; set; }
        public decimal? MaxQ4 { get; set; }
        public decimal? MaxQuarter { get; set; }
        public decimal? CourseworkTotal { get; set; }
        public decimal? YourGrade { get; set; }
        public decimal? QuarterGrade { get; set; }
        public decimal? Percentage { get; set; }
        public List<StudentQuizItemDto> Quizzes { get; set; } = new();
    }

    public class StudentQuarterGradesResponseDto
    {
        public List<StudentQuarterGradeItemDto> Grades { get; set; } = new();
        public string Year { get; set; } = string.Empty;
        public string AcademicYearName { get; set; } = string.Empty;
        public List<int> AvailableTerms { get; set; } = new();
        public int SelectedTerm { get; set; }
    }

    // Keep StudentGradesResponseDto for backward compatibility if needed
    public class StudentGradesResponseDto
    {
        public List<StudentGradeItemDto> Grades { get; set; } = new();
        public string Year { get; set; } = string.Empty;
    }

    public class StudentGradeItemDto
    {
        public string Subject { get; set; } = string.Empty;
        public decimal YourGrade { get; set; }
        public decimal QuarterGrade { get; set; }
    }

    public class StudentFinalGradeItemDto
    {
        public int SubjectId { get; set; }
        public string Subject { get; set; } = string.Empty;
        public string? SubjectArabic { get; set; }
        public string? SubjectCode { get; set; }
        public int? TermId { get; set; }
        public string? TermName { get; set; }
        public int? CreditHours { get; set; }
        public decimal? CourseworkScore { get; set; }
        public decimal? FinalExamScore { get; set; }
        public decimal? TotalScore { get; set; }
        public decimal MaxScore { get; set; }
        public decimal? Percentage { get; set; }
        public string LetterGrade { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public bool IsApproved { get; set; }
        public decimal? YourGrade { get; set; }
        public decimal? QuarterGrade { get; set; }
    }

    public class StudentFinalGradesResponseDto
    {
        public List<StudentFinalGradeItemDto> Grades { get; set; } = new();
        public string Year { get; set; } = string.Empty;
        public string AcademicYearName { get; set; } = string.Empty;
        public decimal? CumulativeAverage { get; set; }
        public decimal TotalEarnedScore { get; set; }
        public decimal TotalMaxScore { get; set; }
        public int TotalSubjects { get; set; }
        public int PassedSubjects { get; set; }
        public string Standing { get; set; } = string.Empty;
    }

    public class CompetencyAttemptHistoryDto
    {
        public int AttemptId { get; set; }
        public int AttemptNumber { get; set; }
        public string Result { get; set; } = string.Empty;
        public DateTime? EvaluatedAt { get; set; }
        public string? EvaluatedByName { get; set; }
    }

    public class StudentCompetencyGradeItemDto
    {
        public int CompetencyId { get; set; }
        public string Jadarat { get; set; } = string.Empty;
        public string? MajorName { get; set; }
        public string CurrentStatus { get; set; } = string.Empty;
        public int CurrentAttempt { get; set; } = 1;
        public int MaxAttempts { get; set; } = 3;
        public DateTime? LastEvaluatedAt { get; set; }
        public string? EvaluatorName { get; set; }
        public string Your_Attemps { get; set; } = string.Empty;
        public string Attemps { get; set; } = string.Empty;
        public List<CompetencyAttemptHistoryDto> AttemptHistory { get; set; } = new();
    }

    public class StudentCompetenciesResponseDto
    {
        public List<StudentCompetencyGradeItemDto> Grades { get; set; } = new();
        public string Year { get; set; } = string.Empty;
        public string AcademicYearName { get; set; } = string.Empty;
        public int TotalCompetencies { get; set; }
        public int PassedCompetencies { get; set; }
        public int PendingCompetencies { get; set; }
    }

    public class StudentEnrolledTeacherDto
    {
        public int SubjectId { get; set; }
        public string SubjectName { get; set; } = string.Empty;
        public string? SubjectCode { get; set; }
        public string TeacherName { get; set; } = string.Empty;
        public string? TeacherEmail { get; set; }
    }

    public class StudentEnrollmentDetailsDto
    {
        public int StudentId { get; set; }
        public int? ClassId { get; set; }
        public string ClassName { get; set; } = string.Empty;
        public string Section { get; set; } = string.Empty;
        public string AcademicYearName { get; set; } = string.Empty;
        public string Stage { get; set; } = string.Empty;
        public List<StudentEnrolledTeacherDto> Teachers { get; set; } = new();
    }

    public class StudentProgressPointDto
    {
        public string Subject { get; set; } = string.Empty;
        public decimal QuarterAverage { get; set; }
        public decimal FinalExam { get; set; }
    }

    public class StudentReportGradeDto
    {
        public string Subject { get; set; } = string.Empty;
        public decimal Q1 { get; set; }
        public decimal Q2 { get; set; }
        public decimal Q3 { get; set; }
        public decimal Q4 { get; set; }
        public decimal Final { get; set; }
        public decimal Average { get; set; }
    }

    public class StudentReportDto
    {
        public string StudentName { get; set; } = string.Empty;
        public string StudentId { get; set; } = string.Empty;
        public string ClassName { get; set; } = string.Empty;
        public string Year { get; set; } = string.Empty;
        public List<StudentReportGradeDto> Grades { get; set; } = new();
    }
}
