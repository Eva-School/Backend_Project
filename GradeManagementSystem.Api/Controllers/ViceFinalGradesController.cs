using GradeManagementSystem.Core.DTOs.Vice;
using GradeManagementSystem.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;

namespace GradeManagementSystem.Api.Controllers
{
    [ApiController]
    [Route("api/vice/grades/final")]
    [Authorize(Roles = "Student Affairs,StudentAffairs,Admin")]
    public class ViceFinalGradesController : ControllerBase
    {
        private readonly IViceFinalGradesService _viceFinalGradesService;

        public ViceFinalGradesController(IViceFinalGradesService viceFinalGradesService)
        {
            _viceFinalGradesService = viceFinalGradesService;
        }

        // 19 GET /api/vice/grades/final/students
        [HttpGet("students")]
        public async Task<IActionResult> GetFinalStudents(
            [FromQuery] string level,
            [FromQuery] int semester,
            [FromQuery] string department,
            [FromQuery] int? classId)
        {
            var res = await _viceFinalGradesService.GetFinalStudentsTableAsync(level, semester, department, classId);
            if (res == null)
            {
                return NotFound(new { message = "Final grades not found" });
            }
            return Ok(res);
        }

        // 20 PUT /api/vice/grades/final/students
        [HttpPut("students")]
        public async Task<IActionResult> UpsertFinalStudents([FromBody] ViceUpsertFinalGradesRequestDTO request)
        {
            if (!ModelState.IsValid)
            {
                return ValidationProblem(ModelState);
            }

            int updatedCount;
            try
            {
                updatedCount = await _viceFinalGradesService.UpsertFinalGradesBulkAsync(request);
            }
            catch (ArgumentException exception)
            {
                return BadRequest(new { message = exception.Message });
            }
            if (updatedCount == 0)
            {
                return BadRequest(new { message = "Final grades could not be updated (maybe already approved/locked)" });
            }

            return Ok(new { message = "Final grades saved successfully", updatedCount });
        }

        // 21 POST /api/vice/grades/final/submit
        [HttpPost("submit")]
        public async Task<IActionResult> SubmitFinalGrades([FromBody] ViceSubmitFinalGradesRequestDTO request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { message = "Invalid request body" });
            }

            var ok = await _viceFinalGradesService.SubmitFinalGradesAsync(request);
            if (!ok)
            {
                return BadRequest(new { message = "Final grades submission failed" });
            }

            return Ok(new { message = "Final grades submitted for approval" });
        }

        // 22 GET /api/vice/grades/final/history?studentId={id}&subjectId={id}
        [HttpGet("history")]
        public async Task<IActionResult> GetFinalHistory(
            [FromQuery] string studentId,
            [FromQuery] int subjectId)
        {
            if (string.IsNullOrWhiteSpace(studentId))
            {
                return BadRequest(new { message = "studentId is required" });
            }

            var history = await _viceFinalGradesService.GetFinalHistoryAsync(studentId, subjectId);
            return Ok(history);
        }
    }
}
