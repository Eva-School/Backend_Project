using GradeManagementSystem.Core.DTOs.Subject;
using GradeManagementSystem.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GradeManagementSystem.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
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
            // Note: year parameter is ignored as per specification
            var subjects = await _subjectService.GetSubjectsForActiveYearAsync();

            if (subjects == null)
            {
                return NotFound(new { message = "No active academic year found" });
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
