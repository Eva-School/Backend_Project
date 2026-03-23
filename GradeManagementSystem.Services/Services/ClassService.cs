using GradeManagementSystem.Core.DTOs.Class;
using GradeManagementSystem.Core.Interfaces;
using GradeManagementSystem.Repository.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GradeManagementSystem.Services.Services
{
    public class ClassService : IClassService
    {
        private readonly GradeDbContext _context;

        public ClassService(GradeDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<ClassResponseDTO>> GetClassesByYearIdAsync(string yearId)
        {
            if (string.IsNullOrWhiteSpace(yearId))
            {
                return null; // Or throw an exception, depending on desired error handling
            }

            // Find the AcademicYearID based on the provided yearId (YearName)
            // Note: There can be multiple academic years with the same YearName but different stages.
            // For simplicity, we'll get classes associated with any active academic year matching the yearId.
            // A more robust solution might require specifying the stage as well.
            var academicYearIds = await _context.AcademicYears
                .Where(ay => ay.YearName == yearId && ay.IsActive)
                .Select(ay => ay.AcademicYearID)
                .ToListAsync();

            if (!academicYearIds.Any())
            {
                return new List<ClassResponseDTO>(); // No active academic year found for the given yearId
            }

            var classes = await _context.Classes
                .Where(c => academicYearIds.Contains(c.AcademicYearID.Value) && c.IsActive)
                .Select(c => new ClassResponseDTO
                {
                    ClassId = c.ClassID,
                    ClassName = c.ClassName
                })
                .ToListAsync();

            return classes;
        }
    }

}
