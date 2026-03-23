using GradeManagementSystem.Core.DTOs.TeacherAssignment;
using GradeManagementSystem.Core.Interfaces;
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
        public async Task<IActionResult> AssignTeacher([FromBody] TeacherAssignmentRequestDTO request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { message = "All fields are required or invalid" });
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
    }
}
