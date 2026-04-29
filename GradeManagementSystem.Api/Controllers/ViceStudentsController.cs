using GradeManagementSystem.Core.DTOs.Vice;
using GradeManagementSystem.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace GradeManagementSystem.Api.Controllers
{
    [ApiController]
    [Route("api/vice/students")]
    [Authorize(Roles = "Student Affairs,StudentAffairs")]
    public class ViceStudentsController : ControllerBase
    {
        private readonly IViceStudentService _viceStudentService;

        public ViceStudentsController(IViceStudentService viceStudentService)
        {
            _viceStudentService = viceStudentService;
        }

        [HttpGet]
        public async Task<IActionResult> GetStudents([FromQuery] string year, [FromQuery] string department, [FromQuery] int? classId)
        {
            if (string.IsNullOrWhiteSpace(year) || string.IsNullOrWhiteSpace(department))
            {
                return BadRequest(new { message = "year and department are required" });
            }

            var students = await _viceStudentService.GetStudentsAsync(year, department, classId);
            return Ok(students);
        }

        [HttpPost]
        public async Task<IActionResult> CreateStudent([FromBody] ViceCreateStudentRequestDTO request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { message = "Invalid request body" });
            }

            var created = await _viceStudentService.CreateStudentAsync(request);
            if (created == null)
            {
                return BadRequest(new { message = "Unable to create student" });
            }

            return Ok(created);
        }

        [HttpPut("{studentId}")]
        public async Task<IActionResult> UpdateStudent([FromRoute] string studentId, [FromBody] ViceCreateStudentRequestDTO request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { message = "Invalid request body" });
            }

            var updated = await _viceStudentService.UpdateStudentAsync(studentId, request);
            if (updated == null)
            {
                return NotFound(new { message = "Student not found" });
            }

            return Ok(updated);
        }

        [HttpDelete("{studentId}")]
        public async Task<IActionResult> DeleteStudent([FromRoute] string studentId)
        {
            var deleted = await _viceStudentService.DeleteStudentAsync(studentId);
            if (!deleted)
            {
                return NotFound(new { message = "Student not found" });
            }

            return Ok(new { message = "Student deleted successfully" });
        }
    }
}

