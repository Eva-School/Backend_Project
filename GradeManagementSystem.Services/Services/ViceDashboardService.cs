using GradeManagementSystem.Core.DTOs.Vice;
using GradeManagementSystem.Core.Entities.Domain;
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

        public async Task<ViceGradesDashboardResponseDto> GetGradesDashboardAsync(string? academicYear = null)
        {
            var now = DateTime.UtcNow;

            AcademicYear? yearEntity = null;
            if (!string.IsNullOrWhiteSpace(academicYear))
            {
                var cleanYear = academicYear.Trim();
                yearEntity = await _context.AcademicYears.AsNoTracking()
                    .FirstOrDefaultAsync(y => y.YearName.Equals(cleanYear, StringComparison.OrdinalIgnoreCase) ||
                                              y.Stage.ToString().Equals(cleanYear, StringComparison.OrdinalIgnoreCase));
            }

            if (yearEntity == null)
            {
                yearEntity = await _context.AcademicYears.AsNoTracking()
                    .Where(y => y.IsActive)
                    .OrderByDescending(y => y.AcademicYearID)
                    .FirstOrDefaultAsync();
            }

            var yearId = yearEntity?.AcademicYearID;

            var studentsQuery = _context.Students.AsNoTracking();
            if (yearId.HasValue)
            {
                studentsQuery = studentsQuery.Where(s => s.CurrentAcademicYearID == yearId.Value);
            }
            var totalStudents = await studentsQuery.CountAsync();
            if (totalStudents == 0)
            {
                totalStudents = await _context.Students.AsNoTracking().CountAsync();
            }

            var subjectsQuery = _context.Subjects.AsNoTracking().Where(s => s.IsActive);
            if (yearId.HasValue)
            {
                subjectsQuery = subjectsQuery.Where(s => s.AcademicYearID == yearId.Value);
            }
            var totalSubjects = await subjectsQuery.CountAsync();
            if (totalSubjects == 0)
            {
                totalSubjects = await _context.Subjects.AsNoTracking().Where(s => s.IsActive).CountAsync();
            }

            var submittedKeys = _context.QuarterGradeSubmissions
                .AsNoTracking()
                .Select(x => new { x.StudentID, x.SubjectID, x.AcademicYearID, x.TermID });

            var pendingQuarterQuery = _context.StudentSubjectTermResults
                .AsNoTracking()
                .Where(r => r.StudentID.HasValue && r.SubjectID.HasValue);

            if (yearId.HasValue)
            {
                pendingQuarterQuery = pendingQuarterQuery.Where(r => r.AcademicYearID == yearId.Value);
            }

            var pendingQuarterGrades = await pendingQuarterQuery
                .Where(r => !submittedKeys.Any(sk =>
                    sk.StudentID == r.StudentID!.Value &&
                    sk.SubjectID == r.SubjectID!.Value &&
                    sk.AcademicYearID == r.AcademicYearID &&
                    sk.TermID == r.TermID))
                .CountAsync();

            var finalGradesQuery = _context.StudentAllResults.AsNoTracking();
            if (yearId.HasValue)
            {
                finalGradesQuery = finalGradesQuery.Where(ar => ar.AcademicYearID == yearId.Value);
            }

            var finalGradesPending = await finalGradesQuery
                .Where(ar => ar.ResultApproval == null || ar.ResultApproval.Decision == Core.Entities.Enums.Decision.Pending)
                .CountAsync();

            var actionLogsQuery = _context.GradeActionLogs.AsNoTracking();
            if (yearId.HasValue)
            {
                actionLogsQuery = actionLogsQuery.Where(x => x.AcademicYearID == yearId.Value);
            }

            var lastUpdated = await actionLogsQuery
                .OrderByDescending(x => x.Timestamp)
                .Select(x => x.Timestamp)
                .FirstOrDefaultAsync();

            var recentActivity = await actionLogsQuery
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

            if (recentActivity.Count == 0)
            {
                recentActivity = await _context.GradeActionLogs
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
            }

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
