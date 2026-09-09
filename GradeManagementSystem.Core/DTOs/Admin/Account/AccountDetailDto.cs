using System;

namespace GradeManagementSystem.Core.DTOs.Admin.Account
{
    public class AccountDetailDto : AccountSummaryDto
    {
        public bool EmailConfirmed { get; set; }
        public bool PhoneNumberConfirmed { get; set; }
        public bool TwoFactorEnabled { get; set; }
        public DateTimeOffset? LockoutEnd { get; set; }
        public int AccessFailedCount { get; set; }

        public TeacherProfileDto? TeacherProfile { get; set; }
        public StudentProfileDto? StudentProfile { get; set; }
    }
}
