using GradeManagementSystem.Core.DTOs.Vice;
using GradeManagementSystem.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace GradeManagementSystem.Api.Controllers
{
    [ApiController]
    [Route("api/admin/grades/final")]
    [Authorize(Roles = "Admin")]
    public class AdminFinalGradesController : ControllerBase
    {
        private readonly IAdminFinalGradesService _adminFinalGradesService;

        public AdminFinalGradesController(IAdminFinalGradesService adminFinalGradesService)
        {
            _adminFinalGradesService = adminFinalGradesService;
        }

        // 19.1 POST /api/admin/grades/final/approve
        [HttpPost("approve")]
        public async Task<IActionResult> ApproveFinalGrades([FromBody] ViceFinalApproveRequestDTO request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { message = "Invalid request body" });
            }

            var message = await _adminFinalGradesService.ApproveAndLockFinalGradesAsync(request);
            if (message == null)
            {
                return BadRequest(new { message = "Unable to approve final grades" });
            }

            return Ok(new { message });
        }
    }
}

