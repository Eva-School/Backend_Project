using GradeManagementSystem.Core.DTOs.Subject;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GradeManagementSystem.Core.Interfaces
{
    public interface ISubjectService
    {
        Task<IEnumerable<SubjectResponseDTO>> GetSubjectsForActiveYearAsync(string? yearName = null, string? stage = null);
        Task<SubjectResponseDTO> CreateSubjectAsync(CreateSubjectRequestDTO request);
    }
}
