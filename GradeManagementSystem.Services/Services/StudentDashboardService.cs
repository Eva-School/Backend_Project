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
                new StudentCardDto
                {
                    Id = "quarter-grades",
                    Title = "Quarter Grades",
                    Description = "Review your quarter assessment results.",
                    Route = "/student/quarter"
                },
                new StudentCardDto
                {
                    Id = "final-grades",
                    Title = "Final Grades",
                    Description = "Review your final examination results.",
                    Route = "/student/final"
                },
                new StudentCardDto
                {
                    Id = "competencies",
                    Title = "Competencies",
                    Description = "Review your Jadarat competency progress.",
                    Route = "/student/jadarat"
                }
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

            var grades = await _context.StudentAllResults
                .Where(r => r.StudentID == contextInfo.Value.StudentId && r.AcademicYearID == contextInfo.Value.AcademicYearId && r.ResultApproval != null && r.ResultApproval.Decision == Decision.Approved)
                .Include(r => r.Subject)
                .Select(r => new
                {
                    r.Subject.SubjectName,
                    YourGrade = r.FinalSubjectScore ?? 0,
                    QuarterGrade = _context.StudentSubjectTermResults
                        .Where(q => q.StudentID == r.StudentID && q.SubjectID == r.SubjectID && q.TermID == r.TermID && q.AcademicYearID == r.AcademicYearID)
                        .Select(q => (q.Quarter1Score ?? 0) + (q.Quarter2Score ?? 0) + (q.Quarter3Score ?? 0) + (q.Quarter4Score ?? 0))
                        .FirstOrDefault()
                })
                .Select(r => new StudentGradeItemDto
                {
                    Subject = r.SubjectName,
                    YourGrade = r.YourGrade,
                    QuarterGrade = r.QuarterGrade
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

            var results = await _context.StudentSubjectTermResults
                .AsNoTracking()
                .Where(r => r.StudentID == contextInfo.Value.StudentId && r.AcademicYearID == contextInfo.Value.AcademicYearId)
                .Select(r => new
                {
                    r.Subject.SubjectName,
                    r.Quarter1Score,
                    r.Quarter2Score,
                    r.Quarter3Score,
                    r.Quarter4Score,
                    FinalExamScore = _context.StudentAllResults
                        .Where(ar => ar.StudentID == r.StudentID && ar.SubjectID == r.SubjectID && ar.TermID == r.TermID && ar.AcademicYearID == r.AcademicYearID && ar.ResultApproval != null && ar.ResultApproval.Decision == Decision.Approved)
                        .Select(ar => ar.FinalSubjectScore)
                        .FirstOrDefault(),
                    r.Subject.MaxQuarterQ1Score,
                    r.Subject.MaxQuarterQ2Score,
                    r.Subject.MaxQuarterQ3Score,
                    r.Subject.MaxQuarterQ4Score,
                    r.Subject.MaxQuarterScore,
                    r.Subject.MaxFinalScore
                })
                .ToListAsync();

            return results.Select(item =>
            {
                var quarterPercentages = new[]
                {
                    ToPercentage(item.Quarter1Score, item.MaxQuarterQ1Score ?? item.MaxQuarterScore),
                    ToPercentage(item.Quarter2Score, item.MaxQuarterQ2Score ?? item.MaxQuarterScore),
                    ToPercentage(item.Quarter3Score, item.MaxQuarterQ3Score ?? item.MaxQuarterScore),
                    ToPercentage(item.Quarter4Score, item.MaxQuarterQ4Score ?? item.MaxQuarterScore)
                }
                .Where(value => value.HasValue)
                .Select(value => value!.Value)
                .ToList();

                return new StudentProgressPointDto
                {
                    Subject = item.SubjectName,
                    QuarterAverage = quarterPercentages.Count == 0 ? 0 : Math.Round(quarterPercentages.Average(), 1),
                    FinalExam = ToPercentage(item.FinalExamScore, item.MaxFinalScore) ?? 0
                };
            }).ToList();
        }

        public async Task<StudentReportDto?> GetReportAsync(int userId, string year)
        {
            if (string.IsNullOrWhiteSpace(year))
            {
                return null;
            }

            var student = await _context.Students
                .AsNoTracking()
                .Where(item => item.UserID == userId)
                .Select(item => new
                {
                    item.StudentID,
                    ClassName = item.Class != null ? item.Class.ClassName : "Unassigned"
                })
                .FirstOrDefaultAsync();
            if (student == null)
            {
                return null;
            }

            var academicYear = await _context.AcademicYears
                .AsNoTracking()
                .Where(item => item.IsActive && item.YearName == year.Trim())
                .OrderByDescending(item => item.AcademicYearID)
                .FirstOrDefaultAsync();

            if (academicYear == null && Enum.TryParse<EducationStage>(year, true, out var stage))
            {
                academicYear = await _context.AcademicYears
                    .AsNoTracking()
                    .Where(item => item.IsActive && item.Stage == stage)
                    .OrderByDescending(item => item.AcademicYearID)
                    .FirstOrDefaultAsync();
            }
            if (academicYear == null)
            {
                return null;
            }

            var resultRows = await _context.StudentSubjectTermResults
                .AsNoTracking()
                .Where(item => item.StudentID == student.StudentID && item.AcademicYearID == academicYear.AcademicYearID)
                .Select(item => new
                {
                    Subject = item.Subject.SubjectName,
                    item.Quarter1Score,
                    item.Quarter2Score,
                    item.Quarter3Score,
                    item.Quarter4Score,
                    FinalExamScore = _context.StudentAllResults
                        .Where(ar => ar.StudentID == item.StudentID && ar.SubjectID == item.SubjectID && ar.TermID == item.TermID && ar.AcademicYearID == item.AcademicYearID && ar.ResultApproval != null && ar.ResultApproval.Decision == Decision.Approved)
                        .Select(ar => ar.FinalSubjectScore)
                        .FirstOrDefault(),
                    item.Subject.MaxQuarterQ1Score,
                    item.Subject.MaxQuarterQ2Score,
                    item.Subject.MaxQuarterQ3Score,
                    item.Subject.MaxQuarterQ4Score,
                    item.Subject.MaxQuarterScore,
                    item.Subject.MaxFinalScore
                })
                .ToListAsync();

            var grades = resultRows
                .GroupBy(item => item.Subject)
                .OrderBy(group => group.Key)
                .Select(group =>
                {
                    var q1 = group.Average(item => item.Quarter1Score ?? 0m);
                    var q2 = group.Average(item => item.Quarter2Score ?? 0m);
                    var q3 = group.Average(item => item.Quarter3Score ?? 0m);
                    var q4 = group.Average(item => item.Quarter4Score ?? 0m);
                    var final = group.Average(item => item.FinalExamScore ?? 0m);
                    var points = group.SelectMany(item => new[]
                    {
                        new { Score = item.Quarter1Score, Maximum = (decimal)(item.MaxQuarterQ1Score ?? item.MaxQuarterScore ?? 100) },
                        new { Score = item.Quarter2Score, Maximum = (decimal)(item.MaxQuarterQ2Score ?? item.MaxQuarterScore ?? 100) },
                        new { Score = item.Quarter3Score, Maximum = (decimal)(item.MaxQuarterQ3Score ?? item.MaxQuarterScore ?? 100) },
                        new { Score = item.Quarter4Score, Maximum = (decimal)(item.MaxQuarterQ4Score ?? item.MaxQuarterScore ?? 100) },
                        new { Score = item.FinalExamScore, Maximum = (decimal)(item.MaxFinalScore ?? 100) }
                    }).Where(item => item.Score.HasValue).ToList();
                    var average = points.Count == 0
                        ? 0m
                        : Math.Round(points.Sum(item => item.Score!.Value) * 100m / points.Sum(item => item.Maximum), 1);

                    return new StudentReportGradeDto
                    {
                        Subject = group.Key,
                        Q1 = q1,
                        Q2 = q2,
                        Q3 = q3,
                        Q4 = q4,
                        Final = final,
                        Average = average
                    };
                })
                .ToList();

            var studentName = await _context.Users
                .AsNoTracking()
                .Where(item => item.UserId == userId)
                .Select(item => item.FullName)
                .FirstOrDefaultAsync() ?? "Student";

            return new StudentReportDto
            {
                StudentName = studentName,
                StudentId = student.StudentID.ToString(),
                ClassName = student.ClassName,
                Year = academicYear.YearName,
                Grades = grades
            };
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

        private static decimal? ToPercentage(decimal? score, int? maximum)
        {
            if (!score.HasValue)
            {
                return null;
            }

            var denominator = maximum.GetValueOrDefault(100);
            return denominator <= 0 ? 0m : Math.Round(score.Value * 100m / denominator, 1);
        }
    }
}
