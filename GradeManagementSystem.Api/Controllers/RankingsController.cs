using GradeManagementSystem.Repository.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GradeManagementSystem.Api.Controllers
{
    [ApiController]
    [Route("api/rankings")]
    [Authorize(Roles = "Student Affairs,StudentAffairs,Admin,Teacher")]
    public class RankingsController : ControllerBase
    {
        private readonly GradeDbContext _context;

        public RankingsController(GradeDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetRankings([FromQuery] string year, [FromQuery] int? classId, [FromQuery] int limit = 20)
        {
            if (string.IsNullOrWhiteSpace(year))
            {
                return BadRequest(new { message = "year is required." });
            }
            if (limit is < 1 or > 100)
            {
                return BadRequest(new { message = "limit must be between 1 and 100." });
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

            var query = _context.StudentAllResults
                .AsNoTracking()
                .Where(item => item.AcademicYearID == academicYear.AcademicYearID && item.FinalSubjectScore.HasValue && item.StudentID.HasValue);
            if (classId.HasValue)
            {
                query = query.Where(item => item.Student.ClassID == classId.Value);
            }

            var rows = await query
                .Join(_context.Students,
                    result => result.StudentID!.Value,
                    student => student.StudentID,
                    (result, student) => new { Result = result, Student = student })
                .Join(_context.Users,
                    row => row.Student.UserID!.Value,
                    user => user.UserId,
                    (row, user) => new { row.Result, row.Student, User = user })
                .Join(_context.Classes,
                    row => row.Student.ClassID!.Value,
                    schoolClass => schoolClass.ClassID,
                    (row, schoolClass) => new
                    {
                        StudentId = row.Student.StudentID,
                        Name = row.User.FullName,
                        ClassName = schoolClass.ClassName,
                        Score = row.Result.FinalSubjectScore!.Value
                    })
                .ToListAsync();

            var rankings = rows
                .GroupBy(item => new { item.StudentId, item.Name, item.ClassName })
                .OrderByDescending(group => group.Average(item => item.Score))
                .ThenBy(group => group.Key.Name)
                .Take(limit)
                .Select((group, index) => new
                {
                    rank = index + 1,
                    studentId = group.Key.StudentId.ToString(),
                    name = group.Key.Name ?? "Student",
                    className = group.Key.ClassName,
                    average = Math.Round(group.Average(item => item.Score), 1),
                    totalGrades = group.Count(),
                    trend = "stable",
                    badge = index switch { 0 => "gold", 1 => "silver", 2 => "bronze", _ => null }
                })
                .ToList();

            return Ok(new { rankings, total = rankings.Count, year = academicYear.YearName, classId });
        }
    }
}
