using GradeManagementSystem.Core.DTOs.Vice;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GradeManagementSystem.Core.Interfaces
{
    public interface IViceFinalGradesService
    {
        Task<ViceFinalStudentsTableResponseDto?> GetFinalStudentsTableAsync(string level, int semester, string department, int? classId, int subjectId);

        Task<int> UpsertFinalGradesBulkAsync(ViceUpsertFinalGradesRequestDTO request);

        Task<bool> SubmitFinalGradesAsync(ViceSubmitFinalGradesRequestDTO request);

        Task<List<ViceFinalGradeHistoryItemDto>> GetFinalHistoryAsync(string studentId, int subjectId);
    }
}

