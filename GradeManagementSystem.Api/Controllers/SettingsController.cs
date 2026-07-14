using GradeManagementSystem.Core.DTOs.Settings;
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
    }
}
