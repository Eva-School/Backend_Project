using GradeManagementSystem.Core.DTOs.Class;
using GradeManagementSystem.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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
        public async Task<IActionResult> GetClasses([FromQuery] string yearId, [FromQuery] string? stage)
        {
            if (string.IsNullOrWhiteSpace(yearId))
            {
                return BadRequest(new { message = "YearId parameter is required" });
            }

            var classes = await _classService.GetClassesByYearIdAsync(yearId, stage);

            // An academic year without classes is a normal setup state. Return
            // an empty collection so Student Affairs can create its first class
            // without treating the request as an error.
            return Ok(classes ?? Enumerable.Empty<ClassResponseDTO>());
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
            catch (DbUpdateException)
            {
                return Conflict(new { message = "The class could not be saved. Check that its name is unique for the selected year and department." });
            }
        }
    }
}
