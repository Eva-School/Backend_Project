using GradeManagementSystem.Core.DTOs.Subject;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GradeManagementSystem.Core.Interfaces
{
    public interface ISubjectService
    {
        Task<IEnumerable<SubjectResponseDTO>> GetSubjectsForActiveYearAsync();
        Task<SubjectResponseDTO> CreateSubjectAsync(CreateSubjectRequestDTO request);
    }
}
