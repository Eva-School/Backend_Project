using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace GradeManagementSystem.Core.DTOs.Vice
{
    public class ViceDashboardCardDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Route { get; set; } = string.Empty;
    }

    public class ViceRecentActivityDto
    {
        public string Id { get; set; } = string.Empty;
        public string TeacherName { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string ClassName { get; set; } = string.Empty;
        public string Level { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
    }

    public class ViceGradesDashboardResponseDto
    {
        public int TotalStudents { get; set; }
        public int TotalSubjects { get; set; }
        public int QuarterGradesPending { get; set; }
        public int FinalGradesPending { get; set; }
        public DateTime LastUpdated { get; set; }
        public List<ViceRecentActivityDto> RecentActivity { get; set; } = new();
    }
}

