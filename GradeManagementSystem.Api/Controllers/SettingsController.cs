using GradeManagementSystem.Core.DTOs.Settings;
using GradeManagementSystem.Core.Entities.Domain;
using GradeManagementSystem.Core.Entities.Enums;
using GradeManagementSystem.Repository.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GradeManagementSystem.Api.Controllers
{
    [ApiController]
    [Route("api/settings")]
    [Authorize(Roles = "Student Affairs,StudentAffairs,Admin")]
    public class SettingsController : ControllerBase
    {
        private readonly GradeDbContext _context;

        public SettingsController(GradeDbContext context)
        {
            _context = context;
        }

        [HttpGet("year-mappings")]
        public async Task<IActionResult> GetYearMappings()
        {
            var mappings = await BuildMappingsAsync();
            return Ok(mappings);
        }

        [HttpPut("year-mappings")]
        public async Task<IActionResult> UpdateYearMappings([FromBody] UpdateYearMappingsRequestDto request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { message = "junior, wheeler, and senior mappings are required." });
            }

            var requested = new Dictionary<EducationStage, string>
            {
                [EducationStage.Junior] = request.Junior.Trim(),
                [EducationStage.Wheeler] = request.Wheeler.Trim(),
                [EducationStage.Senior] = request.Senior.Trim()
            };

            var academicYears = await _context.AcademicYears.ToListAsync();
            foreach (var mapping in requested)
            {
                var target = academicYears.FirstOrDefault(item => item.Stage == mapping.Key && item.YearName == mapping.Value);
                if (target == null)
                {
                    return BadRequest(new { message = $"No {mapping.Key} academic year named '{mapping.Value}' exists." });
                }

                foreach (var academicYear in academicYears.Where(item => item.Stage == mapping.Key))
                {
                    academicYear.IsActive = academicYear.AcademicYearID == target.AcademicYearID;
                }
            }

            await _context.SaveChangesAsync();
            return Ok(await BuildMappingsAsync());
        }

        [HttpGet("academic-years")]
        public async Task<IActionResult> GetAcademicYears()
        {
            var years = await _context.AcademicYears
                .AsNoTracking()
                .Select(item => new { item.YearName, item.Stage, item.IsActive })
                .ToListAsync();

            var options = years
                .GroupBy(item => item.YearName)
                .Select(group => new AcademicYearOptionDto
                {
                    YearName = group.Key,
                    IsActive = group.All(item => item.IsActive)
                })
                .OrderByDescending(item => item.YearName)
                .ToList();

            return Ok(options);
        }

        [HttpPost("academic-years")]
        public async Task<IActionResult> CreateAcademicYear([FromBody] CreateAcademicYearRequestDto request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { message = "Provide an academic year in YYYY-YYYY format." });
            }

            var targetYearName = request.YearName.Trim();
            if (!IsConsecutiveAcademicYear(targetYearName))
            {
                return BadRequest(new { message = "The end year must be exactly one year after the start year." });
            }

            var hasSource = !string.IsNullOrWhiteSpace(request.CopyFromYearName);
            var importsSelected = request.CopyTerms || request.CopySubjects || request.CopyClasses ||
                request.CopyTeacherAssignments || request.CarryStudents;
            if (!hasSource && importsSelected)
            {
                return BadRequest(new { message = "Choose a source academic year before importing data." });
            }

            if (request.CopyTeacherAssignments && (!request.CopyClasses || !request.CopySubjects))
            {
                return BadRequest(new { message = "Teacher assignments require both classes and subjects to be imported." });
            }

            if (request.CarryStudents && !request.CopyClasses)
            {
                return BadRequest(new { message = "Carrying students requires classes to be imported so their class memberships can be preserved." });
            }

            if (await _context.AcademicYears.AnyAsync(item => item.YearName == targetYearName))
            {
                return Conflict(new { message = $"The academic year '{targetYearName}' already exists." });
            }

            var stages = Enum.GetValues<EducationStage>();
            Dictionary<EducationStage, AcademicYear> sourceYears = new();
            if (hasSource)
            {
                var sourceYearName = request.CopyFromYearName!.Trim();
                var sourceRecords = await _context.AcademicYears
                    .Where(item => item.YearName == sourceYearName)
                    .OrderByDescending(item => item.AcademicYearID)
                    .ToListAsync();
                sourceYears = sourceRecords
                    .GroupBy(item => item.Stage)
                    .ToDictionary(group => group.Key, group => group.First());

                if (stages.Any(stage => !sourceYears.ContainsKey(stage)))
                {
                    return BadRequest(new { message = "The source academic year must include Junior, Wheeler, and Senior records." });
                }
            }

            var requestedBy = int.TryParse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value, out var userId)
                ? userId
                : (int?)null;
            var result = new AcademicYearRolloverResultDto
            {
                YearName = targetYearName,
                IsActive = request.ActivateImmediately
            };

            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                if (request.ActivateImmediately)
                {
                    var currentlyActive = await _context.AcademicYears
                        .Where(item => item.IsActive)
                        .ToListAsync();
                    foreach (var year in currentlyActive)
                    {
                        year.IsActive = false;
                    }
                }

                var targetYears = stages.ToDictionary(
                    stage => stage,
                    stage => new AcademicYear
                    {
                        YearName = targetYearName,
                        Stage = stage,
                        IsActive = request.ActivateImmediately
                    });
                _context.AcademicYears.AddRange(targetYears.Values);
                await _context.SaveChangesAsync();

                // A year without terms cannot be used by any grade-entry flow.
                // When terms are not imported, create the two standard terms so
                // the new year is immediately usable; dates remain configurable.
                if (!request.CopyTerms)
                {
                    foreach (var target in targetYears.Values)
                    {
                        _context.Terms.AddRange(
                            new Term { AcademicYearID = target.AcademicYearID, TermName = "Term 1", StartDate = DateTime.UtcNow, EndDate = DateTime.UtcNow.AddMonths(4) },
                            new Term { AcademicYearID = target.AcademicYearID, TermName = "Term 2", StartDate = DateTime.UtcNow.AddMonths(5), EndDate = DateTime.UtcNow.AddMonths(9) });
                    }
                }

                var classIds = new Dictionary<int, int>();
                var subjectIds = new Dictionary<int, int>();
                var copiedClasses = new List<(int SourceId, GradeManagementSystem.Core.Entities.Domain.Class Target)>();

                if (hasSource)
                {
                    foreach (var stage in stages)
                    {
                        var source = sourceYears[stage];
                        var target = targetYears[stage];

                        if (request.CopyTerms)
                        {
                            var terms = await _context.Terms.AsNoTracking()
                                .Where(item => item.AcademicYearID == source.AcademicYearID)
                                .ToListAsync();
                            foreach (var term in terms)
                            {
                                _context.Terms.Add(new Term
                                {
                                    AcademicYearID = target.AcademicYearID,
                                    TermName = term.TermName,
                                    StartDate = term.StartDate,
                                    EndDate = term.EndDate
                                });
                                result.TermsCopied++;
                            }
                        }

                        if (request.CopySubjects)
                        {
                            // Subjects are shared across academic years. Reuse the
                            // existing records instead of creating duplicates for
                            // the new year, while preserving assignment mappings.
                            var subjects = await _context.Subjects.AsNoTracking()
                                .Where(item => item.AcademicYearID == source.AcademicYearID && item.IsActive)
                                .ToListAsync();
                            foreach (var subject in subjects)
                            {
                                subjectIds[subject.SubjectID] = subject.SubjectID;
                                result.SubjectsCopied++;
                            }
                        }

                        if (request.CopyClasses)
                        {
                            var classes = await _context.Classes.AsNoTracking()
                                .Where(item => item.AcademicYearID == source.AcademicYearID && item.IsActive)
                                .ToListAsync();
                            foreach (var sourceClass in classes)
                            {
                                var copied = new GradeManagementSystem.Core.Entities.Domain.Class
                                {
                                    AcademicYearID = target.AcademicYearID,
                                    DepartmentID = sourceClass.DepartmentID,
                                    ClassName = sourceClass.ClassName,
                                    Capacity = sourceClass.Capacity,
                                    IsActive = true
                                };
                                _context.Classes.Add(copied);
                                copiedClasses.Add((sourceClass.ClassID, copied));
                                result.ClassesCopied++;
                            }
                        }
                    }

                    // Database-generated IDs are required for assignment and student mappings.
                    await _context.SaveChangesAsync();
                    foreach (var copiedClass in copiedClasses)
                    {
                        classIds[copiedClass.SourceId] = copiedClass.Target.ClassID;
                    }
                    foreach (var stage in stages)
                    {
                        var source = sourceYears[stage];
                        var target = targetYears[stage];

                        if (request.CopyTeacherAssignments)
                        {
                            var assignments = await _context.TeacherAssignments.AsNoTracking()
                                .Where(item => item.AcademicYearID == source.AcademicYearID && item.IsActive)
                                .ToListAsync();
                            foreach (var assignment in assignments)
                            {
                                if (!assignment.TeacherID.HasValue || !assignment.ClassID.HasValue || !assignment.SubjectID.HasValue ||
                                    !classIds.TryGetValue(assignment.ClassID.Value, out var newClassId) ||
                                    !subjectIds.TryGetValue(assignment.SubjectID.Value, out var newSubjectId))
                                {
                                    continue;
                                }

                                _context.TeacherAssignments.Add(new TeacherAssignment
                                {
                                    TeacherID = assignment.TeacherID,
                                    ClassID = newClassId,
                                    SubjectID = newSubjectId,
                                    AcademicYearID = target.AcademicYearID,
                                    AssignedAt = DateTime.UtcNow,
                                    IsActive = true
                                });
                                result.TeacherAssignmentsCopied++;
                            }
                        }

                        if (request.CarryStudents)
                        {
                            var students = await _context.Students
                                .Where(item => item.CurrentAcademicYearID == source.AcademicYearID)
                                .ToListAsync();
                            foreach (var student in students)
                            {
                                var sourceClassId = student.ClassID;
                                student.CurrentAcademicYearID = target.AcademicYearID;
                                student.ClassID = sourceClassId.HasValue && classIds.TryGetValue(sourceClassId.Value, out var newClassId)
                                    ? newClassId
                                    : null;
                                _context.StudentPromotions.Add(new StudentPromotion
                                {
                                    StudentID = student.StudentID,
                                    FromAcademicYearID = source.AcademicYearID,
                                    ToAcademicYearID = target.AcademicYearID,
                                    RequestDate = DateTime.UtcNow,
                                    IsApproved = true,
                                    RequestedBy = requestedBy,
                                    ApprovedBy = requestedBy,
                                    ApprovalDate = DateTime.UtcNow
                                });
                                result.StudentsCarried++;
                            }
                        }
                    }
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return CreatedAtAction(nameof(GetAcademicYears), result);
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "The academic-year setup could not be completed. No changes were saved." });
            }
        }

        private async Task<YearMappingsDto> BuildMappingsAsync()
        {
            var active = await _context.AcademicYears
                .AsNoTracking()
                .Where(item => item.IsActive)
                .ToListAsync();
            return new YearMappingsDto
            {
                Junior = active.FirstOrDefault(item => item.Stage == EducationStage.Junior)?.YearName ?? string.Empty,
                Wheeler = active.FirstOrDefault(item => item.Stage == EducationStage.Wheeler)?.YearName ?? string.Empty,
                Senior = active.FirstOrDefault(item => item.Stage == EducationStage.Senior)?.YearName ?? string.Empty
            };
        }

        private static bool IsConsecutiveAcademicYear(string value)
        {
            var parts = value.Split('-');
            return parts.Length == 2 &&
                int.TryParse(parts[0], out var startYear) &&
                int.TryParse(parts[1], out var endYear) &&
                endYear == startYear + 1;
        }
    }
}
