using GradeManagementSystem.Core.DTOs.Subject;
using GradeManagementSystem.Core.Entities.Enums;
using GradeManagementSystem.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GradeManagementSystem.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin,Student Affairs,StudentAffairs")]
    public class SubjectsController : ControllerBase
    {
        private readonly ISubjectService _subjectService;

        public SubjectsController(ISubjectService subjectService)
        {
            _subjectService = subjectService;
        }

        [HttpGet]
        public async Task<IActionResult> GetSubjects([FromQuery] string? year)
        {
            var subjects = (await _subjectService.GetSubjectsForActiveYearAsync()).ToList();

            if (subjects == null)
            {
                return NotFound(new { message = "No active academic year found" });
            }

            if (!string.IsNullOrWhiteSpace(year))
            {
                if (!Enum.TryParse<EducationStage>(year, true, out var stage))
                {
                    return BadRequest(new { message = "Invalid year value. Expected: junior|wheeler|senior." });
                }

                subjects = subjects
                    .Where(s => string.Equals(s.Stage, stage.ToString(), System.StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            return Ok(subjects);
        }

        [HttpPost]
        public async Task<IActionResult> CreateSubject([FromBody] CreateSubjectRequestDTO request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { message = "Subject name is required or stage is invalid" });
            }

            try
            {
                var result = await _subjectService.CreateSubjectAsync(request);
                
                if (result == null)
                {
                    return BadRequest(new { message = "Subject name is required or stage is invalid" });
                }

                return CreatedAtAction(nameof(GetSubjects), new { id = result.Id }, result);
            }
            catch (System.InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
