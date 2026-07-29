using GradeManagementSystem.Core.DTOs.Class;
using GradeManagementSystem.Core.DTOs.Teacher;
using GradeManagementSystem.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace GradeManagementSystem.Api.Controllers
{
    [Route("api/teacher")]
    [ApiController]
    [Authorize(Roles = "Teacher")]
    public class TeacherController : ControllerBase
    {
        private readonly ITeacherDashboardService _teacherDashboardService;

        public TeacherController(ITeacherDashboardService teacherDashboardService)
        {
            _teacherDashboardService = teacherDashboardService;
        }

        [HttpGet("profile")]
        public async Task<IActionResult> GetProfile()
        {
            if (!TryGetUserId(out var userId))
            {
                return Unauthorized(new { message = "Unauthenticated" });
            }

            var profile = await _teacherDashboardService.GetProfileAsync(userId);
            if (profile == null)
            {
                return NotFound(new { message = "Teacher profile not found." });
            }

            return Ok(profile);
        }

        [HttpGet("subjects")]
        public async Task<IActionResult> GetSubjects()
        {
            if (!TryGetUserId(out var userId))
            {
                return Unauthorized(new { message = "Unauthenticated" });
            }

            var groups = await _teacherDashboardService.GetSubjectsAsync(userId);
            return Ok(groups);
        }

        [HttpGet("classes")]
        public async Task<IActionResult> GetClasses([FromQuery] string year, [FromQuery] string subject)
        {
            if (string.IsNullOrWhiteSpace(year))
            {
                return BadRequest(new { message = "year parameter is required." });
            }

            if (string.IsNullOrWhiteSpace(subject))
            {
                return BadRequest(new { message = "subject parameter is required." });
            }

            if (!TryGetUserId(out var userId))
            {
                return Unauthorized(new { message = "Unauthenticated" });
            }

            try
            {
                var classes = await _teacherDashboardService.GetClassesAsync(userId, year, subject);
                if (classes == null)
                {
                    return NotFound(new { message = "No classes found for the provided filters." });
                }

                return Ok(classes);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("students")]
        public async Task<IActionResult> GetStudents([FromQuery] int classId, [FromQuery] int subjectId)
        {
            if (classId <= 0 || subjectId <= 0)
            {
                return BadRequest(new { message = "classId and subjectId must be provided and > 0." });
            }

            if (!TryGetUserId(out var userId))
            {
                return Unauthorized(new { message = "Unauthenticated" });
            }

            try
            {
                var students = await _teacherDashboardService.GetStudentsAsync(userId, classId, subjectId);
                if (students == null)
                {
                    return NotFound(new { message = "Teacher profile not found." });
                }

                return Ok(students);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("grades")]
        public async Task<IActionResult> UpsertGrade([FromBody] TeacherGradeUpdateRequestDTO request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { message = "Invalid request body." });
            }

            if (request.ClassId <= 0 || request.StudentId <= 0 || request.SubjectId <= 0)
            {
                return BadRequest(new { message = "classId, studentId and subjectId must be > 0." });
            }

            if (new[] { request.Q1, request.Q2, request.Q3, request.Q4 }.Any(score => score < 0))
            {
                return BadRequest(new { message = "Quarter grades must be >= 0." });
            }

            if (!request.Q1.HasValue && !request.Q2.HasValue && !request.Q3.HasValue && !request.Q4.HasValue)
            {
                return BadRequest(new { message = "At least one quarter grade must be provided." });
            }

            if (!TryGetUserId(out var userId))
            {
                return Unauthorized(new { message = "Unauthenticated" });
            }

            try
            {
                var result = await _teacherDashboardService.UpsertGradeAsync(userId, request);
                if (result == null)
                {
                    return NotFound(new { message = "Grade update failed: teacher/assignment not found." });
                }

                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
        }

        #region Quiz Endpoints

        [HttpGet("quizzes")]
        public async Task<IActionResult> GetQuizzes([FromQuery] int classId, [FromQuery] int subjectId)
        {
            if (classId <= 0 || subjectId <= 0)
            {
                return BadRequest(new { message = "classId and subjectId must be > 0." });
            }

            if (!TryGetUserId(out var userId))
            {
                return Unauthorized(new { message = "Unauthenticated" });
            }

            var quizzes = await _teacherDashboardService.GetQuizzesAsync(userId, classId, subjectId);
            return Ok(quizzes);
        }

        [HttpGet("quizzes/{quizId}")]
        public async Task<IActionResult> GetQuizById(int quizId)
        {
            if (quizId <= 0)
            {
                return BadRequest(new { message = "quizId must be > 0." });
            }

            if (!TryGetUserId(out var userId))
            {
                return Unauthorized(new { message = "Unauthenticated" });
            }

            var quizDetail = await _teacherDashboardService.GetQuizByIdAsync(userId, quizId);
            if (quizDetail == null)
            {
                return NotFound(new { message = "Quiz not found or unauthorized access." });
            }

            return Ok(quizDetail);
        }

        [HttpPost("quizzes")]
        public async Task<IActionResult> CreateQuiz([FromBody] CreateQuizRequestDto request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (!TryGetUserId(out var userId))
            {
                return Unauthorized(new { message = "Unauthenticated" });
            }

            try
            {
                var quiz = await _teacherDashboardService.CreateQuizAsync(userId, request);
                if (quiz == null)
                {
                    return BadRequest(new { message = "Could not create quiz. Teacher assignment not found." });
                }

                return CreatedAtAction(nameof(GetQuizById), new { quizId = quiz.QuizId }, quiz);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("quizzes/{quizId}")]
        public async Task<IActionResult> UpdateQuiz(int quizId, [FromBody] UpdateQuizRequestDto request)
        {
            if (quizId <= 0)
            {
                return BadRequest(new { message = "quizId must be > 0." });
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (!TryGetUserId(out var userId))
            {
                return Unauthorized(new { message = "Unauthenticated" });
            }

            try
            {
                var quiz = await _teacherDashboardService.UpdateQuizAsync(userId, quizId, request);
                if (quiz == null)
                {
                    return NotFound(new { message = "Quiz not found." });
                }

                return Ok(quiz);
            }
            catch (InvalidOperationException ex)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
            }
        }

        [HttpDelete("quizzes/{quizId}")]
        public async Task<IActionResult> DeleteQuiz(int quizId)
        {
            if (quizId <= 0)
            {
                return BadRequest(new { message = "quizId must be > 0." });
            }

            if (!TryGetUserId(out var userId))
            {
                return Unauthorized(new { message = "Unauthenticated" });
            }

            try
            {
                var deleted = await _teacherDashboardService.DeleteQuizAsync(userId, quizId);
                if (!deleted)
                {
                    return NotFound(new { message = "Quiz not found." });
                }

                return NoContent();
            }
            catch (InvalidOperationException ex)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
            }
        }

        [HttpPost("quizzes/{quizId}/grades")]
        public async Task<IActionResult> UpsertQuizGrades(int quizId, [FromBody] UpsertQuizGradesRequestDto request)
        {
            if (quizId <= 0)
            {
                return BadRequest(new { message = "quizId must be > 0." });
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (!TryGetUserId(out var userId))
            {
                return Unauthorized(new { message = "Unauthenticated" });
            }

            try
            {
                var result = await _teacherDashboardService.UpsertQuizGradesAsync(userId, quizId, request);
                if (result == null)
                {
                    return NotFound(new { message = "Quiz not found or unauthorized access." });
                }

                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
            }
        }

        #endregion

        private bool TryGetUserId(out int userId)
        {
            userId = 0;
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            return userIdClaim != null && int.TryParse(userIdClaim.Value, out userId);
        }
    }
}
