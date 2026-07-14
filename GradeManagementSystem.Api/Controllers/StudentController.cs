using GradeManagementSystem.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace GradeManagementSystem.Api.Controllers
{
    [Route("api/student")]
    [ApiController]
    [Authorize(Roles = "Student")]
    public class StudentController : ControllerBase
    {
        private readonly IStudentDashboardService _studentDashboardService;

        public StudentController(IStudentDashboardService studentDashboardService)
        {
            _studentDashboardService = studentDashboardService;
        }

        [HttpGet("cards")]
        public async Task<IActionResult> GetCards()
        {
            var cards = await _studentDashboardService.GetCardsAsync();
            return Ok(cards);
        }

        [HttpGet("profile")]
        public async Task<IActionResult> GetProfile()
        {
            if (!TryGetUserId(out var userId))
            {
                return Unauthorized(new { message = "Unauthenticated" });
            }

            var profile = await _studentDashboardService.GetProfileAsync(userId);
            if (profile == null)
            {
                return NotFound(new { message = "Student profile not found." });
            }

            return Ok(profile);
        }

        [HttpGet("years")]
        public async Task<IActionResult> GetYears()
        {
            var years = await _studentDashboardService.GetYearsAsync();
            return Ok(years);
        }

        [HttpGet("grades/quarter")]
        public async Task<IActionResult> GetQuarterGrades([FromQuery] string year)
        {
            if (string.IsNullOrWhiteSpace(year))
            {
                return BadRequest(new { message = "Year parameter is required." });
            }

            if (!TryGetUserId(out var userId))
            {
                return Unauthorized(new { message = "Unauthenticated" });
            }

            var response = await _studentDashboardService.GetQuarterGradesAsync(userId, year);
            if (response == null)
            {
                return NotFound(new { message = "No quarter grades found for the provided year." });
            }

            return Ok(response);
        }

        [HttpGet("grades/final")]
        public async Task<IActionResult> GetFinalGrades([FromQuery] string year)
        {
            if (string.IsNullOrWhiteSpace(year))
            {
                return BadRequest(new { message = "Year parameter is required." });
            }

            if (!TryGetUserId(out var userId))
            {
                return Unauthorized(new { message = "Unauthenticated" });
            }

            var response = await _studentDashboardService.GetFinalGradesAsync(userId, year);
            if (response == null)
            {
                return NotFound(new { message = "No final grades found for the provided year." });
            }

            return Ok(response);
        }

        [HttpGet("grades/jadarat")]
        public async Task<IActionResult> GetJadaratGrades([FromQuery] string year)
        {
            if (string.IsNullOrWhiteSpace(year))
            {
                return BadRequest(new { message = "Year parameter is required." });
            }

            if (!TryGetUserId(out var userId))
            {
                return Unauthorized(new { message = "Unauthenticated" });
            }

            var response = await _studentDashboardService.GetJadaratGradesAsync(userId, year);
            if (response == null)
            {
                return NotFound(new { message = "No competencies found for the provided year." });
            }

            return Ok(response);
        }

        [HttpGet("grades/progress")]
        public async Task<IActionResult> GetProgress([FromQuery] string year)
        {
            if (string.IsNullOrWhiteSpace(year))
            {
                return BadRequest(new { message = "Year parameter is required." });
            }

            if (!TryGetUserId(out var userId))
            {
                return Unauthorized(new { message = "Unauthenticated" });
            }

            var response = await _studentDashboardService.GetProgressAsync(userId, year);
            return Ok(response);
        }

        [HttpGet("report")]
        public async Task<IActionResult> GetReport([FromQuery] string year)
        {
            if (string.IsNullOrWhiteSpace(year))
            {
                return BadRequest(new { message = "Year parameter is required." });
            }

            if (!TryGetUserId(out var userId))
            {
                return Unauthorized(new { message = "Unauthenticated" });
            }

            var report = await _studentDashboardService.GetReportAsync(userId, year);
            if (report == null)
            {
                return NotFound(new { message = "No report data found for the provided year." });
            }

            return Ok(report);
        }

        private bool TryGetUserId(out int userId)
        {
            userId = 0;
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            return userIdClaim != null && int.TryParse(userIdClaim.Value, out userId);
        }
    }
}
