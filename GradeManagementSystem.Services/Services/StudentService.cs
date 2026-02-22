using GradeManagementSystem.Core.DTOs.Student;
using GradeManagementSystem.Core.Entities.Enums;
using GradeManagementSystem.Core.Entities.Identity;
using GradeManagementSystem.Core.Interfaces;
using GradeManagementSystem.Repository.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GradeManagementSystem.Services.Services
{
    public class StudentService : IStudentService
    {
        private readonly GradeDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public StudentService(GradeDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<StudentProfileResponse?> GetProfileAsync(int userId)
        {
            var user = await _userManager.Users
                .Include(u => u.Student)
                    .ThenInclude(s => s.CurrentAcademicYear)
                .FirstOrDefaultAsync(u => u.UserId == userId);

            if (user == null) return null;

            var student = user.Student;
            string? currentAcademicYear = null;
            string? yearLabel = null;

            if (student?.CurrentAcademicYear != null)
            {
                currentAcademicYear = student.CurrentAcademicYear.Stage.ToString().ToLower();
                yearLabel = student.CurrentAcademicYear.Stage switch
                {
                    EducationStage.Junior => "Year 1",
                    EducationStage.Wheeler => "Year 2",
                    EducationStage.Senior => "Year 3",
                    _ => null
                };
            }

            return new StudentProfileResponse
            {
                Name = user.FirstName ?? user.FullName ?? "Student",
                Year = yearLabel,
                Subtitle = "Your academic overview",
                CurrentAcademicYear = currentAcademicYear
            };
        }

        public Task<List<YearOptionResponse>> GetYearsAsync()
        {
            var years = new List<YearOptionResponse>
            {
                new YearOptionResponse { Id = "junior",  Number = "1", Title = "Junior"  },
                new YearOptionResponse { Id = "wheeler", Number = "2", Title = "Wheeler" },
                new YearOptionResponse { Id = "senior",  Number = "3", Title = "Senior"  }
            };

            return Task.FromResult(years);
        }

        public async Task<GradesResponse<QuarterGradeRow>?> GetQuarterGradesAsync(int userId, string year)
        {
            if (!Enum.TryParse<EducationStage>(year, true, out var stage))
                return null;

            var user = await _userManager.Users
                .Include(u => u.Student)
                .FirstOrDefaultAsync(u => u.UserId == userId);

            if (user?.Student == null) return null;

            var studentId = user.Student.StudentID;

            var academicYear = await _context.AcademicYears
                .FirstOrDefaultAsync(y => y.IsActive && y.Stage == stage);

            if (academicYear == null)
                return new GradesResponse<QuarterGradeRow> { Year = year };

            var rows = await _context.StudentSubjectTermResults
                .Include(r => r.Subject)
                .Where(r => r.StudentID == studentId && r.AcademicYearID == academicYear.AcademicYearID)
                .Select(r => new QuarterGradeRow
                {
                    Subject = r.Subject.SubjectName,
                    YourGrade = r.Quarter1Score,
                    QuarterGrade = r.Subject.MaxQuarterScore.HasValue
                        ? (decimal?)r.Subject.MaxQuarterScore.Value
                        : null
                })
                .ToListAsync();

            return new GradesResponse<QuarterGradeRow>
            {
                Grades = rows,
                Year = year
            };
        }

        public async Task<GradesResponse<FinalGradeRow>?> GetFinalGradesAsync(int userId, string year)
        {
            if (!Enum.TryParse<EducationStage>(year, true, out var stage))
                return null;

            var user = await _userManager.Users
                .Include(u => u.Student)
                .FirstOrDefaultAsync(u => u.UserId == userId);

            if (user?.Student == null) return null;

            var studentId = user.Student.StudentID;

            var academicYear = await _context.AcademicYears
                .FirstOrDefaultAsync(y => y.IsActive && y.Stage == stage);

            if (academicYear == null)
                return new GradesResponse<FinalGradeRow> { Year = year };

            var rows = await _context.StudentSubjectTermResults
                .Include(r => r.Subject)
                .Where(r => r.StudentID == studentId && r.AcademicYearID == academicYear.AcademicYearID)
                .Select(r => new FinalGradeRow
                {
                    Subject = r.Subject.SubjectName,
                    YourGrade = r.FinalExamScore,
                    QuarterGrade = r.Subject.MaxFinalScore.HasValue
                        ? (decimal?)r.Subject.MaxFinalScore.Value
                        : null
                })
                .ToListAsync();

            return new GradesResponse<FinalGradeRow>
            {
                Grades = rows,
                Year = year
            };
        }

        public async Task<GradesResponse<JadaratGradeRow>?> GetJadaratGradesAsync(int userId, string year)
        {
            if (!Enum.TryParse<EducationStage>(year, true, out var stage))
                return null;

            var user = await _userManager.Users
                .Include(u => u.Student)
                .FirstOrDefaultAsync(u => u.UserId == userId);

            if (user?.Student == null) return null;

            var studentId = user.Student.StudentID;

            // Get all StudentCompetencyStatuses for this student with their latest attempt
            var statuses = await _context.StudentCompetencyStatuses
                .Include(scs => scs.Competency)
                .Include(scs => scs.CompetencyAttempts)
                .Where(scs => scs.StudentID == studentId)
                .ToListAsync();

            var rows = statuses.Select(scs =>
            {
                // Get the latest attempt ordered by AttemptNumber
                var latestAttempt = scs.CompetencyAttempts
                    .OrderByDescending(a => a.AttemptNumber)
                    .FirstOrDefault();

                string result = latestAttempt?.Result?.Trim().ToLower() == "pass" ? "Pass" : "Fail";

                string attemptLabel = (latestAttempt?.AttemptNumber) switch
                {
                    1 => "Attemp-one",
                    2 => "Attemp-two",
                    3 => "Attemp-three",
                    _ => "Attemp-one"
                };

                return new JadaratGradeRow
                {
                    Jadarat = scs.Competency?.CompetencyName ?? "",
                    Your_Attemps = result,
                    Attemps = attemptLabel
                };
            }).ToList();

            return new GradesResponse<JadaratGradeRow>
            {
                Grades = rows,
                Year = year
            };
        }
    }
}
