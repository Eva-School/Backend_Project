using GradeManagementSystem.Core.DTOs.Vice;
using GradeManagementSystem.Core.Entities.Domain;
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
    public class AdminFinalGradesService : IAdminFinalGradesService
    {
        private readonly GradeDbContext _context;

        public AdminFinalGradesService(GradeDbContext context)
        {
            _context = context;
        }

        public async Task<string?> ApproveAndLockFinalGradesAsync(ViceFinalApproveRequestDTO request)
        {
            if (request == null)
            {
                return null;
            }

            if (!Enum.TryParse<EducationStage>(request.Level, true, out var stage))
            {
                return null;
            }

            var academicYear = await _context.AcademicYears
                .Where(a => a.IsActive && a.Stage == stage)
                .OrderByDescending(a => a.AcademicYearID)
                .FirstOrDefaultAsync();
            if (academicYear == null)
            {
                return null;
            }

            var dept = await _context.Departments.FirstOrDefaultAsync(d => d.IsActive && d.DepartmentName == request.Department);
            if (dept == null)
            {
                return null;
            }

            var termId = await ResolveTermIdAsync(academicYear.AcademicYearID, 1 /* fallback */);
            if (termId == null)
            {
                return null;
            }

            // Map semester -> term index similar to ViceFinalGradesService
            termId = await ResolveTermIdAsync(academicYear.AcademicYearID, request.Semester);
            if (termId == null)
            {
                return null;
            }

            var subjectExists = await _context.Subjects
                .AnyAsync(s => s.SubjectID == request.SubjectId && s.IsActive && s.AcademicYearID == academicYear.AcademicYearID);
            if (!subjectExists)
            {
                return null;
            }

            var classId = request.ClassId;
            int? classIdInt = null;
            if (!string.IsNullOrWhiteSpace(classId) && int.TryParse(classId, out var parsed))
            {
                classIdInt = parsed;
            }

            IQueryable<StudentAllResults> rowsQuery = _context.StudentAllResults
                .Where(ar =>
                    ar.SubjectID == request.SubjectId &&
                    ar.TermID == termId.Value &&
                    ar.AcademicYearID == academicYear.AcademicYearID &&
                    ar.StudentID.HasValue)
                .Include(ar => ar.Subject)
                .Include(ar => ar.ResultApproval)
                .Include(ar => ar.Student)
                    .ThenInclude(s => s.Class);

            if (classIdInt.HasValue)
            {
                var cid = classIdInt.Value;
                rowsQuery = rowsQuery.Where(ar => ar.Student != null && ar.Student.ClassID == cid);
            }

            var rows = await rowsQuery.ToListAsync();
            if (!rows.Any())
            {
                return "No grades found for the provided filters.";
            }

            var now = DateTime.UtcNow;
            foreach (var row in rows)
            {
                if (row.ResultApproval != null && row.ResultApproval.Decision == Decision.Approved)
                {
                    continue;
                }

                if (row.ResultApproval == null)
                {
                    row.ResultApproval = new ResultApproval
                    {
                        AllResultID = row.AllResultID,
                        Decision = Decision.Approved,
                        Notes = "",
                        ApprovalDate = now,
                        ApprovedBy = null
                    };
                }
                else
                {
                    row.ResultApproval.Decision = Decision.Approved;
                    row.ResultApproval.ApprovalDate = now;
                }

                _context.GradeActionLogs.Add(new GradeActionLog
                {
                    Action = "Approved final grades",
                    ActorUserID = null,
                    ActorName = "Admin",
                    StudentID = row.StudentID,
                    SubjectID = request.SubjectId,
                    AcademicYearID = academicYear.AcademicYearID,
                    DepartmentID = dept.DepartmentID,
                    ClassID = row.Student?.ClassID,
                    TermID = termId.Value,
                    Level = stage.ToString().ToLowerInvariant(),
                    SubjectName = row.Subject?.SubjectName,
                    ClassName = row.Student?.Class?.ClassName,
                    Timestamp = now
                });
            }

            await _context.SaveChangesAsync();
            return "Grades locked successfully";
        }

        private async Task<int?> ResolveTermIdAsync(int academicYearId, int semester)
        {
            var terms = await _context.Terms
                .AsNoTracking()
                .Where(t => t.AcademicYearID == academicYearId)
                .OrderBy(t => t.TermID)
                .Select(t => t.TermID)
                .ToListAsync();

            if (!terms.Any())
            {
                return null;
            }

            if (semester <= 1)
            {
                return terms[0];
            }

            if (terms.Count > 1)
            {
                return terms[1];
            }

            return terms[0];
        }
    }
}

