using GradeManagementSystem.Core.DTOs.Vice;
using System.Threading.Tasks;

namespace GradeManagementSystem.Core.Interfaces
{
    public interface IAdminFinalGradesService
    {
        Task<string?> ApproveAndLockFinalGradesAsync(ViceFinalApproveRequestDTO request);
    }
}

