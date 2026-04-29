using System;

namespace GradeManagementSystem.Core.Entities.Domain
{
    public class QuarterGradesLock
    {
        // Composite key: (AcademicYearID, SubjectID, DepartmentID, ClassID)
        public int AcademicYearID { get; set; }
        public int SubjectID { get; set; }
        public int DepartmentID { get; set; }
        public int ClassID { get; set; }

        public DateTime LockedAt { get; set; }

        public int? LockedBy { get; set; }
    }
}

