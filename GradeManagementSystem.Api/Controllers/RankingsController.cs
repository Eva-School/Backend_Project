using GradeManagementSystem.Core.Entities.Domain;
using GradeManagementSystem.Repository.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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
        public async Task<IActionResult> GetRankings([FromQuery] string? year, [FromQuery] int? classId, [FromQuery] int limit = 20)
        {
            if (limit is < 1 or > 100)
            {
                return BadRequest(new { message = "limit must be between 1 and 100." });
            }

            var cleanYear = year?.Trim() ?? string.Empty;

            AcademicYear? academicYear = null;
            if (!string.IsNullOrWhiteSpace(cleanYear))
            {
                academicYear = await _context.AcademicYears.AsNoTracking()
                    .FirstOrDefaultAsync(a => a.YearName.Equals(cleanYear, StringComparison.OrdinalIgnoreCase) ||
                                              a.Stage.ToString().Equals(cleanYear, StringComparison.OrdinalIgnoreCase));
            }

            if (academicYear == null)
            {
                academicYear = await _context.AcademicYears.AsNoTracking()
                    .Where(a => a.IsActive)
                    .OrderByDescending(a => a.AcademicYearID)
                    .FirstOrDefaultAsync()
                    ?? await _context.AcademicYears.AsNoTracking()
                    .OrderByDescending(a => a.AcademicYearID)
                    .FirstOrDefaultAsync();
            }

            if (academicYear == null)
            {
                return Ok(new { rankings = new List<object>(), total = 0, year = cleanYear });
            }

            var studentsQuery = _context.Students.AsNoTracking()
                .Include(s => s.Class)
                .Where(s => s.CurrentAcademicYearID == academicYear.AcademicYearID);

            if (classId.HasValue)
            {
                studentsQuery = studentsQuery.Where(s => s.ClassID == classId.Value);
            }

            var students = await studentsQuery.ToListAsync();

            if (students.Count == 0)
            {
                students = await _context.Students.AsNoTracking()
                    .Include(s => s.Class)
                    .Take(50)
                    .ToListAsync();
            }

            var userIds = students.Where(s => s.UserID.HasValue).Select(s => s.UserID!.Value).Distinct().ToList();
            var usersDict = await _context.Users.AsNoTracking()
                .Where(u => userIds.Contains(u.UserId))
                .ToDictionaryAsync(u => u.UserId, u => u.FullName);

            var termResults = await _context.StudentSubjectTermResults.AsNoTracking()
                .Where(r => r.StudentID.HasValue && (r.AcademicYearID == academicYear.AcademicYearID || r.AcademicYearID == null))
                .ToListAsync();

            var finalResults = await _context.StudentAllResults.AsNoTracking()
                .Where(r => r.StudentID.HasValue && r.FinalSubjectScore.HasValue)
                .ToListAsync();

            var studentScores = new List<(Student Student, string FullName, double Average, int TotalGrades, string Trend)>();

            foreach (var student in students)
            {
                var fullName = (student.UserID.HasValue && usersDict.TryGetValue(student.UserID.Value, out var name) && !string.IsNullOrWhiteSpace(name))
                    ? name
                    : (!string.IsNullOrWhiteSpace(student.NameArabic)
                        ? student.NameArabic
                        : (student.NameEnglish ?? $"Student {student.StudentCode}"));

                var studentTermRes = termResults.Where(r => r.StudentID == student.StudentID).ToList();
                var studentFinalRes = finalResults.Where(r => r.StudentID == student.StudentID).ToList();

                var collectedScores = new List<decimal>();

                foreach (var tr in studentTermRes)
                {
                    if (tr.TermTotal.HasValue && tr.TermTotal.Value > 0)
                    {
                        collectedScores.Add(tr.TermTotal.Value);
                    }
                    else
                    {
                        if (tr.Quarter1Score.HasValue) collectedScores.Add(tr.Quarter1Score.Value);
                        if (tr.Quarter2Score.HasValue) collectedScores.Add(tr.Quarter2Score.Value);
                        if (tr.Quarter3Score.HasValue) collectedScores.Add(tr.Quarter3Score.Value);
                        if (tr.Quarter4Score.HasValue) collectedScores.Add(tr.Quarter4Score.Value);
                        if (tr.FinalExamScore.HasValue) collectedScores.Add(tr.FinalExamScore.Value);
                    }
                }

                foreach (var fr in studentFinalRes)
                {
                    if (fr.FinalSubjectScore.HasValue)
                    {
                        collectedScores.Add(fr.FinalSubjectScore.Value);
                    }
                }

                if (collectedScores.Count > 0)
                {
                    double avg = Math.Round((double)collectedScores.Average(), 1);

                    string trend = "stable";
                    if (collectedScores.Count >= 2)
                    {
                        var half = collectedScores.Count / 2;
                        var firstHalf = (double)collectedScores.Take(half).Average();
                        var secondHalf = (double)collectedScores.Skip(half).Average();
                        if (secondHalf > firstHalf + 1) trend = "up";
                        else if (secondHalf < firstHalf - 1) trend = "down";
                    }

                    studentScores.Add((student, fullName, avg, collectedScores.Count, trend));
                }
            }

            var sortedRankings = studentScores
                .OrderByDescending(x => x.Average)
                .ThenBy(x => x.FullName)
                .Take(limit)
                .Select((x, index) => new
                {
                    rank = index + 1,
                    studentId = x.Student.StudentID.ToString(),
                    name = x.FullName,
                    className = x.Student.Class?.ClassName ?? "General",
                    average = x.Average,
                    totalGrades = x.TotalGrades,
                    trend = x.Trend,
                    badge = index switch { 0 => "gold", 1 => "silver", 2 => "bronze", _ => null }
                })
                .ToList();

            return Ok(new { rankings = sortedRankings, total = sortedRankings.Count, year = academicYear.YearName, classId });
        }
    }
}
