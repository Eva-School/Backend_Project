using GradeManagementSystem.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Threading.Tasks;

namespace GradeManagementSystem.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class StudentController : ControllerBase
    {
        private readonly IStudentService _studentService;

        public StudentController(IStudentService studentService)
        {
            _studentService = studentService;
        }

        // GET: api/student/profile
        [HttpGet("profile")]
        public async Task<IActionResult> GetProfile()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            {
                return Unauthorized(new { message = "Unauthenticated" });
            }

            var profile = await _studentService.GetProfileAsync(userId);
            if (profile == null)
            {
                return NotFound(new { message = "Student profile not found" });
            }

            return Ok(profile);
        }

        // GET: api/student/years
        [HttpGet("years")]
        public async Task<IActionResult> GetYears()
        {
            var years = await _studentService.GetYearsAsync();
            return Ok(years);
        }

        // GET: api/student/grades/quarter?year={year}
        [HttpGet("grades/quarter")]
        public async Task<IActionResult> GetQuarterGrades([FromQuery] string year)
        {
            if (string.IsNullOrWhiteSpace(year))
            {
                return BadRequest(new { message = "year query parameter is required (junior | wheeler | senior)" });
            }

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            {
                return Unauthorized(new { message = "Unauthenticated" });
            }

            var result = await _studentService.GetQuarterGradesAsync(userId, year);
            if (result == null)
            {
                return BadRequest(new { message = "Invalid year. Must be one of: junior, wheeler, senior" });
            }

            return Ok(result);
        }

        // GET: api/student/grades/final?year={year}
        [HttpGet("grades/final")]
        public async Task<IActionResult> GetFinalGrades([FromQuery] string year)
        {
            if (string.IsNullOrWhiteSpace(year))
            {
                return BadRequest(new { message = "year query parameter is required (junior | wheeler | senior)" });
            }

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            {
                return Unauthorized(new { message = "Unauthenticated" });
            }

            var result = await _studentService.GetFinalGradesAsync(userId, year);
            if (result == null)
            {
                return BadRequest(new { message = "Invalid year. Must be one of: junior, wheeler, senior" });
            }

            return Ok(result);
        }

        // GET: api/student/grades/jadarat?year={year}
        [HttpGet("grades/jadarat")]
        public async Task<IActionResult> GetJadaratGrades([FromQuery] string year)
        {
            if (string.IsNullOrWhiteSpace(year))
            {
                return BadRequest(new { message = "year query parameter is required (junior | wheeler | senior)" });
            }

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            {
                return Unauthorized(new { message = "Unauthenticated" });
            }

            var result = await _studentService.GetJadaratGradesAsync(userId, year);
            if (result == null)
            {
                return BadRequest(new { message = "Invalid year. Must be one of: junior, wheeler, senior" });
            }

            return Ok(result);
        }
    }
}
