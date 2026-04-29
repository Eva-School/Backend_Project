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
    public class ViceQuarterGradesService : IViceQuarterGradesService
    {
        private readonly GradeDbContext _context;

        public ViceQuarterGradesService(GradeDbContext context)
        {
            _context = context;
        }

        public async Task<ViceQuarterMaxGradesDto?> SetSubjectQuarterMaxGradesAsync(int subjectId, ViceSetQuarterMaxGradesRequestDTO request)
        {
            if (request == null)
            {
                return null;
            }

            var subject = await _context.Subjects.FirstOrDefaultAsync(s => s.SubjectID == subjectId);
            if (subject == null)
            {
                return null;
            }

            subject.MaxQuarterQ1Score = request.MaxQuarterGrades.Q1;
            subject.MaxQuarterQ2Score = request.MaxQuarterGrades.Q2;
            subject.MaxQuarterQ3Score = request.MaxQuarterGrades.Q3;
            subject.MaxQuarterQ4Score = request.MaxQuarterGrades.Q4;

            // Keep legacy MaxQuarterScore consistent with term1 quarters (Q1+Q2).
            subject.MaxQuarterScore = request.MaxQuarterGrades.Q1 + request.MaxQuarterGrades.Q2;

            await _context.SaveChangesAsync();

            return request.MaxQuarterGrades;
        }

        public async Task<ViceQuarterStudentsSheetResponseDto?> GetQuarterStudentsSheetAsync(string level, int subjectId, string department, int? classId)
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

            var termIds = await GetTermIdsAsync(academicYear.AcademicYearID);
            if (termIds.Term1Id == null)
            {
                return null;
            }

            var subject = await _context.Subjects
                .FirstOrDefaultAsync(s => s.SubjectID == subjectId && s.IsActive);

            if (subject == null)
            {
                return null;
            }

            var classesQuery = _context.Classes
                .AsNoTracking()
                .Where(c => c.IsActive && c.AcademicYearID == academicYear.AcademicYearID && c.DepartmentID == dept.DepartmentID);

            if (classId.HasValue)
            {
                classesQuery = classesQuery.Where(c => c.ClassID == classId.Value);
            }

            var classIds = await classesQuery.Select(c => c.ClassID).ToListAsync();
            if (!classIds.Any())
            {
                return new ViceQuarterStudentsSheetResponseDto
                {
                    Status = "draft",
                    MaxQuarterGrades = new ViceQuarterMaxGradesDto(),
                    Students = new List<ViceQuarterStudentSheetRowDto>()
                };
            }

            // Status = locked if QuarterGradesLocks exists for these filters.
            var isLocked = await _context.QuarterGradesLocks
                .AsNoTracking()
                .Where(l => l.AcademicYearID == academicYear.AcademicYearID)
                .Where(l => l.SubjectID == subjectId)
                .Where(l => l.DepartmentID == dept.DepartmentID)
                .Where(l => classIds.Contains(l.ClassID))
                .AnyAsync();

            var maxGrades = new ViceQuarterMaxGradesDto
            {
                Q1 = subject.MaxQuarterQ1Score ?? 0,
                Q2 = subject.MaxQuarterQ2Score ?? 0,
                Q3 = subject.MaxQuarterQ3Score ?? 0,
                Q4 = subject.MaxQuarterQ4Score ?? 0
            };

            // Backward compatibility: if Q1/Q2 not set, derive them from MaxQuarterScore (legacy).
            var quarterMax = subject.MaxQuarterScore ?? 0;
            if (subject.MaxQuarterQ1Score == null && subject.MaxQuarterQ2Score == null && quarterMax > 0)
            {
                var quarter1 = decimal.Round(quarterMax * 0.48m, 2, MidpointRounding.AwayFromZero);
                var quarter2 = decimal.Round(quarterMax - quarter1, 2, MidpointRounding.AwayFromZero);

                maxGrades.Q1 = (int)quarter1;
                maxGrades.Q2 = (int)quarter2;

                // Assume term2 distribution matches term1 if not set.
                maxGrades.Q3 = subject.MaxQuarterQ3Score ?? maxGrades.Q1;
                maxGrades.Q4 = subject.MaxQuarterQ4Score ?? maxGrades.Q2;
            }

            // Pull all students for the selected classes.
            var students = await _context.Students
                .AsNoTracking()
                .Where(s => s.UserID.HasValue && s.CurrentAcademicYearID == academicYear.AcademicYearID && s.ClassID.HasValue && classIds.Contains(s.ClassID!.Value))
                .Join(_context.Users,
                    s => s.UserID!.Value,
                    u => u.UserId,
                    (s, u) => new { Student = s, User = u })
                .Select(x => new
                {
                    x.Student.StudentID,
                    x.User.FullName
                })
                .ToListAsync();

            var studentIds = students.Select(s => s.StudentID).ToList();

            var term1 = termIds.Term1Id!.Value;
            var term2 = termIds.Term2Id ?? term1;

            var results = await _context.StudentSubjectTermResults
                .AsNoTracking()
                .Where(r =>
                    r.SubjectID == subjectId &&
                    r.AcademicYearID == academicYear.AcademicYearID &&
                    r.StudentID.HasValue &&
                    studentIds.Contains(r.StudentID.Value) &&
                    (r.TermID == term1 || r.TermID == term2))
                .ToListAsync();

            var byStudentTerm = results
                .GroupBy(r => new { r.StudentID, r.TermID })
                .ToDictionary(g => (g.Key.StudentID!.Value, g.Key.TermID!.Value), g => g.First());

            var sheetStudents = new List<ViceQuarterStudentSheetRowDto>();
            foreach (var st in students)
            {
                var r1 = byStudentTerm.TryGetValue((st.StudentID, term1), out var a) ? a : null;
                var r2 = byStudentTerm.TryGetValue((st.StudentID, term2), out var b) ? b : null;

                sheetStudents.Add(new ViceQuarterStudentSheetRowDto
                {
                    StudentId = st.StudentID.ToString(),
                    StudentName = st.FullName ?? string.Empty,
                    Q1 = (r1?.Quarter1Score ?? 0),
                    Q2 = (r1?.Quarter2Score ?? 0),
                    Q3 = (r2?.Quarter3Score ?? 0),
                    Q4 = (r2?.Quarter4Score ?? 0)
                });
            }

            return new ViceQuarterStudentsSheetResponseDto
            {
                Status = isLocked ? "locked" : "draft",
                MaxQuarterGrades = maxGrades,
                Students = sheetStudents
            };
        }

        public async Task<int> UpsertQuarterGradesBulkAsync(ViceUpsertQuarterGradesRequestDTO request)
        {
            if (request == null || request.Students == null || !request.Students.Any())
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

            var termIds = await GetTermIdsAsync(academicYear.AcademicYearID);
            if (termIds.Term1Id == null)
            {
                return 0;
            }

            var subject = await _context.Subjects.FirstOrDefaultAsync(s => s.SubjectID == request.SubjectId && s.IsActive);
            if (subject == null)
            {
                return 0;
            }

            // Reject if locked.
            var isLocked = await _context.QuarterGradesLocks.AnyAsync(l =>
                l.AcademicYearID == academicYear.AcademicYearID &&
                l.SubjectID == request.SubjectId &&
                l.DepartmentID == dept.DepartmentID &&
                l.ClassID == request.ClassId);

            if (isLocked)
            {
                return 0;
            }

            var term1 = termIds.Term1Id!.Value;
            var term2 = termIds.Term2Id ?? term1;

            var parsedStudentIds = new List<int>();
            foreach (var row in request.Students)
            {
                if (!int.TryParse(row.StudentId, out var sid))
                {
                    throw new ArgumentException($"Invalid studentId: {row.StudentId}");
                }
                parsedStudentIds.Add(sid);
            }

            parsedStudentIds = parsedStudentIds.Distinct().ToList();

            // Load existing quarter results for both terms.
            var existing = await _context.StudentSubjectTermResults
                .Where(r =>
                    r.SubjectID == request.SubjectId &&
                    r.AcademicYearID == academicYear.AcademicYearID &&
                    r.StudentID.HasValue &&
                    parsedStudentIds.Contains(r.StudentID.Value) &&
                    (r.TermID == term1 || r.TermID == term2))
                .ToListAsync();

            var dict = existing.ToDictionary(r => (r.StudentID!.Value, r.TermID!.Value), r => r);

            var now = DateTime.UtcNow;
            foreach (var row in request.Students)
            {
                var sid = int.Parse(row.StudentId);

                var key1 = (sid, term1);
                if (!dict.TryGetValue(key1, out var res1))
                {
                    res1 = new StudentSubjectTermResult
                    {
                        StudentID = sid,
                        SubjectID = request.SubjectId,
                        TermID = term1,
                        AcademicYearID = academicYear.AcademicYearID,
                        Status = SubjectStatus.InProgress,
                        CreatedAt = now
                    };
                    _context.StudentSubjectTermResults.Add(res1);
                    dict[key1] = res1;
                }

                res1.Quarter1Score = row.Q1;
                res1.Quarter2Score = row.Q2;
                res1.TermTotal = (row.Q1 + row.Q2);
                res1.LastUpdatedAt = now;
                res1.Status = SubjectStatus.InProgress;

                var key2 = (sid, term2);
                if (!dict.TryGetValue(key2, out var res2))
                {
                    res2 = new StudentSubjectTermResult
                    {
                        StudentID = sid,
                        SubjectID = request.SubjectId,
                        TermID = term2,
                        AcademicYearID = academicYear.AcademicYearID,
                        Status = SubjectStatus.InProgress,
                        CreatedAt = now
                    };
                    _context.StudentSubjectTermResults.Add(res2);
                    dict[key2] = res2;
                }

                res2.Quarter3Score = row.Q3;
                res2.Quarter4Score = row.Q4;
                res2.TermTotal = (row.Q3 + row.Q4);
                res2.LastUpdatedAt = now;
                res2.Status = SubjectStatus.InProgress;

                // Quarter submission records to help the dashboard.
                await EnsureQuarterSubmissionAsync(sid, request.SubjectId, academicYear.AcademicYearID, term1, null);
                await EnsureQuarterSubmissionAsync(sid, request.SubjectId, academicYear.AcademicYearID, term2, null);

                _context.GradeActionLogs.Add(new GradeActionLog
                {
                    Action = "Updated quarter grades",
                    ActorUserID = null,
                    ActorName = "Vice",
                    StudentID = sid,
                    SubjectID = request.SubjectId,
                    AcademicYearID = academicYear.AcademicYearID,
                    DepartmentID = dept.DepartmentID,
                    ClassID = request.ClassId,
                    TermID = term1,
                    Level = stage.ToString().ToLowerInvariant(),
                    SubjectName = subject.SubjectName,
                    ClassName = request.ClassId.ToString(),
                    Timestamp = now
                });
            }

            await _context.SaveChangesAsync();
            return request.Students.Count;
        }

        private async Task<(int? Term1Id, int? Term2Id)> GetTermIdsAsync(int academicYearId)
        {
            var terms = await _context.Terms
                .AsNoTracking()
                .Where(t => t.AcademicYearID == academicYearId)
                .OrderBy(t => t.TermID)
                .Select(t => t.TermID)
                .ToListAsync();

            int? term1 = terms.Count > 0 ? terms[0] : null;
            int? term2 = terms.Count > 1 ? terms[1] : null;
            return (term1, term2);
        }

        private async Task EnsureQuarterSubmissionAsync(int studentId, int subjectId, int academicYearId, int termId, int? submittedBy)
        {
            var existing = await _context.QuarterGradeSubmissions
                .FirstOrDefaultAsync(s =>
                    s.StudentID == studentId &&
                    s.SubjectID == subjectId &&
                    s.AcademicYearID == academicYearId &&
                    s.TermID == termId);

            if (existing != null)
            {
                existing.SubmittedAt = DateTime.UtcNow;
                existing.SubmittedBy = submittedBy;
                return;
            }

            _context.QuarterGradeSubmissions.Add(new QuarterGradeSubmission
            {
                StudentID = studentId,
                SubjectID = subjectId,
                AcademicYearID = academicYearId,
                TermID = termId,
                SubmittedAt = DateTime.UtcNow,
                SubmittedBy = submittedBy
            });
        }
    }
}

