using GradeManagementSystem.Core.DTOs.TeacherAssignment;
using GradeManagementSystem.Core.Interfaces;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace GradeManagementSystem.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class TeacherAssignmentsController : ControllerBase
    {
        private readonly ITeacherAssignmentService _teacherAssignmentService;

        public TeacherAssignmentsController(ITeacherAssignmentService teacherAssignmentService)
        {
            _teacherAssignmentService = teacherAssignmentService;
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Student Affairs,StudentAffairs")]
        public async Task<IActionResult> AssignTeacher([FromBody] TeacherAssignmentRequestDTO request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { message = "All fields are required" });
            }

            var (success, message) = await _teacherAssignmentService.AssignTeacherToClassesAsync(request);

            if (!success)
            {
                // Specific error messages for 400 and 404 cases
                if (message.Contains("not found") || message.Contains("Invalid"))
                {
                    return NotFound(new { message = message });
                }
                return BadRequest(new { message = message });
            }

            return Ok(new { message = message });
        }

        [HttpGet("MyDashboard")]
        [Authorize(Roles = "Teacher")]
        public async Task<IActionResult> GetMyDashboard()
        {
            if (!TryGetUserId(out var userId))
            {
                return Unauthorized(new { message = "Unauthenticated" });
            }

            var dashboard = await _teacherAssignmentService.GetMyDashboardAsync(userId);
            return Ok(dashboard);
        }

        [HttpGet]
        [Authorize(Roles = "Admin,Student Affairs,StudentAffairs")]
        public async Task<IActionResult> GetAssignments([FromQuery] string? yearName, [FromQuery] string? stage)
        {
            try
            {
                return Ok(await _teacherAssignmentService.GetAssignmentsAsync(yearName, stage));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut]
        [Authorize(Roles = "Admin,Student Affairs,StudentAffairs")]
        public async Task<IActionResult> ReplaceAssignmentClasses([FromBody] TeacherAssignmentRequestDTO request)
        {
            if (!ModelState.IsValid) return BadRequest(new { message = "All fields are required." });
            var (success, message) = await _teacherAssignmentService.ReplaceTeacherAssignmentClassesAsync(request);
            return success ? Ok(new { message }) : BadRequest(new { message });
        }

        [HttpPatch("status")]
        [Authorize(Roles = "Admin,Student Affairs,StudentAffairs")]
        public async Task<IActionResult> SetAssignmentStatus([FromBody] TeacherAssignmentStatusRequestDto request)
        {
            var (success, message) = await _teacherAssignmentService.SetAssignmentStatusAsync(request);
            return success ? Ok(new { message }) : NotFound(new { message });
        }

        [HttpDelete]
        [Authorize(Roles = "Admin,Student Affairs,StudentAffairs")]
        public async Task<IActionResult> DeleteAssignment([FromBody] TeacherAssignmentStatusRequestDto request)
        {
            var (success, message) = await _teacherAssignmentService.DeleteAssignmentAsync(request);
            return success ? NoContent() : NotFound(new { message });
        }

        [HttpGet("MyClasses")]
        [Authorize(Roles = "Teacher")]
        public async Task<IActionResult> GetMyClasses([FromQuery] string yearId)
        {
            if (string.IsNullOrWhiteSpace(yearId))
            {
                return BadRequest(new { message = "yearId parameter is required" });
            }

            if (!TryGetUserId(out var userId))
            {
                return Unauthorized(new { message = "Unauthenticated" });
            }

            var classes = await _teacherAssignmentService.GetMyClassesAsync(userId, yearId);
            return Ok(classes);
        }

        private bool TryGetUserId(out int userId)
        {
            userId = 0;
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            return userIdClaim != null && int.TryParse(userIdClaim.Value, out userId);
        }
    }
}
