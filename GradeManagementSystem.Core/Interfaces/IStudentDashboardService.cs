using GradeManagementSystem.Core.DTOs.Student;

namespace GradeManagementSystem.Core.Interfaces
{
    public interface IStudentDashboardService
    {
        Task<IEnumerable<StudentCardDto>> GetCardsAsync();
        Task<StudentProfileDto?> GetProfileAsync(int userId);
        Task<bool> UpdateProfileAsync(int userId, UpdateStudentContactDto request);
        Task<IEnumerable<StudentYearOptionDto>> GetYearsAsync();
        Task<StudentQuarterGradesResponseDto?> GetQuarterGradesAsync(int userId, string year, int? termId = null);
        Task<StudentFinalGradesResponseDto?> GetFinalGradesAsync(int userId, string year);
        Task<StudentCompetenciesResponseDto?> GetJadaratGradesAsync(int userId, string year);
        Task<IEnumerable<StudentProgressPointDto>> GetProgressAsync(int userId, string year);
        Task<StudentReportDto?> GetReportAsync(int userId, string year);
        Task<StudentEnrollmentDetailsDto?> GetEnrollmentDetailsAsync(int userId);
    }
}
