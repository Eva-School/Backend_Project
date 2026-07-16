using GradeManagementSystem.Core.DTOs.TeacherAssignment;
using GradeManagementSystem.Core.Entities.Domain;
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
    public class TeacherAssignmentService : ITeacherAssignmentService
    {
        private readonly GradeDbContext _context;

        public TeacherAssignmentService(GradeDbContext context)
        {
            _context = context;
        }

        public async Task<(bool success, string message)> AssignTeacherToClassesAsync(TeacherAssignmentRequestDTO request)
        {
            // Validate input fields
            if (string.IsNullOrWhiteSpace(request.TeacherId) ||
                string.IsNullOrWhiteSpace(request.YearId) ||
                string.IsNullOrWhiteSpace(request.SubjectId) ||
                request.ClassIds == null || !request.ClassIds.Any())
            {
                return (false, "All fields are required");
            }

            // Validate Teacher
            if (!int.TryParse(request.TeacherId, out int teacherIdInt))
            {
                return (false, "Invalid TeacherId format");
            }
            var teacherExists = await _context.Teachers.AnyAsync(t => t.TeacherID == teacherIdInt && t.IsActive);
            if (!teacherExists)
            {
                return (false, "Teacher not found");
            }

            // Validate Subject
            if (!int.TryParse(request.SubjectId, out int subjectIdInt))
            {
                return (false, "Invalid SubjectId format");
            }
            // Validate Academic Year
            var academicYear = await _context.AcademicYears
                .FirstOrDefaultAsync(ay => ay.YearName == request.YearId && ay.IsActive);
            if (academicYear == null)
            {
                return (false, "Academic year not found or not active");
            }

            var subjectExists = await _context.Subjects.AnyAsync(s =>
                s.SubjectID == subjectIdInt && s.IsActive);
            if (!subjectExists)
            {
                return (false, "The selected subject was not found or is inactive.");
            }

            // Validate Classes
            var existingClassIds = await _context.Classes
                .Where(c => request.ClassIds.Contains(c.ClassID) && c.IsActive && c.AcademicYearID == academicYear.AcademicYearID)
                .Select(c => c.ClassID)
                .ToListAsync();

            var missingClassIds = request.ClassIds.Except(existingClassIds).ToList();
            if (missingClassIds.Any())
            {
                return (false, $"Class(es) with ID(s) {string.Join(", ", missingClassIds)} not found");
            }

            var assignmentsToAdd = new List<TeacherAssignment>();
            var reactivatedAny = false;
            foreach (var classId in request.ClassIds)
            {
                // Check for existing assignment to prevent duplicates based on composite key
                var existingAssignment = await _context.TeacherAssignments.FirstOrDefaultAsync(
                    ta => ta.TeacherID == teacherIdInt &&
                          ta.ClassID == classId &&
                          ta.SubjectID == subjectIdInt &&
                          ta.AcademicYearID == academicYear.AcademicYearID);

                if (existingAssignment == null)
                {
                    assignmentsToAdd.Add(new TeacherAssignment
                    {
                        TeacherID = teacherIdInt,
                        ClassID = classId,
                        SubjectID = subjectIdInt,
                        AcademicYearID = academicYear.AcademicYearID,
                        AssignedAt = DateTime.UtcNow,
                        IsActive = true
                    });
                }
                else if (!existingAssignment.IsActive)
                {
                    existingAssignment.IsActive = true;
                    reactivatedAny = true;
                }
            }

            if (!assignmentsToAdd.Any() && !reactivatedAny)
            {
                return (true, "All teachers are already assigned to the specified classes for this subject and academic year.");
            }

            if (assignmentsToAdd.Any()) await _context.TeacherAssignments.AddRangeAsync(assignmentsToAdd);
            await _context.SaveChangesAsync();

            return (true, "Teacher assigned successfully");
        }

        public async Task<(bool success, string message)> ReplaceTeacherAssignmentClassesAsync(TeacherAssignmentRequestDTO request)
        {
            var validation = await AssignTeacherToClassesAsync(new TeacherAssignmentRequestDTO
            {
                TeacherId = request.TeacherId,
                YearId = request.YearId,
                SubjectId = request.SubjectId,
                ClassIds = request.ClassIds
            });
            if (!validation.success) return validation;

            if (!int.TryParse(request.TeacherId, out var teacherId) || !int.TryParse(request.SubjectId, out var subjectId))
                return (false, "Teacher and subject identifiers must be numeric.");
            var academicYear = await _context.AcademicYears.FirstOrDefaultAsync(item => item.YearName == request.YearId && item.IsActive);
            if (academicYear == null) return (false, "Academic year not found or not active.");

            var retained = request.ClassIds.Distinct().ToHashSet();
            var obsolete = await _context.TeacherAssignments.Where(item =>
                item.TeacherID == teacherId && item.SubjectID == subjectId && item.AcademicYearID == academicYear.AcademicYearID &&
                item.IsActive && item.ClassID.HasValue && !retained.Contains(item.ClassID.Value)).ToListAsync();
            foreach (var assignment in obsolete) assignment.IsActive = false;
            await _context.SaveChangesAsync();
            return (true, "Teacher assignment updated successfully.");
        }

        public async Task<List<TeacherAssignmentListItemDto>> GetAssignmentsAsync(string? yearName, string? stage)
        {
            IQueryable<TeacherAssignment> query = _context.TeacherAssignments
                .Include(item => item.Teacher).ThenInclude(teacher => teacher.Department)
                .Include(item => item.Class)
                .Include(item => item.Subject)
                .Include(item => item.AcademicYear);

            if (!string.IsNullOrWhiteSpace(yearName)) query = query.Where(item => item.AcademicYear.YearName == yearName.Trim());
            if (!string.IsNullOrWhiteSpace(stage))
            {
                if (!Enum.TryParse<GradeManagementSystem.Core.Entities.Enums.EducationStage>(stage, true, out var parsedStage))
                    throw new ArgumentException("Invalid stage. Expected: junior|wheeler|senior.");
                query = query.Where(item => item.AcademicYear.Stage == parsedStage);
            }

            return await query
                .Join(_context.Users, item => item.Teacher.UserID, user => user.UserId, (item, user) => new TeacherAssignmentListItemDto
                {
                    TeacherId = item.TeacherID ?? 0,
                    TeacherName = user.FullName,
                    AcademicYearId = item.AcademicYearID ?? 0,
                    YearName = item.AcademicYear.YearName,
                    Stage = item.AcademicYear.Stage.ToString().ToLowerInvariant(),
                    SubjectId = item.SubjectID ?? 0,
                    SubjectName = item.Subject.SubjectName,
                    ClassId = item.ClassID ?? 0,
                    ClassName = item.Class.ClassName,
                    IsActive = item.IsActive,
                    AssignedAt = item.AssignedAt
                })
                .OrderByDescending(item => item.AssignedAt).ThenBy(item => item.TeacherName).ThenBy(item => item.ClassName)
                .ToListAsync();
        }

        public async Task<(bool success, string message)> SetAssignmentStatusAsync(TeacherAssignmentStatusRequestDto request)
        {
            var assignment = await FindAssignmentAsync(request);
            if (assignment == null) return (false, "Teacher assignment not found.");
            assignment.IsActive = request.IsActive;
            await _context.SaveChangesAsync();
            return (true, request.IsActive ? "Teacher assignment activated." : "Teacher assignment deactivated.");
        }

        public async Task<(bool success, string message)> DeleteAssignmentAsync(TeacherAssignmentStatusRequestDto request)
        {
            var assignment = await FindAssignmentAsync(request);
            if (assignment == null) return (false, "Teacher assignment not found.");
            _context.TeacherAssignments.Remove(assignment);
            await _context.SaveChangesAsync();
            return (true, "Teacher assignment deleted.");
        }

        private Task<TeacherAssignment?> FindAssignmentAsync(TeacherAssignmentStatusRequestDto request) =>
            _context.TeacherAssignments.FirstOrDefaultAsync(item => item.TeacherID == request.TeacherId &&
                item.AcademicYearID == request.AcademicYearId && item.SubjectID == request.SubjectId && item.ClassID == request.ClassId);

        public async Task<List<TeacherAssignmentDashboardYearDto>> GetMyDashboardAsync(int teacherUserId)
        {
            var teacher = await _context.Teachers
                .FirstOrDefaultAsync(t => t.UserID == teacherUserId && t.IsActive);

            if (teacher == null)
            {
                return new List<TeacherAssignmentDashboardYearDto>();
            }

            var assignments = await _context.TeacherAssignments
                .Where(ta =>
                    ta.TeacherID == teacher.TeacherID &&
                    ta.IsActive &&
                    ta.AcademicYearID.HasValue &&
                    ta.ClassID.HasValue &&
                    ta.AcademicYear.IsActive &&
                    ta.Class.IsActive)
                .Select(ta => new
                {
                    ta.AcademicYear.YearName,
                    ClassId = ta.ClassID!.Value,
                    ClassName = ta.Class.ClassName
                })
                .ToListAsync();

            var result = assignments
                .GroupBy(x => x.YearName)
                .Select(g => new TeacherAssignmentDashboardYearDto
                {
                    YearId = g.Key,
                    Classes = g
                        .GroupBy(c => c.ClassId)
                        .Select(cg => new ClassResponseDTO { ClassId = cg.Key, ClassName = cg.First().ClassName })
                        .OrderBy(c => c.ClassName)
                        .ToList()
                })
                .OrderBy(y => y.YearId)
                .ToList();

            return result;
        }

        public async Task<List<ClassResponseDTO>> GetMyClassesAsync(int teacherUserId, string yearId)
        {
            if (string.IsNullOrWhiteSpace(yearId))
            {
                return new List<ClassResponseDTO>();
            }

            var teacher = await _context.Teachers
                .FirstOrDefaultAsync(t => t.UserID == teacherUserId && t.IsActive);

            if (teacher == null)
            {
                return new List<ClassResponseDTO>();
            }

            var baseQuery = _context.TeacherAssignments
                .Where(ta =>
                    ta.TeacherID == teacher.TeacherID &&
                    ta.IsActive &&
                    ta.AcademicYearID.HasValue &&
                    ta.ClassID.HasValue &&
                    ta.AcademicYear.IsActive &&
                    ta.Class.IsActive);

            if (int.TryParse(yearId, out var academicYearNumericId))
            {
                baseQuery = baseQuery.Where(ta => ta.AcademicYearID == academicYearNumericId);
            }
            else
            {
                baseQuery = baseQuery.Where(ta => ta.AcademicYear.YearName == yearId);
            }

            var classes = await baseQuery
                .Select(ta => new
                {
                    ClassId = ta.ClassID!.Value,
                    ClassName = ta.Class.ClassName
                })
                .ToListAsync();

            var result = classes
                .GroupBy(c => c.ClassId)
                .Select(g => new ClassResponseDTO { ClassId = g.Key, ClassName = g.First().ClassName })
                .OrderBy(c => c.ClassName)
                .ToList();

            return result;
        }
    }
}
