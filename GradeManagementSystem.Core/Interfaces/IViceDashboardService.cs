using GradeManagementSystem.Core.DTOs.Vice;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GradeManagementSystem.Core.Interfaces
{
    public interface IViceDashboardService
    {
        Task<IEnumerable<ViceDashboardCardDto>> GetCardsAsync();
        Task<ViceGradesDashboardResponseDto> GetGradesDashboardAsync(string? academicYear = null);
    }
}

