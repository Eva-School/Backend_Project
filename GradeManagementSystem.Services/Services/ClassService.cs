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

        public async Task<IEnumerable<ClassResponseDTO>> GetClassesByYearIdAsync(string yearId, string? stage = null)
        {
            if (string.IsNullOrWhiteSpace(yearId))
            {
                return null; // Or throw an exception, depending on desired error handling
            }

            var academicYears = _context.AcademicYears.AsNoTracking();
            if (Enum.TryParse<GradeManagementSystem.Core.Entities.Enums.EducationStage>(yearId, true, out var stageFromYearId))
            {
                // Existing grade pages use a level such as "junior" as yearId.
                academicYears = academicYears.Where(item => item.IsActive && item.Stage == stageFromYearId);
            }
            else
            {
                academicYears = academicYears.Where(item => item.YearName == yearId.Trim());
                if (!string.IsNullOrWhiteSpace(stage))
                {
                    if (!Enum.TryParse<GradeManagementSystem.Core.Entities.Enums.EducationStage>(stage, true, out var parsedStage))
                    {
                        return new List<ClassResponseDTO>();
                    }
                    academicYears = academicYears.Where(item => item.Stage == parsedStage);
                }
            }

            var academicYearIds = await academicYears
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

            var academicYears = _context.AcademicYears
                .Where(year => year.YearName == request.YearId.Trim());
            if (!string.IsNullOrWhiteSpace(request.Stage))
            {
                if (!Enum.TryParse<GradeManagementSystem.Core.Entities.Enums.EducationStage>(request.Stage, true, out var stage))
                {
                    throw new InvalidOperationException("Invalid stage. Expected: junior|wheeler|senior.");
                }
                academicYears = academicYears.Where(year => year.Stage == stage);
            }
            else
            {
                // Preserve behavior for existing callers that do not send a stage.
                academicYears = academicYears.Where(year => year.IsActive);
            }

            var academicYear = await academicYears
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
                // Capacity is required by the database, while the Student
                // Affairs form currently does not ask for it.
                Capacity = request.Capacity ?? 30,
                IsActive = true
            };
            _context.Classes.Add(created);
            await _context.SaveChangesAsync();

            return new ClassResponseDTO { ClassId = created.ClassID, ClassName = created.ClassName };
        }
    }

}
