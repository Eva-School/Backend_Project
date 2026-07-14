using GradeManagementSystem.Core.DTOs.Vice;
using GradeManagementSystem.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace GradeManagementSystem.Api.Controllers
{
    [ApiController]
    [Route("api/vice/grades/quarter")]
    [Authorize(Roles = "Student Affairs,StudentAffairs,Admin")]
    public class ViceQuarterGradesController : ControllerBase
    {
        private readonly IViceQuarterGradesService _viceQuarterGradesService;

        public ViceQuarterGradesController(IViceQuarterGradesService viceQuarterGradesService)
        {
            _viceQuarterGradesService = viceQuarterGradesService;
        }

        // 16.1 PUT /api/vice/grades/quarter/subjects/{subjectId}/max-grades
        [HttpPut("subjects/{subjectId}/max-grades")]
        public async Task<IActionResult> SetMaxQuarterGrades([FromRoute] int subjectId, [FromBody] ViceSetQuarterMaxGradesRequestDTO request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { message = "Invalid request body" });
            }

            var updated = await _viceQuarterGradesService.SetSubjectQuarterMaxGradesAsync(subjectId, request);
            if (updated == null)
            {
                return BadRequest(new { message = "Unable to update max quarter grades" });
            }

            return Ok(updated);
        }

        // 17 GET /api/vice/grades/quarter/students
        [HttpGet("students")]
        public async Task<IActionResult> GetQuarterStudents(
            [FromQuery] string level,
            [FromQuery] int subjectId,
            [FromQuery] string department,
            [FromQuery] int? classId)
        {
            if (string.IsNullOrWhiteSpace(level) || string.IsNullOrWhiteSpace(department))
            {
                return BadRequest(new { message = "level and department are required" });
            }

            var sheet = await _viceQuarterGradesService.GetQuarterStudentsSheetAsync(level, subjectId, department, classId);
            if (sheet == null)
            {
                return NotFound(new { message = "Quarter students sheet not found" });
            }

            return Ok(sheet);
        }

        // 18 PUT /api/vice/grades/quarter/students
        [HttpPut("students")]
        public async Task<IActionResult> UpsertQuarterStudents([FromBody] ViceUpsertQuarterGradesRequestDTO request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { message = "Invalid request body" });
            }

            var updatedCount = await _viceQuarterGradesService.UpsertQuarterGradesBulkAsync(request);
            return Ok(new { message = "Quarter grades saved successfully", updatedCount });
        }
    }
}
