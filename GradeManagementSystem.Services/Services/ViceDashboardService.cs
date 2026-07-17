using GradeManagementSystem.Core.DTOs.Vice;
using GradeManagementSystem.Core.Interfaces;
using GradeManagementSystem.Repository.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GradeManagementSystem.Services.Services
{
    public class ViceDashboardService : IViceDashboardService
    {
        private readonly GradeDbContext _context;

        public ViceDashboardService(GradeDbContext context)
        {
            _context = context;
        }

        public Task<IEnumerable<ViceDashboardCardDto>> GetCardsAsync()
        {
            IEnumerable<ViceDashboardCardDto> cards = new[]
            {
                new ViceDashboardCardDto
                {
                    Id = 1,
                    Title = "Teacher",
                    Description = "Add teachers and assign them to subjects.",
                    Route = "/vice/teachers"
                },
                new ViceDashboardCardDto
                {
                    Id = 2,
                    Title = "Student",
                    Description = "Manage classes and student enrollment.",
                    Route = "/vice/students"
                },
                new ViceDashboardCardDto
                {
                    Id = 3,
                    Title = "Grades",
                    Description = "Manage quarter and final grades setup.",
                    Route = "/vice/grades"
                }
            };

            return Task.FromResult(cards);
        }

        public async Task<ViceGradesDashboardResponseDto> GetGradesDashboardAsync()
        {
            var now = DateTime.UtcNow;

            var totalStudents = await _context.Students.CountAsync();
            var totalSubjects = await _context.Subjects.Where(s => s.IsActive).CountAsync();

            // Pending quarter grades = StudentSubjectTermResults not present in QuarterGradeSubmissions for the same (Student,Subject,AcademicYear,Term)
            var submittedKeys = _context.QuarterGradeSubmissions
                .AsNoTracking()
                .Select(x => new { x.StudentID, x.SubjectID, x.AcademicYearID, x.TermID });

            var pendingQuarterGrades = await _context.StudentSubjectTermResults
                .AsNoTracking()
                .Where(r => r.StudentID.HasValue && r.SubjectID.HasValue)
                .Where(r => !submittedKeys.Any(sk =>
                    sk.StudentID == r.StudentID!.Value &&
                    sk.SubjectID == r.SubjectID!.Value &&
                    sk.AcademicYearID == r.AcademicYearID &&
                    sk.TermID == r.TermID))
                .CountAsync();

            // Pending final grades = StudentAllResults where ResultApproval doesn't exist or is Pending.
            var finalGradesPending = await _context.StudentAllResults
                .AsNoTracking()
                .Where(ar => ar.ResultApproval == null || ar.ResultApproval.Decision == Core.Entities.Enums.Decision.Pending)
                .CountAsync();

            var lastUpdated = await _context.GradeActionLogs
                .AsNoTracking()
                .OrderByDescending(x => x.Timestamp)
                .Select(x => x.Timestamp)
                .FirstOrDefaultAsync();

            var recentActivity = await _context.GradeActionLogs
                .AsNoTracking()
                .OrderByDescending(x => x.Timestamp)
                .Take(20)
                .Select(x => new ViceRecentActivityDto
                {
                    Id = x.ActionLogID.ToString(),
                    TeacherName = x.ActorName ?? string.Empty,
                    Action = x.Action ?? string.Empty,
                    Subject = x.SubjectName ?? string.Empty,
                    ClassName = x.ClassName ?? string.Empty,
                    Level = x.Level ?? string.Empty,
                    Timestamp = x.Timestamp
                })
                .ToListAsync();

            return new ViceGradesDashboardResponseDto
            {
                TotalStudents = totalStudents,
                TotalSubjects = totalSubjects,
                QuarterGradesPending = pendingQuarterGrades,
                FinalGradesPending = finalGradesPending,
                LastUpdated = lastUpdated == default ? now : lastUpdated,
                RecentActivity = recentActivity
            };
        }
    }
}
