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

        public async Task<ClassResponseDTO?> CreateClassAsync(CreateClassRequestDTO request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.YearId) ||
                string.IsNullOrWhiteSpace(request.Department) || string.IsNullOrWhiteSpace(request.ClassName))
            {
                return null;
            }

            var academicYear = await _context.AcademicYears
                .Where(year => year.IsActive && year.YearName == request.YearId.Trim())
                .OrderByDescending(year => year.AcademicYearID)
                .FirstOrDefaultAsync();
            if (academicYear == null)
            {
                return null;
            }

            var department = await _context.Departments
                .FirstOrDefaultAsync(item => item.IsActive && item.DepartmentName == request.Department.Trim());
            if (department == null)
            {
                return null;
            }

            var className = request.ClassName.Trim();
            var exists = await _context.Classes.AnyAsync(item =>
                item.IsActive && item.AcademicYearID == academicYear.AcademicYearID &&
                item.DepartmentID == department.DepartmentID && item.ClassName == className);
            if (exists)
            {
                throw new InvalidOperationException("A class with this name already exists for the selected year and department.");
            }

            var created = new GradeManagementSystem.Core.Entities.Domain.Class
            {
                AcademicYearID = academicYear.AcademicYearID,
                DepartmentID = department.DepartmentID,
                ClassName = className,
                Capacity = request.Capacity,
                IsActive = true
            };
            _context.Classes.Add(created);
            await _context.SaveChangesAsync();

            return new ClassResponseDTO { ClassId = created.ClassID, ClassName = created.ClassName };
        }
    }

}
