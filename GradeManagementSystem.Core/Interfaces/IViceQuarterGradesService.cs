using GradeManagementSystem.Core.DTOs.Vice;
using System.Threading.Tasks;

namespace GradeManagementSystem.Core.Interfaces
{
    public interface IViceQuarterGradesService
    {
        Task<ViceQuarterMaxGradesDto?> SetSubjectQuarterMaxGradesAsync(int subjectId, ViceSetQuarterMaxGradesRequestDTO request);

        Task<ViceQuarterStudentsSheetResponseDto?> GetQuarterStudentsSheetAsync(string level, int subjectId, string department, int? classId);

        Task<int> UpsertQuarterGradesBulkAsync(ViceUpsertQuarterGradesRequestDTO request);
    }
}

