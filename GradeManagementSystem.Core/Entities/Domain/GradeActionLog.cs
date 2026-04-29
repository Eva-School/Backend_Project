using System;

namespace GradeManagementSystem.Core.Entities.Domain
{
    public class GradeActionLog
    {
        public int ActionLogID { get; set; }

        public string Action { get; set; } = string.Empty;

        public int? ActorUserID { get; set; }
        public string? ActorName { get; set; }

        public int? StudentID { get; set; }
        public int? SubjectID { get; set; }

        public int? AcademicYearID { get; set; }
        public int? DepartmentID { get; set; }

        public int? ClassID { get; set; }
        public int? TermID { get; set; }

        public string? Level { get; set; }
        public string? SubjectName { get; set; }
        public string? ClassName { get; set; }

        public decimal? BeforeFinalScore { get; set; }
        public decimal? AfterFinalScore { get; set; }

        public DateTime Timestamp { get; set; }
    }
}

