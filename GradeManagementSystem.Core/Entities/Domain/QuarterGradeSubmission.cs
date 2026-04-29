using System;

namespace GradeManagementSystem.Core.Entities.Domain
{
    public class QuarterGradeSubmission
    {
        public int SubmissionID { get; set; }

        public int StudentID { get; set; }
        public int SubjectID { get; set; }

        public int AcademicYearID { get; set; }
        public int TermID { get; set; }

        public DateTime SubmittedAt { get; set; }

        public int? SubmittedBy { get; set; }
    }
}

