using GradeManagementSystem.Core.DTOs.Class;
using GradeManagementSystem.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

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

        [HttpGet("cohorts-summary")]
        public async Task<IActionResult> GetCohortsSummary()
        {
            var summaries = await _classService.GetCohortsSummaryAsync();
            return Ok(summaries);
        }

        [HttpGet]
        public async Task<IActionResult> GetClasses([FromQuery] string? yearId, [FromQuery] string? stage)
        {
            if (string.IsNullOrWhiteSpace(yearId) && string.IsNullOrWhiteSpace(stage))
            {
                return BadRequest(new { message = "Either yearId or stage parameter is required." });
            }

            var classes = await _classService.GetClassesByYearIdAsync(yearId, stage);

            // An academic year without classes is a normal setup state. Return
            // an empty collection so Student Affairs can create its first class
            // without treating the request as an error.
            return Ok(classes ?? Enumerable.Empty<ClassResponseDTO>());
        }

        [HttpGet("{classId:int}")]
        [HttpGet("{classId:int}/details")]
        public async Task<IActionResult> GetClassDetails([FromRoute] int classId)
        {
            var details = await _classService.GetClassDetailsAsync(classId);
            if (details == null)
            {
                return NotFound(new { message = $"Class with ID {classId} was not found." });
            }

            return Ok(details);
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

                return CreatedAtAction(nameof(GetClassDetails), new { classId = created.ClassId }, created);
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

        [HttpPut("{classId:int}")]
        public async Task<IActionResult> UpdateClass([FromRoute] int classId, [FromBody] UpdateClassRequestDTO request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var updated = await _classService.UpdateClassAsync(classId, request);
                if (updated == null)
                {
                    return NotFound(new { message = $"Class with ID {classId} was not found." });
                }

                return Ok(updated);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
            catch (DbUpdateException)
            {
                return Conflict(new { message = "Could not update class. Ensure class name is unique for the department and year." });
            }
        }

        [HttpDelete("{classId:int}")]
        public async Task<IActionResult> DeleteClass([FromRoute] int classId)
        {
            var (success, message) = await _classService.DeleteClassAsync(classId);
            if (!success)
            {
                return NotFound(new { message });
            }

            return Ok(new { message });
        }
    }
}
