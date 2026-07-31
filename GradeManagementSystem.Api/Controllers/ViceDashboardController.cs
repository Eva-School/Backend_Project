using GradeManagementSystem.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace GradeManagementSystem.Api.Controllers
{
    [ApiController]
    [Route("api/vice")]
    public class ViceDashboardController : ControllerBase
    {
        private readonly IViceDashboardService _viceDashboardService;

        public ViceDashboardController(IViceDashboardService viceDashboardService)
        {
            _viceDashboardService = viceDashboardService;
        }

        // 11 GET /api/vice/dashboard/cards
        [HttpGet("dashboard/cards")]
        [Authorize(Roles = "Student Affairs,StudentAffairs,Admin")]
        public async Task<IActionResult> GetCards()
        {
            var cards = await _viceDashboardService.GetCardsAsync();
            return Ok(cards);
        }

        // 23 GET /api/vice/grades/dashboard
        [HttpGet("grades/dashboard")]
        [Authorize(Roles = "Student Affairs,StudentAffairs,Admin")]
        public async Task<IActionResult> GetGradesDashboard([FromQuery] string? academicYear)
        {
            try
            {
                var dashboard = await _viceDashboardService.GetGradesDashboardAsync(academicYear);
                return Ok(dashboard);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to load grades dashboard.", detail = ex.Message });
            }
        }
    }
}
