using GradeManagementSystem.Core.DTOs.Teacher;
using GradeManagementSystem.Core.Interfaces;
using GradeManagementSystem.Repository.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace GradeManagementSystem.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class TeachersController : ControllerBase
    {
        private readonly GradeDbContext _context;
        private readonly IAuthService _authService;

        public TeachersController(GradeDbContext context, IAuthService authService)
        {
            _context = context;
            _authService = authService;
        }

        // GET: api/teachers
        [HttpGet]
        public async Task<IActionResult> GetAllTeachers()
        {
            var teachers = await _context.Teachers
                .Join(_context.Users, 
                      t => t.UserID, 
                      u => u.UserId, 
                      (t, u) => new TeacherResponse
                      {
                          Id = t.TeacherID.ToString(),
                          FullName = u.FullName
                      })
                .ToListAsync();

            return Ok(teachers);
        }

        // POST: api/teachers
        [HttpPost]
        public async Task<IActionResult> CreateTeacher([FromBody] TeacherRegisterRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { message = "Validation error message" });
            }

            var result = await _authService.RegisterTeacherAsync(request);
            
            // Extracting properties from anonymous object
            var successProp = result.GetType().GetProperty("success");
            bool success = successProp != null && (bool)successProp.GetValue(result);

            if (!success)
            {
                var messageProp = result.GetType().GetProperty("message");
                string message = messageProp != null ? (string)messageProp.GetValue(result) : "Registration failed";
                return BadRequest(new { message = message });
            }

            var dataProp = result.GetType().GetProperty("data");
            var data = dataProp != null ? dataProp.GetValue(result) : null;

            return Ok(data);
        }
    }
}
