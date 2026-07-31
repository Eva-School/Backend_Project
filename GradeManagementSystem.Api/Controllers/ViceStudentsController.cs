using GradeManagementSystem.Core.DTOs.Vice;
using GradeManagementSystem.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace GradeManagementSystem.Api.Controllers
{
    [ApiController]
    [Route("api/vice/students")]
    [Authorize(Roles = "Student Affairs,StudentAffairs,Admin")]
    public class ViceStudentsController : ControllerBase
    {
        private readonly IViceStudentService _viceStudentService;

        public ViceStudentsController(IViceStudentService viceStudentService)
        {
            _viceStudentService = viceStudentService;
        }

        [HttpGet]
        public async Task<IActionResult> GetStudents(
            [FromQuery] string year,
            [FromQuery] string department,
            [FromQuery] int? classId,
            [FromQuery] bool unassigned = false,
            [FromQuery] string? academicYearName = null)
        {
            if (string.IsNullOrWhiteSpace(year) || string.IsNullOrWhiteSpace(department))
            {
                return BadRequest(new { message = "year and department are required" });
            }

            var students = await _viceStudentService.GetStudentsAsync(year, department, classId, unassigned, academicYearName);
            return Ok(students);
        }

        [HttpPost]
        public async Task<IActionResult> CreateStudent([FromBody] ViceCreateStudentRequestDTO request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { message = "Invalid request body" });
            }

            try
            {
                var created = await _viceStudentService.CreateStudentAsync(request);
                if (created == null)
                {
                    return BadRequest(new { message = "The selected academic year or department was not found." });
                }

                return Ok(created);
            }
            catch (ArgumentException exception)
            {
                return BadRequest(new { message = exception.Message });
            }
            catch (InvalidOperationException exception)
            {
                return Conflict(new { message = exception.Message });
            }
            catch (DbUpdateException)
            {
                return Conflict(new { message = "The student could not be saved. Check that the student code and email address are unique." });
            }
        }

        [HttpPut("{studentId}")]
        public async Task<IActionResult> UpdateStudent([FromRoute] string studentId, [FromBody] ViceCreateStudentRequestDTO request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { message = "Invalid request body" });
            }

            try
            {
                var updated = await _viceStudentService.UpdateStudentAsync(studentId, request);
                if (updated == null)
                {
                    return NotFound(new { message = "Student not found" });
                }

                return Ok(updated);
            }
            catch (ArgumentException exception)
            {
                return BadRequest(new { message = exception.Message });
            }
            catch (InvalidOperationException exception)
            {
                return Conflict(new { message = exception.Message });
            }
            catch (DbUpdateException)
            {
                return Conflict(new { message = "The student could not be updated. Check that the student code and email address are unique." });
            }
        }

        [HttpPatch("{studentId}/class")]
        public async Task<IActionResult> AssignClass([FromRoute] string studentId, [FromBody] ViceStudentClassAssignmentRequestDTO request)
        {
            var updated = await _viceStudentService.AssignStudentToClassAsync(studentId, request.ClassId);
            if (updated == null)
            {
                return BadRequest(new { message = "The student or selected class was not found, or the class belongs to another academic year." });
            }

            return Ok(updated);
        }

        [HttpPost("promote")]
        public async Task<IActionResult> PromoteStudents([FromBody] VicePromoteStudentsRequestDTO request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { message = "Student IDs, source level, target level, and department are required." });
            }

            var userId = int.TryParse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value, out var parsedUserId)
                ? parsedUserId
                : (int?)null;
            try
            {
                var promoted = await _viceStudentService.PromoteStudentsAsync(request, userId);
                return Ok(new { promoted });
            }
            catch (ArgumentException exception)
            {
                return BadRequest(new { message = exception.Message });
            }
            catch (InvalidOperationException exception)
            {
                return Conflict(new { message = exception.Message });
            }
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

        [HttpPost("import")]
        public async Task<IActionResult> ImportStudents(
            Microsoft.AspNetCore.Http.IFormFile file,
            [FromQuery] string year = "junior",
            [FromQuery] string department = "OM",
            [FromQuery] string? academicYearName = null,
            CancellationToken cancellationToken = default)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(new { message = "Please select an Excel or CSV file to upload." });
            }

            try
            {
                using var stream = file.OpenReadStream();
                var result = await _viceStudentService.ImportStudentsFromExcelAsync(stream, file.FileName, year, department, academicYearName, cancellationToken);
                return Ok(result);
            }
            catch (System.Exception ex)
            {
                return BadRequest(new { message = $"Failed to process file: {ex.Message}" });
            }
        }
    }
}
