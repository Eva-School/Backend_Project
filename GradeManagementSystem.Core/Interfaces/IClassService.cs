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
        Task<IEnumerable<ClassResponseDTO>> GetClassesByYearIdAsync(string yearId);
        Task<ClassResponseDTO?> CreateClassAsync(CreateClassRequestDTO request);
    }
}
