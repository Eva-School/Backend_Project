namespace GradeManagementSystem.Core.DTOs.Student
{
    public class StudentCardDto
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
    }

    public class StudentProfileDto
    {
        public string Name { get; set; } = string.Empty;
        public string Year { get; set; } = string.Empty;
        public string Subtitle { get; set; } = "Your academic overview";
        public string CurrentAcademicYear { get; set; } = string.Empty;
    }

    public class StudentYearOptionDto
    {
        public string Id { get; set; } = string.Empty;
        public string Number { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
    }

    public class StudentGradeItemDto
    {
        public string Subject { get; set; } = string.Empty;
        public decimal YourGrade { get; set; }
        public decimal QuarterGrade { get; set; }
    }

    public class StudentGradesResponseDto
    {
        public List<StudentGradeItemDto> Grades { get; set; } = new();
        public string Year { get; set; } = string.Empty;
    }

    public class StudentCompetencyGradeItemDto
    {
        public string Jadarat { get; set; } = string.Empty;
        public string Your_Attemps { get; set; } = string.Empty;
        public string Attemps { get; set; } = string.Empty;
    }

    public class StudentCompetenciesResponseDto
    {
        public List<StudentCompetencyGradeItemDto> Grades { get; set; } = new();
        public string Year { get; set; } = string.Empty;
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
