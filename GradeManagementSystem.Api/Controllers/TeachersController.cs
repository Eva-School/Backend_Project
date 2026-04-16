using GradeManagementSystem.Core.DTOs.Teacher;
using GradeManagementSystem.Core.Interfaces;
using GradeManagementSystem.Core.Interfaces.Services;
using System.Security.Claims;
using GradeManagementSystem.Repository.Data;
using GradeManagementSystem.Services.Services;
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
        private readonly ITeacherGradeService teacherGradeService;

        public TeachersController(GradeDbContext context, IAuthService authService,ITeacherGradeService teacherGradeService)
        {
            _context = context;
            _authService = authService;
            this.teacherGradeService = teacherGradeService;
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

        [HttpGet("students/{classId}")]
        public async Task<IActionResult> GetStudentsByClass(int classId)
        {
            if (classId <= 0)
                return BadRequest(new { success = false, message = "Invalid classId" });

            var result = await teacherGradeService.GetStudentsByClassAsync(classId);

            if (result == null)
                return NotFound(new { success = false, message = "Class not found" });

            return Ok(result);
        }

        // Post api/teacher/Grede
        [HttpPost("SubmitGrade")]
        
        public async Task<IActionResult> SubmitGrade([FromBody] TeacherSubmitGradeDto teacherSubmitGradeDto)
        {

            try
            {
                var teacherIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

               
                if (teacherIdClaim == null)
                    return Unauthorized(new { ok = false, message = "Teacher not authenticated" });

                int teacherId = int.Parse(teacherIdClaim);

                await teacherGradeService.SubmitGradeAsync(teacherSubmitGradeDto);//, teacherId);
                return Ok(new
                {
                    success = true,
                    message = "Grade saved successfully",
                    data = teacherSubmitGradeDto
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }

            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = ex.Message,
                    details = ex.InnerException?.Message
                });
            }

        }




    }
}
