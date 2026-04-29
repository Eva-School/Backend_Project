using GradeManagementSystem.Core.DTOs.Vice;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GradeManagementSystem.Core.Interfaces
{
    public interface IViceStudentService
    {
        Task<List<ViceStudentDto>> GetStudentsAsync(string year, string department, int? classId);

        Task<ViceStudentDto?> CreateStudentAsync(ViceCreateStudentRequestDTO request);

        Task<ViceStudentDto?> UpdateStudentAsync(string studentId, ViceCreateStudentRequestDTO request);

        Task<bool> DeleteStudentAsync(string studentId);
    }
}

