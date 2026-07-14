using GradeManagementSystem.Core.DTOs.Student;

namespace GradeManagementSystem.Core.Interfaces
{
    public interface IStudentDashboardService
    {
        Task<IEnumerable<StudentCardDto>> GetCardsAsync();
        Task<StudentProfileDto?> GetProfileAsync(int userId);
        Task<IEnumerable<StudentYearOptionDto>> GetYearsAsync();
        Task<StudentGradesResponseDto?> GetQuarterGradesAsync(int userId, string year);
        Task<StudentGradesResponseDto?> GetFinalGradesAsync(int userId, string year);
        Task<StudentCompetenciesResponseDto?> GetJadaratGradesAsync(int userId, string year);
        Task<IEnumerable<StudentProgressPointDto>> GetProgressAsync(int userId, string year);
        Task<StudentReportDto?> GetReportAsync(int userId, string year);
    }
}
