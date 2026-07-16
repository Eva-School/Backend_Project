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
    }
}
