using GradeManagementSystem.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace GradeManagementSystem.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
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
    }
}
