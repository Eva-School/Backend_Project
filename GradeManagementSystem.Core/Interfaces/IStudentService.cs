using GradeManagementSystem.Core.DTOs.Student;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GradeManagementSystem.Core.Interfaces
{
    public interface IStudentService
    {
        Task<StudentProfileResponse?> GetProfileAsync(int userId);
        Task<List<YearOptionResponse>> GetYearsAsync();
        Task<GradesResponse<QuarterGradeRow>?> GetQuarterGradesAsync(int userId, string year);
        Task<GradesResponse<FinalGradeRow>?> GetFinalGradesAsync(int userId, string year);
        Task<GradesResponse<JadaratGradeRow>?> GetJadaratGradesAsync(int userId, string year);
    }
}
