using GradeManagementSystem.Core.DTOs.Class;
using GradeManagementSystem.Core.Entities.Enums;
using GradeManagementSystem.Core.Interfaces;
using GradeManagementSystem.Repository.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
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

        public async Task<IEnumerable<ClassCohortSummaryDTO>> GetCohortsSummaryAsync()
        {
            var stages = new[]
            {
                (Stage: EducationStage.Junior, Key: "junior", Name: "Junior"),
                (Stage: EducationStage.Wheeler, Key: "wheeler", Name: "Wheeler"),
                (Stage: EducationStage.Senior, Key: "senior", Name: "Senior")
            };

            var summaries = new List<ClassCohortSummaryDTO>();

            foreach (var (stageEnum, key, name) in stages)
            {
                var academicYear = await _context.AcademicYears
                    .AsNoTracking()
                    .Where(a => a.Stage == stageEnum && a.IsActive)
                    .OrderByDescending(a => a.AcademicYearID)
                    .FirstOrDefaultAsync()
                    ?? await _context.AcademicYears
                        .AsNoTracking()
                        .Where(a => a.Stage == stageEnum)
                        .OrderByDescending(a => a.AcademicYearID)
                        .FirstOrDefaultAsync();

                if (academicYear == null)
                {
                    summaries.Add(new ClassCohortSummaryDTO
                    {
                        Stage = key,
                        StageName = name,
                        AcademicYearId = 0,
                        AcademicYearName = "Not Set",
                        ClassCount = 0,
                        TotalStudents = 0,
                        TotalCapacity = 0
                    });
                    continue;
                }

                var classes = await _context.Classes
                    .AsNoTracking()
                    .Where(c => c.AcademicYearID == academicYear.AcademicYearID && c.IsActive)
                    .ToListAsync();

                var classIds = classes.Select(c => c.ClassID).ToList();
                var totalCapacity = classes.Sum(c => c.Capacity ?? 30);
                var totalStudents = await _context.Students
                    .AsNoTracking()
                    .CountAsync(s => s.CurrentAcademicYearID == academicYear.AcademicYearID && s.ClassID.HasValue && classIds.Contains(s.ClassID.Value));

                summaries.Add(new ClassCohortSummaryDTO
                {
                    Stage = key,
                    StageName = name,
                    AcademicYearId = academicYear.AcademicYearID,
                    AcademicYearName = academicYear.YearName,
                    ClassCount = classes.Count,
                    TotalStudents = totalStudents,
                    TotalCapacity = totalCapacity
                });
            }

            return summaries;
        }

        public async Task<IEnumerable<ClassResponseDTO>> GetClassesByYearIdAsync(string? yearId = null, string? stage = null)
        {
            var academicYearsQuery = _context.AcademicYears.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(yearId))
            {
                if (Enum.TryParse<EducationStage>(yearId, true, out var stageFromYearId))
                {
                    academicYearsQuery = academicYearsQuery.Where(item => item.IsActive && item.Stage == stageFromYearId);
                }
                else
                {
                    academicYearsQuery = academicYearsQuery.Where(item => item.YearName == yearId.Trim());
                    if (!string.IsNullOrWhiteSpace(stage))
                    {
                        if (!Enum.TryParse<EducationStage>(stage, true, out var parsedStage))
                        {
                            return new List<ClassResponseDTO>();
                        }
                        academicYearsQuery = academicYearsQuery.Where(item => item.Stage == parsedStage);
                    }
                }
            }
            else if (!string.IsNullOrWhiteSpace(stage))
            {
                if (!Enum.TryParse<EducationStage>(stage, true, out var parsedStage))
                {
                    return new List<ClassResponseDTO>();
                }
                // When yearId is not supplied, default to active academic year for this stage
                academicYearsQuery = academicYearsQuery.Where(item => item.Stage == parsedStage && item.IsActive);
            }

            var academicYears = await academicYearsQuery.ToListAsync();
            var academicYearIds = academicYears.Select(ay => ay.AcademicYearID).ToList();

            if (!academicYearIds.Any() && !string.IsNullOrWhiteSpace(stage))
            {
                // Fall back to latest academic year for that stage if no active one found
                if (Enum.TryParse<EducationStage>(stage, true, out var parsedStage))
                {
                    var fallbackYear = await _context.AcademicYears
                        .AsNoTracking()
                        .Where(item => item.Stage == parsedStage)
                        .OrderByDescending(item => item.AcademicYearID)
                        .FirstOrDefaultAsync();
                    if (fallbackYear != null)
                    {
                        academicYearIds.Add(fallbackYear.AcademicYearID);
                        academicYears.Add(fallbackYear);
                    }
                }
            }

            if (!academicYearIds.Any())
            {
                return new List<ClassResponseDTO>();
            }

            var yearDict = academicYears.ToDictionary(y => y.AcademicYearID);

            var classes = await _context.Classes
                .AsNoTracking()
                .Where(c => c.AcademicYearID.HasValue && academicYearIds.Contains(c.AcademicYearID.Value) && c.IsActive)
                .Include(c => c.Department)
                .Select(c => new ClassResponseDTO
                {
                    ClassId = c.ClassID,
                    ClassName = c.ClassName,
                    DepartmentId = c.DepartmentID,
                    DepartmentName = c.Department != null ? c.Department.DepartmentName : string.Empty,
                    Capacity = c.Capacity ?? 30,
                    StudentCount = c.Students.Count(),
                    IsActive = c.IsActive,
                    AcademicYearId = c.AcademicYearID,
                    AcademicYearName = string.Empty,
                    Stage = string.Empty
                })
                .ToListAsync();

            foreach (var cls in classes)
            {
                if (cls.AcademicYearId.HasValue && yearDict.TryGetValue(cls.AcademicYearId.Value, out var year))
                {
                    cls.AcademicYearName = year.YearName;
                    cls.Stage = year.Stage.ToString().ToLowerInvariant();
                }
            }

            return classes;
        }

        public async Task<ClassDetailsDTO?> GetClassDetailsAsync(int classId)
        {
            var cls = await _context.Classes
                .AsNoTracking()
                .Include(c => c.Department)
                .Include(c => c.AcademicYear)
                .FirstOrDefaultAsync(c => c.ClassID == classId);

            if (cls == null)
            {
                return null;
            }

            var students = await _context.Students
                .AsNoTracking()
                .Where(s => s.ClassID == classId)
                .Join(_context.Users,
                    s => s.UserID,
                    u => u.UserId,
                    (s, u) => new { Student = s, User = u })
                .OrderBy(x => x.Student.StudentID)
                .ToListAsync();

            var teacherAssignments = await _context.TeacherAssignments
                .AsNoTracking()
                .Where(ta => ta.ClassID == classId && ta.IsActive)
                .Include(ta => ta.Subject)
                .Join(_context.Teachers,
                    ta => ta.TeacherID,
                    t => t.TeacherID,
                    (ta, t) => new { Assignment = ta, Teacher = t })
                .GroupJoin(_context.Users,
                    x => x.Teacher.UserID,
                    u => (int?)u.UserId,
                    (x, users) => new { x.Assignment, x.Teacher, Users = users })
                .SelectMany(
                    x => x.Users.DefaultIfEmpty(),
                    (x, u) => new ClassTeacherAssignmentDTO
                    {
                        TeacherId = x.Teacher.TeacherID,
                        TeacherName = u != null ? u.FullName : "Teacher",
                        SubjectId = x.Assignment.SubjectID ?? 0,
                        SubjectName = x.Assignment.Subject != null ? x.Assignment.Subject.SubjectName : "Subject"
                    })
                .ToListAsync();

            var studentDtos = students.Select(x => new ClassStudentDTO
            {
                StudentId = x.Student.StudentID,
                UserId = x.Student.UserID,
                StudentCode = !string.IsNullOrWhiteSpace(x.Student.StudentCode) ? x.Student.StudentCode : (x.Student.NationalID ?? string.Empty),
                NationalId = x.Student.NationalID ?? string.Empty,
                FullName = !string.IsNullOrWhiteSpace(x.User.FullName) ? x.User.FullName : $"{x.User.FirstName} {x.User.LastName}".Trim(),
                FirstName = x.User.FirstName,
                MiddleName = x.User.MiddleName ?? string.Empty,
                LastName = x.User.LastName,
                Email = x.User.Email ?? string.Empty,
                Phone = !string.IsNullOrWhiteSpace(x.User.PhoneNumber) ? x.User.PhoneNumber : (x.Student.StudentPhone ?? string.Empty),
                Gender = x.Student.Gender.ToString(),
                Address = !string.IsNullOrWhiteSpace(x.Student.Address) ? x.Student.Address : (x.Student.AddressArabic ?? string.Empty),
                Status = x.Student.Status ?? "Active",
                EnrollmentDate = x.Student.EnrollmentDate
            }).ToList();

            return new ClassDetailsDTO
            {
                ClassId = cls.ClassID,
                ClassName = cls.ClassName,
                DepartmentId = cls.DepartmentID,
                DepartmentName = cls.Department?.DepartmentName ?? string.Empty,
                Capacity = cls.Capacity ?? 30,
                StudentCount = studentDtos.Count,
                IsActive = cls.IsActive,
                AcademicYearId = cls.AcademicYearID,
                AcademicYearName = cls.AcademicYear?.YearName ?? string.Empty,
                Stage = cls.AcademicYear != null ? cls.AcademicYear.Stage.ToString().ToLowerInvariant() : string.Empty,
                Teachers = teacherAssignments,
                Students = studentDtos
            };
        }

        public async Task<ClassResponseDTO?> CreateClassAsync(CreateClassRequestDTO request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.YearId) ||
                string.IsNullOrWhiteSpace(request.Department) || string.IsNullOrWhiteSpace(request.ClassName))
            {
                return null;
            }

            var trimmedYear = request.YearId.Trim();
            int.TryParse(trimmedYear, out var parsedYearId);

            var academicYears = _context.AcademicYears.AsQueryable();
            if (parsedYearId > 0)
            {
                academicYears = academicYears.Where(year => year.AcademicYearID == parsedYearId || year.YearName == trimmedYear);
            }
            else
            {
                academicYears = academicYears.Where(year => year.YearName == trimmedYear);
            }

            if (!string.IsNullOrWhiteSpace(request.Stage))
            {
                if (!Enum.TryParse<EducationStage>(request.Stage, true, out var stage))
                {
                    throw new InvalidOperationException("Invalid stage. Expected: junior|wheeler|senior.");
                }
                academicYears = academicYears.Where(year => year.Stage == stage);
            }
            else
            {
                academicYears = academicYears.Where(year => year.IsActive);
            }

            var academicYear = await academicYears
                .OrderByDescending(year => year.AcademicYearID)
                .FirstOrDefaultAsync();
            if (academicYear == null)
            {
                return null;
            }

            var trimmedDept = request.Department.Trim();
            int.TryParse(trimmedDept, out var parsedDeptId);
            var department = await _context.Departments
                .FirstOrDefaultAsync(item => item.IsActive && (item.DepartmentName == trimmedDept || (parsedDeptId > 0 && item.DepartmentID == parsedDeptId)));
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
                Capacity = request.Capacity ?? 30,
                IsActive = true
            };
            _context.Classes.Add(created);
            await _context.SaveChangesAsync();

            return new ClassResponseDTO
            {
                ClassId = created.ClassID,
                ClassName = created.ClassName,
                DepartmentId = created.DepartmentID,
                DepartmentName = department.DepartmentName,
                Capacity = created.Capacity,
                StudentCount = 0,
                IsActive = created.IsActive,
                AcademicYearId = academicYear.AcademicYearID,
                AcademicYearName = academicYear.YearName,
                Stage = academicYear.Stage.ToString().ToLowerInvariant()
            };
        }

        public async Task<ClassResponseDTO?> UpdateClassAsync(int classId, UpdateClassRequestDTO request)
        {
            if (request == null)
            {
                return null;
            }

            var cls = await _context.Classes
                .Include(c => c.Department)
                .Include(c => c.AcademicYear)
                .FirstOrDefaultAsync(c => c.ClassID == classId);

            if (cls == null)
            {
                return null;
            }

            var trimmedDept = request.Department.Trim();
            int.TryParse(trimmedDept, out var parsedDeptId);
            var department = await _context.Departments
                .FirstOrDefaultAsync(d => d.IsActive && (d.DepartmentName == trimmedDept || (parsedDeptId > 0 && d.DepartmentID == parsedDeptId)));
            if (department == null)
            {
                throw new InvalidOperationException($"Department '{request.Department}' was not found.");
            }

            var className = request.ClassName.Trim();
            var exists = await _context.Classes.AnyAsync(c =>
                c.ClassID != classId &&
                c.AcademicYearID == cls.AcademicYearID &&
                c.DepartmentID == department.DepartmentID &&
                c.ClassName == className &&
                c.IsActive);

            if (exists)
            {
                throw new InvalidOperationException("A class with this name already exists for the selected year and department.");
            }

            cls.ClassName = className;
            cls.DepartmentID = department.DepartmentID;
            cls.Capacity = request.Capacity;
            cls.IsActive = request.IsActive;

            await _context.SaveChangesAsync();

            var studentCount = await _context.Students.CountAsync(s => s.ClassID == classId);

            return new ClassResponseDTO
            {
                ClassId = cls.ClassID,
                ClassName = cls.ClassName,
                DepartmentId = cls.DepartmentID,
                DepartmentName = department.DepartmentName,
                Capacity = cls.Capacity,
                StudentCount = studentCount,
                IsActive = cls.IsActive,
                AcademicYearId = cls.AcademicYearID,
                AcademicYearName = cls.AcademicYear?.YearName ?? string.Empty,
                Stage = cls.AcademicYear != null ? cls.AcademicYear.Stage.ToString().ToLowerInvariant() : string.Empty
            };
        }

        public async Task<(bool Success, string Message)> DeleteClassAsync(int classId)
        {
            var cls = await _context.Classes
                .Include(c => c.Students)
                .Include(c => c.TeacherAssignments)
                .FirstOrDefaultAsync(c => c.ClassID == classId);

            if (cls == null)
            {
                return (false, "Class not found.");
            }

            // Safely unassign students
            if (cls.Students.Any())
            {
                foreach (var student in cls.Students)
                {
                    student.ClassID = null;
                }
            }

            // Remove teacher assignments
            if (cls.TeacherAssignments.Any())
            {
                _context.TeacherAssignments.RemoveRange(cls.TeacherAssignments);
            }

            _context.Classes.Remove(cls);
            await _context.SaveChangesAsync();

            return (true, "Class deleted successfully.");
        }
    }
}
