namespace GradeManagementSystem.Core.DTOs.Class
{
    public class ClassCohortSummaryDTO
    {
        public string Stage { get; set; } = string.Empty;
        public string StageName { get; set; } = string.Empty;
        public int AcademicYearId { get; set; }
        public string AcademicYearName { get; set; } = string.Empty;
        public int ClassCount { get; set; }
        public int TotalStudents { get; set; }
        public int TotalCapacity { get; set; }
    }
}
