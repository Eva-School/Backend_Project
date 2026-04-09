using AutoMapper;
using GradeManagementSystem.Core.DTOs.Teacher;
using GradeManagementSystem.Core.Entities.Domain;
using GradeManagementSystem.Core.Interfaces;
using GradeManagementSystem.Core.Specifications;
using GradeManagementSystem.Repository.Data;
using GradeManagementSystem.Repository.Repositories;
using GradeManagementSystem.Repository.Specifications;
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
        private readonly IMapper _mapper;
        private readonly IGenericRepository<TeacherAssignment> TeacherAssignmentGenericRepository;

        public TeachersController(GradeDbContext context, IAuthService authService, IMapper mapper, IGenericRepository<TeacherAssignment> teacherAssignmentGenericRepository)
        {
            _context = context;
            _authService = authService;
            _mapper = mapper;
            TeacherAssignmentGenericRepository = teacherAssignmentGenericRepository;
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


        //GET : teacher/profile
       [HttpGet("{id}")]
        public async Task<IActionResult> GetTeacherProfile(int id)
        {
            var response = await _context.Teachers
              .Include(t => t.User)
              .Include(t => t.TeacherAssignments)
                  .ThenInclude(ta => ta.AcademicYear)
              .Include(t => t.TeacherAssignments)
                  .ThenInclude(ta => ta.Subject)
              .FirstOrDefaultAsync(t => t.TeacherID == id);

            if (response == null)
            {
                return NotFound("Teacher Id not found.");
            }

            var mapped = _mapper.Map<TeacherProfileReturnDTO>(response);

            return Ok(mapped);
        }


        //Get : teacher/Subject
        [HttpGet("/teacher/Subject")]
        public async Task<IActionResult> GetTeachersSubjects()
        {
            var spec = new TeacherAssignmentWithRelatedData();
            var response = await TeacherAssignmentGenericRepository.GetAll(spec);
            var mapped = _mapper.Map<IEnumerable<TeacherAssignment>, IEnumerable<TeacherSubjectReturnDTO>>(response);

            return Ok(mapped);
        }


        [HttpGet("/teacher/classes")]
        public async Task<IActionResult> GetTeachersWithTheirClasses(
            [FromQuery] string year,
            [FromQuery] string subject = null)
        {
            var spec = new TeacherAssignmentWithRelatedData();
            var response = await TeacherAssignmentGenericRepository.GetAll(spec);

            if (!string.IsNullOrEmpty(year))
                response = response.Where(t => t.AcademicYear.YearName.Equals(year, StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrEmpty(subject))
                response = response.Where(t => t.Subject.SubjectName.Equals(subject, StringComparison.OrdinalIgnoreCase));

            var mapped = _mapper.Map<IEnumerable<TeacherAssignment>, IEnumerable<TeacherClassesResponseDTO>>(response);

            return Ok(mapped);

        }
    }
}
