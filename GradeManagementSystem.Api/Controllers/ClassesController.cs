using GradeManagementSystem.Core.DTOs.Class;
using GradeManagementSystem.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace GradeManagementSystem.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin,Student Affairs,StudentAffairs")]
    public class ClassesController : ControllerBase
    {
        private readonly IClassService _classService;

        public ClassesController(IClassService classService)
        {
            _classService = classService;
        }

        [HttpGet]
        public async Task<IActionResult> GetClasses([FromQuery] string yearId)
        {
            if (string.IsNullOrWhiteSpace(yearId))
            {
                return BadRequest(new { message = "YearId parameter is required" });
            }

            var classes = await _classService.GetClassesByYearIdAsync(yearId);

            if (classes == null || !classes.Any())
            {
                return NotFound(new { message = "No classes found for the specified academic year or yearId is invalid." });
            }

            return Ok(classes);
        }

        [HttpPost]
        public async Task<IActionResult> CreateClass([FromBody] CreateClassRequestDTO request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { message = "yearId, department, and className are required." });
            }

            try
            {
                var created = await _classService.CreateClassAsync(request);
                if (created == null)
                {
                    return BadRequest(new { message = "The selected academic year or department was not found." });
                }

                return CreatedAtAction(nameof(GetClasses), new { yearId = request.YearId }, created);
            }
            catch (InvalidOperationException exception)
            {
                return Conflict(new { message = exception.Message });
            }
        }
    }
}
