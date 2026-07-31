using GradeManagementSystem.Core.DTOs.Class;
using GradeManagementSystem.Core.DTOs.Teacher;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GradeManagementSystem.Core.Interfaces
{
    public interface ITeacherDashboardService
    {
        Task<TeacherProfileDto?> GetProfileAsync(int userId);
        Task<List<TeacherSubjectYearGroupDto>> GetSubjectsAsync(int userId);
        Task<List<ClassResponseDTO>?> GetClassesAsync(int userId, string year, string subject);
        Task<List<TeacherStudentGradeDto>?> GetStudentsAsync(int userId, int classId, int subjectId);
        Task<TeacherGradeUpdateResponseDto?> UpsertGradeAsync(int userId, TeacherGradeUpdateRequestDTO request);

        // Quiz Methods
        Task<List<QuizDto>> GetQuizzesAsync(int userId, int classId, int subjectId);
        Task<QuizDetailDto?> GetQuizByIdAsync(int userId, int quizId);
        Task<QuizDto?> CreateQuizAsync(int userId, CreateQuizRequestDto dto);
        Task<QuizDto?> UpdateQuizAsync(int userId, int quizId, UpdateQuizRequestDto dto);
        Task<bool> DeleteQuizAsync(int userId, int quizId);
        Task<QuizDetailDto?> UpsertQuizGradesAsync(int userId, int quizId, UpsertQuizGradesRequestDto dto);
    }
}
