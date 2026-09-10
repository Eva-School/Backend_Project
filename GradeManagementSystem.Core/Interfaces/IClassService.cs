using GradeManagementSystem.Core.DTOs.Class;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GradeManagementSystem.Core.Interfaces
{
    public interface IClassService
    {
        Task<IEnumerable<ClassCohortSummaryDTO>> GetCohortsSummaryAsync();
        Task<IEnumerable<ClassResponseDTO>> GetClassesByYearIdAsync(string? yearId = null, string? stage = null);
        Task<ClassDetailsDTO?> GetClassDetailsAsync(int classId);
        Task<ClassResponseDTO?> CreateClassAsync(CreateClassRequestDTO request);
        Task<ClassResponseDTO?> UpdateClassAsync(int classId, UpdateClassRequestDTO request);
        Task<(bool Success, string Message)> DeleteClassAsync(int classId);
    }
}
