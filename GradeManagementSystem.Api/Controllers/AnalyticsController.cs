using GradeManagementSystem.Repository.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GradeManagementSystem.Api.Controllers
{
    [ApiController]
    [Route("api/analytics")]
    [Authorize(Roles = "Student Affairs,StudentAffairs,Admin")]
    public class AnalyticsController : ControllerBase
    {
        private static readonly string[] ChartColors = ["#FFC600", "#4CAF50", "#2196F3", "#9C27B0", "#FF5722", "#00BCD4"];
        private readonly GradeDbContext _context;

        public AnalyticsController(GradeDbContext context)
        {
            _context = context;
        }

        [HttpGet("overview")]
        public async Task<IActionResult> GetOverview([FromQuery] string year)
        {
            if (string.IsNullOrWhiteSpace(year))
            {
                return BadRequest(new { message = "year is required." });
            }

            var academicYear = await _context.AcademicYears
                .AsNoTracking()
                .Where(item => item.IsActive && item.YearName == year.Trim())
                .OrderByDescending(item => item.AcademicYearID)
                .FirstOrDefaultAsync();
            if (academicYear == null)
            {
                return NotFound(new { message = "The requested academic year was not found." });
            }

            var results = await _context.StudentAllResults
                .AsNoTracking()
                .Where(item => item.AcademicYearID == academicYear.AcademicYearID && item.FinalSubjectScore.HasValue)
                .Select(item => new
                {
                    Score = item.FinalSubjectScore!.Value,
                    item.StudentID,
                    item.SubjectID,
                    SubjectName = item.Subject.SubjectName,
                    ClassId = item.Student.ClassID,
                    ClassName = item.Student.Class != null ? item.Student.Class.ClassName : "Unassigned"
                })
                .ToListAsync();

            var scores = results.Select(item => item.Score).ToList();
            var subjectStats = results
                .Where(item => item.SubjectID.HasValue)
                .GroupBy(item => new { item.SubjectID, item.SubjectName })
                .OrderBy(group => group.Key.SubjectName)
                .Select((group, index) => new
                {
                    subject = group.Key.SubjectName,
                    average = Math.Round(group.Average(item => item.Score), 1),
                    highest = group.Max(item => item.Score),
                    lowest = group.Min(item => item.Score),
                    passRate = Math.Round(group.Count(item => item.Score >= 50m) * 100d / group.Count(), 1),
                    color = ChartColors[index % ChartColors.Length]
                })
                .ToList();

            var classRankings = results
                .GroupBy(item => new { item.ClassId, item.ClassName })
                .OrderByDescending(group => group.Average(item => item.Score))
                .Take(10)
                .Select((group, index) => new
                {
                    rank = index + 1,
                    className = group.Key.ClassName,
                    average = Math.Round(group.Average(item => item.Score), 1),
                    students = group.Where(item => item.StudentID.HasValue).Select(item => item.StudentID!.Value).Distinct().Count(),
                    trend = "stable"
                })
                .ToList();

            var distribution = new[]
            {
                new { label = "A+ (90-100)", count = scores.Count(score => score >= 90m), color = "#4CAF50" },
                new { label = "A (80-89)", count = scores.Count(score => score is >= 80m and < 90m), color = "#8BC34A" },
                new { label = "B (70-79)", count = scores.Count(score => score is >= 70m and < 80m), color = "#FFC600" },
                new { label = "C (60-69)", count = scores.Count(score => score is >= 60m and < 70m), color = "#FF9800" },
                new { label = "D (50-59)", count = scores.Count(score => score is >= 50m and < 60m), color = "#FF5722" },
                new { label = "F (0-49)", count = scores.Count(score => score < 50m), color = "#F44336" }
            };

            var quarterRows = await _context.StudentSubjectTermResults
                .AsNoTracking()
                .Where(item => item.AcademicYearID == academicYear.AcademicYearID)
                .Select(item => new
                {
                    item.Quarter1Score,
                    item.Quarter2Score,
                    item.Quarter3Score,
                    item.Quarter4Score,
                    item.Subject.MaxQuarterQ1Score,
                    item.Subject.MaxQuarterQ2Score,
                    item.Subject.MaxQuarterQ3Score,
                    item.Subject.MaxQuarterQ4Score,
                    item.Subject.MaxQuarterScore
                })
                .ToListAsync();
            var monthlyTrend = new[]
            {
                new { month = "Q1", scores = quarterRows.Select(item => ToPercentage(item.Quarter1Score, item.MaxQuarterQ1Score ?? item.MaxQuarterScore)).Where(score => score.HasValue).Select(score => score!.Value).ToList() },
                new { month = "Q2", scores = quarterRows.Select(item => ToPercentage(item.Quarter2Score, item.MaxQuarterQ2Score ?? item.MaxQuarterScore)).Where(score => score.HasValue).Select(score => score!.Value).ToList() },
                new { month = "Q3", scores = quarterRows.Select(item => ToPercentage(item.Quarter3Score, item.MaxQuarterQ3Score ?? item.MaxQuarterScore)).Where(score => score.HasValue).Select(score => score!.Value).ToList() },
                new { month = "Q4", scores = quarterRows.Select(item => ToPercentage(item.Quarter4Score, item.MaxQuarterQ4Score ?? item.MaxQuarterScore)).Where(score => score.HasValue).Select(score => score!.Value).ToList() }
            }
            .Where(item => item.scores.Count > 0)
            .Select(item => new { item.month, average = Math.Round(item.scores.Average(), 1) })
            .ToList();

            return Ok(new
            {
                totalStudents = results.Where(item => item.StudentID.HasValue).Select(item => item.StudentID!.Value).Distinct().Count(),
                averageGrade = scores.Count == 0 ? 0 : Math.Round(scores.Average(), 1),
                passRate = scores.Count == 0 ? 0 : Math.Round(scores.Count(score => score >= 50m) * 100d / scores.Count, 1),
                topPerformers = results.Where(item => item.StudentID.HasValue).GroupBy(item => item.StudentID).Count(group => group.Average(item => item.Score) >= 90m),
                subjectStats,
                classRankings,
                gradeDistribution = distribution,
                monthlyTrend
            });
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
