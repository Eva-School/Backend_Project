using GradeManagementSystem.Core.DTOs.Vice;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GradeManagementSystem.Core.Interfaces
{
    public interface IViceStudentService
    {
        Task<List<ViceStudentDto>> GetStudentsAsync(string year, string department, int? classId, bool unassigned = false, string? academicYearName = null);

        Task<ViceStudentDto?> CreateStudentAsync(ViceCreateStudentRequestDTO request);

        Task<ViceStudentDto?> UpdateStudentAsync(string studentId, ViceCreateStudentRequestDTO request);

        Task<bool> DeleteStudentAsync(string studentId);
        Task<ViceStudentDto?> AssignStudentToClassAsync(string studentId, int? classId);
        Task<int> PromoteStudentsAsync(VicePromoteStudentsRequestDTO request, int? requestedBy);
        Task<ViceBulkImportStudentsResponseDTO> ImportStudentsFromExcelAsync(System.IO.Stream fileStream, string fileName, string defaultYear, string defaultDepartment, string? defaultAcademicYearName = null);
    }
}
