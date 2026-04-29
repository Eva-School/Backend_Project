using GradeManagementSystem.Core.DTOs.Student;
using GradeManagementSystem.Core.Entities.Enums;
using GradeManagementSystem.Core.Interfaces;
using GradeManagementSystem.Repository.Data;
using Microsoft.EntityFrameworkCore;

namespace GradeManagementSystem.Services.Services
{
    public class StudentDashboardService : IStudentDashboardService
    {
        private readonly GradeDbContext _context;

        public StudentDashboardService(GradeDbContext context)
        {
            _context = context;
        }

        public Task<IEnumerable<StudentCardDto>> GetCardsAsync()
        {
            IEnumerable<StudentCardDto> cards = new[]
            {
                new StudentCardDto { Id = "quarter-grades", Title = "Quarter Grades" },
                new StudentCardDto { Id = "final-grades", Title = "Final Grades" },
                new StudentCardDto { Id = "competencies", Title = "Competencies" }
            };

            return Task.FromResult(cards);
        }

        public async Task<StudentProfileDto?> GetProfileAsync(int userId)
        {
            var student = await _context.Students
                .Include(s => s.CurrentAcademicYear)
                .Include(s => s.Class)
                .Include(s => s.Major)
                .Where(s => s.UserID == userId)
                .Select(s => new
                {
                    YearStage = s.CurrentAcademicYear != null ? s.CurrentAcademicYear.Stage.ToString() : string.Empty
                })
                .FirstOrDefaultAsync();

            if (student == null)
            {
                return null;
            }

            var yearLabel = ToYearLabel(student.YearStage);
            return new StudentProfileDto
            {
                Name = await _context.Users.Where(u => u.UserId == userId).Select(u => u.FirstName).FirstOrDefaultAsync() ?? "Student",
                Year = yearLabel,
                Subtitle = "Your academic overview",
                CurrentAcademicYear = student.YearStage.ToLowerInvariant()
            };
        }

        public async Task<IEnumerable<StudentYearOptionDto>> GetYearsAsync()
        {
            var years = await _context.AcademicYears
                .Where(y => y.IsActive)
                .Select(y => y.Stage)
                .Distinct()
                .ToListAsync();

            return years
                .OrderBy(y => y)
                .Select(stage => new StudentYearOptionDto
                {
                    Id = stage.ToString().ToLowerInvariant(),
                    Number = ((int)stage + 1).ToString(),
                    Title = stage.ToString()
                })
                .ToList();
        }

        public async Task<StudentGradesResponseDto?> GetQuarterGradesAsync(int userId, string year)
        {
            var contextInfo = await ResolveContextAsync(userId, year);
            if (contextInfo == null)
            {
                return null;
            }

            var grades = await _context.StudentSubjectTermResults
                .Where(r => r.StudentID == contextInfo.Value.StudentId && r.AcademicYearID == contextInfo.Value.AcademicYearId)
                .Include(r => r.Subject)
                .Select(r => new StudentGradeItemDto
                {
                    Subject = r.Subject.SubjectName,
                    YourGrade = (r.Quarter1Score ?? 0) + (r.Quarter2Score ?? 0),
                    QuarterGrade = (r.Quarter1Score ?? 0) + (r.Quarter2Score ?? 0)
                })
                .ToListAsync();

            return new StudentGradesResponseDto
            {
                Grades = grades,
                Year = year.ToLowerInvariant()
            };
        }

        public async Task<StudentGradesResponseDto?> GetFinalGradesAsync(int userId, string year)
        {
            var contextInfo = await ResolveContextAsync(userId, year);
            if (contextInfo == null)
            {
                return null;
            }

            var grades = await _context.StudentSubjectTermResults
                .Where(r => r.StudentID == contextInfo.Value.StudentId && r.AcademicYearID == contextInfo.Value.AcademicYearId)
                .Include(r => r.Subject)
                .Select(r => new StudentGradeItemDto
                {
                    Subject = r.Subject.SubjectName,
                    YourGrade = r.FinalExamScore ?? 0,
                    QuarterGrade = r.FinalExamScore ?? 0
                })
                .ToListAsync();

            return new StudentGradesResponseDto
            {
                Grades = grades,
                Year = year.ToLowerInvariant()
            };
        }

        public async Task<StudentCompetenciesResponseDto?> GetJadaratGradesAsync(int userId, string year)
        {
            var contextInfo = await ResolveContextAsync(userId, year);
            if (contextInfo == null)
            {
                return null;
            }

            var grades = await _context.StudentCompetencyStatuses
                .Where(s => s.StudentID == contextInfo.Value.StudentId)
                .Include(s => s.Competency)
                .Select(s => new StudentCompetencyGradeItemDto
                {
                    Jadarat = s.Competency.CompetencyName,
                    Your_Attemps = s.StatusID,
                    Attemps = $"Attemp-{(s.CurrentAttemptNumber ?? 1)}"
                })
                .ToListAsync();

            return new StudentCompetenciesResponseDto
            {
                Grades = grades,
                Year = year.ToLowerInvariant()
            };
        }

        public async Task<IEnumerable<StudentProgressPointDto>> GetProgressAsync(int userId, string year)
        {
            var contextInfo = await ResolveContextAsync(userId, year);
            if (contextInfo == null)
            {
                return Enumerable.Empty<StudentProgressPointDto>();
            }

            var progress = await _context.StudentSubjectTermResults
                .Where(r => r.StudentID == contextInfo.Value.StudentId && r.AcademicYearID == contextInfo.Value.AcademicYearId)
                .Include(r => r.Subject)
                .Select(r => new StudentProgressPointDto
                {
                    Subject = r.Subject.SubjectName,
                    QuarterAverage = ((r.Quarter1Score ?? 0) + (r.Quarter2Score ?? 0)) / 2,
                    FinalExam = r.FinalExamScore ?? 0
                })
                .ToListAsync();

            return progress;
        }

        private async Task<(int StudentId, int AcademicYearId)?> ResolveContextAsync(int userId, string year)
        {
            if (!Enum.TryParse<EducationStage>(year, true, out var stage))
            {
                return null;
            }

            var studentId = await _context.Students
                .Where(s => s.UserID == userId)
                .Select(s => s.StudentID)
                .FirstOrDefaultAsync();

            if (studentId == 0)
            {
                return null;
            }

            var academicYearId = await _context.AcademicYears
                .Where(y => y.IsActive && y.Stage == stage)
                .OrderByDescending(y => y.AcademicYearID)
                .Select(y => y.AcademicYearID)
                .FirstOrDefaultAsync();

            if (academicYearId == 0)
            {
                return null;
            }

            return (studentId, academicYearId);
        }

        private static string ToYearLabel(string stage)
        {
            return stage.ToLowerInvariant() switch
            {
                "junior" => "Year 1",
                "wheeler" => "Year 2",
                "senior" => "Year 3",
                _ => "Year"
            };
        }
    }
}
