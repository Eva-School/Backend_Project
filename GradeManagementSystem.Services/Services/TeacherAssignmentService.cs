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
            var teacherExists = await _context.Teachers.AnyAsync(t => t.TeacherID == teacherIdInt);
            if (!teacherExists)
            {
                return (false, "Teacher not found");
            }

            // Validate Subject
            if (!int.TryParse(request.SubjectId, out int subjectIdInt))
            {
                return (false, "Invalid SubjectId format");
            }
            var subjectExists = await _context.Subjects.AnyAsync(s => s.SubjectID == subjectIdInt);
            if (!subjectExists)
            {
                return (false, "Subject not found");
            }

            // Validate Academic Year
            var academicYear = await _context.AcademicYears
                .FirstOrDefaultAsync(ay => ay.YearName == request.YearId && ay.IsActive);
            if (academicYear == null)
            {
                return (false, "Academic year not found or not active");
            }

            // Validate Classes
            var existingClassIds = await _context.Classes
                .Where(c => request.ClassIds.Contains(c.ClassID))
                .Select(c => c.ClassID)
                .ToListAsync();

            var missingClassIds = request.ClassIds.Except(existingClassIds).ToList();
            if (missingClassIds.Any())
            {
                return (false, $"Class(es) with ID(s) {string.Join(", ", missingClassIds)} not found");
            }

            var assignmentsToAdd = new List<TeacherAssignment>();
            foreach (var classId in request.ClassIds)
            {
                // Check for existing assignment to prevent duplicates based on composite key
                var existingAssignment = await _context.TeacherAssignments.AnyAsync(
                    ta => ta.TeacherID == teacherIdInt &&
                          ta.ClassID == classId &&
                          ta.SubjectID == subjectIdInt &&
                          ta.AcademicYearID == academicYear.AcademicYearID);

                if (!existingAssignment)
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
            }

            if (!assignmentsToAdd.Any())
            {
                return (true, "All teachers are already assigned to the specified classes for this subject and academic year.");
            }

            await _context.TeacherAssignments.AddRangeAsync(assignmentsToAdd);
            await _context.SaveChangesAsync();

            return (true, "Teacher assigned successfully");
        }

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