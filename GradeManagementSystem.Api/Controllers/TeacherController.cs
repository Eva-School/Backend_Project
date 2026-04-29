using GradeManagementSystem.Core.DTOs.Class;
using GradeManagementSystem.Core.DTOs.Teacher;
using GradeManagementSystem.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace GradeManagementSystem.Api.Controllers
{
    [Route("api/teacher")]
    [ApiController]
    [Authorize(Roles = "Teacher")]
    public class TeacherController : ControllerBase
    {
        private readonly ITeacherDashboardService _teacherDashboardService;

        public TeacherController(ITeacherDashboardService teacherDashboardService)
        {
            _teacherDashboardService = teacherDashboardService;
        }

        [HttpGet("profile")]
        public async Task<IActionResult> GetProfile()
        {
            if (!TryGetUserId(out var userId))
            {
                return Unauthorized(new { message = "Unauthenticated" });
            }

            var profile = await _teacherDashboardService.GetProfileAsync(userId);
            if (profile == null)
            {
                return NotFound(new { message = "Teacher profile not found." });
            }

            return Ok(profile);
        }

        [HttpGet("subjects")]
        public async Task<IActionResult> GetSubjects()
        {
            if (!TryGetUserId(out var userId))
            {
                return Unauthorized(new { message = "Unauthenticated" });
            }

            var groups = await _teacherDashboardService.GetSubjectsAsync(userId);
            return Ok(groups);
        }

        [HttpGet("classes")]
        public async Task<IActionResult> GetClasses([FromQuery] string year, [FromQuery] string subject)
        {
            if (string.IsNullOrWhiteSpace(year))
            {
                return BadRequest(new { message = "year parameter is required." });
            }

            if (string.IsNullOrWhiteSpace(subject))
            {
                return BadRequest(new { message = "subject parameter is required." });
            }

            if (!TryGetUserId(out var userId))
            {
                return Unauthorized(new { message = "Unauthenticated" });
            }

            try
            {
                var classes = await _teacherDashboardService.GetClassesAsync(userId, year, subject);
                if (classes == null)
                {
                    return NotFound(new { message = "No classes found for the provided filters." });
                }

                return Ok(classes);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("students")]
        public async Task<IActionResult> GetStudents([FromQuery] int classId)
        {
            if (classId <= 0)
            {
                return BadRequest(new { message = "classId must be provided and > 0." });
            }

            if (!TryGetUserId(out var userId))
            {
                return Unauthorized(new { message = "Unauthenticated" });
            }

            try
            {
                var students = await _teacherDashboardService.GetStudentsAsync(userId, classId);
                if (students == null)
                {
                    return NotFound(new { message = "Teacher profile not found." });
                }

                return Ok(students);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("grades")]
        public async Task<IActionResult> UpsertGrade([FromBody] TeacherGradeUpdateRequestDTO request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { message = "Invalid request body." });
            }

            if (request.ClassId <= 0 || request.StudentId <= 0)
            {
                return BadRequest(new { message = "classId and studentId must be > 0." });
            }

            if (request.Grade < 0)
            {
                return BadRequest(new { message = "grade must be >= 0." });
            }

            if (!TryGetUserId(out var userId))
            {
                return Unauthorized(new { message = "Unauthenticated" });
            }

            try
            {
                var result = await _teacherDashboardService.UpsertGradeAsync(userId, request);
                if (result == null)
                {
                    return NotFound(new { message = "Grade update failed: teacher/assignment not found." });
                }

                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        private bool TryGetUserId(out int userId)
        {
            userId = 0;
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            return userIdClaim != null && int.TryParse(userIdClaim.Value, out userId);
        }
    }
}

