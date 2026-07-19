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
    public class ViceFinalGradesService : IViceFinalGradesService
    {
        private readonly GradeDbContext _context;

        public ViceFinalGradesService(GradeDbContext context)
        {
            _context = context;
        }

        public async Task<ViceFinalStudentsTableResponseDto?> GetFinalStudentsTableAsync(string level, int semester, string department, int? classId, int subjectId)
        {
            if (!Enum.TryParse<EducationStage>(level, true, out var stage))
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

            var dept = await _context.Departments.FirstOrDefaultAsync(d => d.IsActive && d.DepartmentName == department);
            if (dept == null)
            {
                return null;
            }

            var termId = await ResolveTermIdAsync(academicYear.AcademicYearID, semester);
            if (termId == null)
            {
                return null;
            }

            var subject = await _context.Subjects.FirstOrDefaultAsync(s => s.SubjectID == subjectId && s.IsActive && s.AcademicYearID == academicYear.AcademicYearID);
            if (subject == null)
            {
                return null;
            }

            var studentsQuery = _context.Students
                .AsNoTracking()
                .Where(s => s.UserID.HasValue && s.CurrentAcademicYearID == academicYear.AcademicYearID && s.ClassID.HasValue);

            if (classId.HasValue)
            {
                studentsQuery = studentsQuery.Where(s => s.ClassID == classId.Value);
            }

            var students = await studentsQuery
                .Join(_context.Users,
                    s => s.UserID!.Value,
                    u => u.UserId,
                    (s, u) => new { Student = s, User = u })
                .Join(_context.Classes,
                    x => x.Student.ClassID!.Value,
                    c => c.ClassID,
                    (x, c) => new { x.Student, x.User, Class = c })
                .Where(x => x.Class.IsActive && x.Class.DepartmentID == dept.DepartmentID)
                .Select(x => new
                {
                    StudentID = x.Student.StudentID,
                    StudentName = x.User.FullName ?? string.Empty,
                    ClassName = x.Class.ClassName
                })
                .OrderBy(x => x.StudentID)
                .ToListAsync();

            var studentIds = students.Select(s => s.StudentID).ToList();
            if (!studentIds.Any())
            {
                return new ViceFinalStudentsTableResponseDto { Status = "draft", Students = new List<ViceFinalStudentRowDto>() };
            }

            var allResults = await _context.StudentAllResults
                .AsNoTracking()
                .Where(ar =>
                    ar.SubjectID == subjectId &&
                    ar.TermID == termId.Value &&
                    ar.AcademicYearID == academicYear.AcademicYearID &&
                    ar.StudentID.HasValue &&
                    studentIds.Contains(ar.StudentID.Value))
                .Include(ar => ar.ResultApproval)
                .ToListAsync();

            var resultByStudent = allResults.ToDictionary(ar => ar.StudentID!.Value, ar => ar);

            bool approvedExists = allResults.Any(ar => ar.ResultApproval != null && ar.ResultApproval.Decision == Decision.Approved);
            bool pendingExists = allResults.Any(ar => ar.ResultApproval != null && ar.ResultApproval.Decision == Decision.Pending);

            var status = approvedExists ? "approved" : (pendingExists ? "submitted" : "draft");

            var rows = students.Select(st =>
            {
                resultByStudent.TryGetValue(st.StudentID, out var row);
                var score = row?.FinalSubjectScore ?? 0m;
                return new ViceFinalStudentRowDto
                {
                    StudentId = st.StudentID.ToString(),
                    StudentName = st.StudentName,
                    ClassName = st.ClassName,
                    Score = score
                };
            }).ToList();

            return new ViceFinalStudentsTableResponseDto
            {
                Status = status,
                Students = rows
            };
        }

        public async Task<int> UpsertFinalGradesBulkAsync(ViceUpsertFinalGradesRequestDTO request)
        {
            if (request == null || request.Grades == null || !request.Grades.Any())
            {
                return 0;
            }

            if (!Enum.TryParse<EducationStage>(request.Level, true, out var stage))
            {
                throw new ArgumentException("Invalid level value. Expected: junior|wheeler|senior.");
            }

            var academicYear = await _context.AcademicYears
                .Where(a => a.IsActive && a.Stage == stage)
                .OrderByDescending(a => a.AcademicYearID)
                .FirstOrDefaultAsync();
            if (academicYear == null)
            {
                return 0;
            }

            var dept = await _context.Departments.FirstOrDefaultAsync(d => d.IsActive && d.DepartmentName == request.Department);
            if (dept == null)
            {
                return 0;
            }

            var termId = await ResolveTermIdAsync(academicYear.AcademicYearID, request.Semester);
            if (termId == null)
            {
                return 0;
            }

            var subject = await _context.Subjects.FirstOrDefaultAsync(s => s.SubjectID == request.SubjectId && s.IsActive && s.AcademicYearID == academicYear.AcademicYearID);
            if (subject == null)
            {
                throw new ArgumentException("The selected subject does not exist or does not belong to the selected academic year/stage.");
            }

            var cls = await _context.Classes
                .AsNoTracking()
                .FirstOrDefaultAsync(c =>
                    c.IsActive &&
                    c.ClassID == request.ClassId &&
                    c.AcademicYearID == academicYear.AcademicYearID &&
                    c.DepartmentID == dept.DepartmentID);
            if (cls == null)
            {
                throw new ArgumentException("The selected class does not belong to the selected academic year and department.");
            }

            var className = cls.ClassName;

            var now = DateTime.UtcNow;

            // If any student already approved, we reject this upsert entirely.
            var parsedStudentIds = new List<int>();
            foreach (var grade in request.Grades)
            {
                if (!int.TryParse(grade.StudentId, out var studentId) || studentId <= 0)
                {
                    throw new ArgumentException($"Invalid student id: {grade.StudentId}.");
                }

                if (grade.Score < 0 || grade.Score > (subject.MaxFinalScore ?? 100m))
                {
                    throw new ArgumentException($"Score for student {grade.StudentId} must be between 0 and {subject.MaxFinalScore ?? 100m}.");
                }

                parsedStudentIds.Add(studentId);
            }

            parsedStudentIds = parsedStudentIds.Distinct().ToList();
            if (parsedStudentIds.Count != request.Grades.Count)
            {
                throw new ArgumentException("Each student may appear only once in a final-grade submission.");
            }

            var validStudentIds = await _context.Students
                .AsNoTracking()
                .Where(student =>
                    parsedStudentIds.Contains(student.StudentID) &&
                    student.CurrentAcademicYearID == academicYear.AcademicYearID &&
                    student.DepartmentID == dept.DepartmentID &&
                    student.ClassID == request.ClassId)
                .Select(student => student.StudentID)
                .ToListAsync();
            if (validStudentIds.Count != parsedStudentIds.Count)
            {
                throw new ArgumentException("One or more students do not belong to the selected class, academic year, and department.");
            }
            var existingApproved = await _context.StudentAllResults
                .AsNoTracking()
                .Where(ar =>
                    ar.SubjectID == request.SubjectId &&
                    ar.TermID == termId.Value &&
                    ar.AcademicYearID == academicYear.AcademicYearID &&
                    ar.StudentID.HasValue &&
                    parsedStudentIds.Contains(ar.StudentID.Value))
                .Include(ar => ar.ResultApproval)
                .Where(ar => ar.ResultApproval != null && ar.ResultApproval.Decision == Decision.Approved)
                .AnyAsync();

            if (existingApproved)
            {
                return 0;
            }

            var existingRows = await _context.StudentAllResults
                .Where(ar =>
                    ar.SubjectID == request.SubjectId &&
                    ar.TermID == termId.Value &&
                    ar.AcademicYearID == academicYear.AcademicYearID &&
                    ar.StudentID.HasValue &&
                    parsedStudentIds.Contains(ar.StudentID.Value))
                .ToListAsync();

            var dict = existingRows.ToDictionary(ar => ar.StudentID!.Value, ar => ar);

            foreach (var grade in request.Grades)
            {
                var sid = int.Parse(grade.StudentId);
                dict.TryGetValue(sid, out var row);

                var before = row?.FinalSubjectScore;
                var after = grade.Score;

                if (row == null)
                {
                    row = new StudentAllResults
                    {
                        StudentID = sid,
                        SubjectID = request.SubjectId,
                        TermID = termId.Value,
                        AcademicYearID = academicYear.AcademicYearID,
                        GeneratedAt = now
                    };
                    _context.StudentAllResults.Add(row);
                    dict[sid] = row;
                }

                row.FinalSubjectScore = after;
                row.TotalTermScore = after;

                var maxFinal = subject.MaxFinalScore ?? 100m;
                var passThreshold = maxFinal / 2m;
                row.SubjectStatus = after >= passThreshold ? SubjectStatus.Passed : SubjectStatus.Failed;
                row.OverallTermStatus = after >= passThreshold ? OverallTermStatus.Passed : OverallTermStatus.Failed;

                _context.GradeActionLogs.Add(new GradeActionLog
                {
                    Action = "Updated final grades",
                    ActorUserID = null,
                    ActorName = "Vice",
                    StudentID = sid,
                    SubjectID = request.SubjectId,
                    AcademicYearID = academicYear.AcademicYearID,
                    DepartmentID = dept.DepartmentID,
                    ClassID = request.ClassId,
                    TermID = termId.Value,
                    Level = stage.ToString().ToLowerInvariant(),
                    SubjectName = subject.SubjectName,
                    ClassName = className,
                    BeforeFinalScore = before,
                    AfterFinalScore = after,
                    Timestamp = now
                });
            }

            await _context.SaveChangesAsync();
            return request.Grades.Count;
        }

        public async Task<bool> SubmitFinalGradesAsync(ViceSubmitFinalGradesRequestDTO request)
        {
            if (request == null)
            {
                return false;
            }

            if (!Enum.TryParse<EducationStage>(request.Level, true, out var stage))
            {
                return false;
            }

            var academicYear = await _context.AcademicYears
                .Where(a => a.IsActive && a.Stage == stage)
                .OrderByDescending(a => a.AcademicYearID)
                .FirstOrDefaultAsync();
            if (academicYear == null)
            {
                return false;
            }

            var dept = await _context.Departments.FirstOrDefaultAsync(d => d.IsActive && d.DepartmentName == request.Department);
            if (dept == null)
            {
                return false;
            }

            var termId = await ResolveTermIdAsync(academicYear.AcademicYearID, request.Semester);
            if (termId == null)
            {
                return false;
            }

            var subjectExists = await _context.Subjects
                .AnyAsync(s => s.SubjectID == request.SubjectId && s.IsActive && s.AcademicYearID == academicYear.AcademicYearID);
            if (!subjectExists)
            {
                return false;
            }

            IQueryable<StudentAllResults> query = _context.StudentAllResults
                .Where(ar =>
                    ar.SubjectID == request.SubjectId &&
                    ar.TermID == termId.Value &&
                    ar.AcademicYearID == academicYear.AcademicYearID &&
                    ar.StudentID.HasValue)
                .Include(ar => ar.ResultApproval)
                .Include(ar => ar.Subject)
                .Include(ar => ar.Student)
                    .ThenInclude(s => s.Class);

            if (request.ClassId.HasValue)
            {
                var classIdValue = request.ClassId.Value;
                query = query.Where(ar => ar.Student != null && ar.Student.ClassID == classIdValue);
            }

            var allResults = await query.ToListAsync();

            var now = DateTime.UtcNow;
            foreach (var row in allResults)
            {
                if (row.StudentID == null)
                {
                    continue;
                }

                var sid = row.StudentID.Value;
                if (row.ResultApproval != null && row.ResultApproval.Decision == Decision.Approved)
                {
                    continue;
                }

                if (row.ResultApproval == null)
                {
                    row.ResultApproval = new ResultApproval
                    {
                        AllResultID = row.AllResultID,
                        Decision = Decision.Pending,
                        Notes = ""
                    };
                }
                else
                {
                    row.ResultApproval.Decision = Decision.Pending;
                }

                var logClassId = request.ClassId ?? row.Student?.ClassID;
                var logClassName = row.Student?.Class?.ClassName ?? logClassId?.ToString();

                _context.GradeActionLogs.Add(new GradeActionLog
                {
                    Action = "Submitted final grades",
                    ActorUserID = null,
                    ActorName = "Vice",
                    StudentID = sid,
                    SubjectID = request.SubjectId,
                    AcademicYearID = academicYear.AcademicYearID,
                    DepartmentID = dept.DepartmentID,
                    ClassID = logClassId,
                    TermID = termId.Value,
                    Level = stage.ToString().ToLowerInvariant(),
                    SubjectName = row.Subject?.SubjectName,
                    ClassName = logClassName,
                    Timestamp = now
                });
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<ViceFinalGradeHistoryItemDto>> GetFinalHistoryAsync(string studentId, int subjectId)
        {
            if (!int.TryParse(studentId, out var sid))
            {
                return new List<ViceFinalGradeHistoryItemDto>();
            }

            var logs = await _context.GradeActionLogs
                .AsNoTracking()
                .Where(l => l.StudentID == sid && l.SubjectID == subjectId)
                .OrderByDescending(l => l.Timestamp)
                .Take(50)
                .ToListAsync();

            return logs.Select(l => new ViceFinalGradeHistoryItemDto
            {
                Id = l.ActionLogID,
                Action = l.Action ?? string.Empty,
                TeacherName = l.ActorName,
                SubjectName = l.SubjectName,
                ClassName = l.ClassName,
                Level = l.Level,
                Timestamp = l.Timestamp,
                BeforeScore = l.BeforeFinalScore,
                AfterScore = l.AfterFinalScore
            }).ToList();
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
