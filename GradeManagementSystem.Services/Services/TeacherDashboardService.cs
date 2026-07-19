using GradeManagementSystem.Core.DTOs.Class;
using GradeManagementSystem.Core.DTOs.Teacher;
using GradeManagementSystem.Core.Entities.Domain;
using GradeManagementSystem.Core.Entities.Enums;
using GradeManagementSystem.Core.Interfaces;
using GradeManagementSystem.Repository.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GradeManagementSystem.Services.Services
{
    public class TeacherDashboardService : ITeacherDashboardService
    {
        private readonly GradeDbContext _context;

        public TeacherDashboardService(GradeDbContext context)
        {
            _context = context;
        }

        public async Task<TeacherProfileDto?> GetProfileAsync(int userId)
        {
            var teacher = await _context.Teachers
                .FirstOrDefaultAsync(t => t.UserID == userId && t.IsActive);

            if (teacher == null)
            {
                return null;
            }

            var currentAcademicYear = await _context.TeacherAssignments
                .Where(ta => ta.TeacherID == teacher.TeacherID && ta.IsActive && ta.AcademicYear.IsActive)
                .OrderByDescending(ta => ta.AcademicYearID)
                .Select(ta => ta.AcademicYear)
                .FirstOrDefaultAsync();

            if (currentAcademicYear == null)
            {
                return null;
            }

            var firstSubject = await _context.TeacherAssignments
                .Where(ta => ta.TeacherID == teacher.TeacherID
                             && ta.IsActive
                             && ta.AcademicYearID == currentAcademicYear.AcademicYearID
                             && ta.Subject.IsActive)
                .OrderBy(ta => ta.SubjectID)
                .Select(ta => ta.Subject.SubjectName)
                .FirstOrDefaultAsync();

            var subtitle = !string.IsNullOrWhiteSpace(firstSubject)
                ? firstSubject
                : "Teacher";

            var user = await _context.Users
                .Where(u => u.UserId == userId)
                .Select(u => new { u.FullName })
                .FirstOrDefaultAsync();

            return new TeacherProfileDto
            {
                Name = user?.FullName ?? "Teacher",
                Subtitle = subtitle,
                CurrentAcademicYear = currentAcademicYear.YearName
            };
        }

        public async Task<List<TeacherSubjectYearGroupDto>> GetSubjectsAsync(int userId)
        {
            var teacher = await _context.Teachers
                .FirstOrDefaultAsync(t => t.UserID == userId && t.IsActive);

            if (teacher == null)
            {
                return new List<TeacherSubjectYearGroupDto>();
            }

            var rows = await _context.TeacherAssignments
                .Where(ta => ta.TeacherID == teacher.TeacherID
                             && ta.IsActive
                             && ta.AcademicYear.IsActive
                             && ta.Subject.IsActive)
                .Select(ta => new
                {
                    YearName = ta.AcademicYear.YearName,
                    Stage = ta.AcademicYear.Stage,
                    SubjectId = ta.SubjectID!.Value,
                    SubjectName = ta.Subject.SubjectName
                })
                .ToListAsync();

            return rows
                .GroupBy(x => new { x.YearName, x.Stage })
                .Select(g => new TeacherSubjectYearGroupDto
                {
                    Year = g.Key.YearName,
                    Stage = g.Key.Stage.ToString().ToLowerInvariant(),
                    Subjects = g
                        .OrderBy(s => s.SubjectId)
                        .Select(s => new TeacherSubjectDto { Id = s.SubjectId, SubjectName = s.SubjectName })
                        .ToList()
                })
                .OrderByDescending(gr => gr.Year)
                .ToList();
        }

        public async Task<List<ClassResponseDTO>?> GetClassesAsync(int userId, string year, string subject)
        {
            var teacher = await _context.Teachers
                .FirstOrDefaultAsync(t => t.UserID == userId && t.IsActive);

            if (teacher == null)
            {
                return null;
            }

            var academicYear = await _context.AcademicYears
                .Where(ay => ay.IsActive && ay.YearName == year)
                .OrderByDescending(ay => ay.AcademicYearID)
                .FirstOrDefaultAsync();

            if (academicYear == null && Enum.TryParse<EducationStage>(year, true, out var stage))
            {
                academicYear = await _context.AcademicYears
                    .Where(ay => ay.IsActive && ay.Stage == stage)
                    .OrderByDescending(ay => ay.AcademicYearID)
                    .FirstOrDefaultAsync();
            }

            if (academicYear == null)
            {
                return null;
            }

            int? subjectId = null;
            if (int.TryParse(subject, out var parsedSubjectId))
            {
                subjectId = await _context.Subjects
                    .Where(s => s.IsActive
                                && s.SubjectID == parsedSubjectId)
                    .Select(s => (int?)s.SubjectID)
                    .FirstOrDefaultAsync();
            }
            else
            {
                subjectId = await _context.Subjects
                    .Where(s => s.IsActive
                                && s.SubjectName == subject)
                    .Select(s => (int?)s.SubjectID)
                    .FirstOrDefaultAsync();
            }

            if (!subjectId.HasValue)
            {
                return null;
            }

            var classes = await _context.TeacherAssignments
                .Where(ta => ta.TeacherID == teacher.TeacherID
                             && ta.IsActive
                             && ta.AcademicYearID == academicYear.AcademicYearID
                             && ta.SubjectID == subjectId.Value
                             && ta.ClassID.HasValue)
                .Select(ta => new
                {
                    ClassId = ta.Class.ClassID,
                    ClassName = ta.Class.ClassName
                })
                .Distinct()
                .OrderBy(c => c.ClassName)
                .ToListAsync();

            return classes.Select(c => new ClassResponseDTO { ClassId = c.ClassId, ClassName = c.ClassName }).ToList();
        }

        public async Task<List<TeacherStudentGradeDto>?> GetStudentsAsync(int userId, int classId, int subjectId)
        {
            var teacher = await _context.Teachers.FirstOrDefaultAsync(t => t.UserID == userId && t.IsActive);
            if (teacher == null) return null;

            var assignment = await _context.TeacherAssignments
                .Where(ta => ta.TeacherID == teacher.TeacherID && ta.IsActive && ta.ClassID == classId &&
                             ta.SubjectID == subjectId && ta.AcademicYearID.HasValue && ta.AcademicYear.IsActive && ta.Subject.IsActive)
                .Select(ta => new { AcademicYearId = ta.AcademicYearID!.Value })
                .FirstOrDefaultAsync();
            if (assignment == null) return new List<TeacherStudentGradeDto>();

            var subject = await _context.Subjects
                .Where(s => s.SubjectID == subjectId && s.IsActive)
                .Select(s => new { s.SubjectName, s.MaxQuarterQ1Score, s.MaxQuarterQ2Score, s.MaxQuarterQ3Score, s.MaxQuarterQ4Score, s.MaxQuarterScore })
                .FirstOrDefaultAsync();
            if (subject == null) return new List<TeacherStudentGradeDto>();

            var termIds = await _context.Terms.Where(t => t.AcademicYearID == assignment.AcademicYearId)
                .OrderBy(t => t.TermID).Select(t => t.TermID).ToListAsync();
            if (termIds.Count == 0) return null;
            var firstTermId = termIds[0];
            var secondTermId = termIds.Count > 1 ? termIds[1] : firstTermId;

            var students = await _context.Students
                .Where(s => s.ClassID == classId && s.CurrentAcademicYearID == assignment.AcademicYearId && s.UserID.HasValue)
                .Join(_context.Users, s => s.UserID!.Value, u => u.UserId, (s, u) => new { StudentId = s.StudentID, StudentName = u.FullName })
                .OrderBy(s => s.StudentName).ToListAsync();
            var studentIds = students.Select(s => s.StudentId).ToList();
            var results = await _context.StudentSubjectTermResults
                .Where(r => r.SubjectID == subjectId && r.AcademicYearID == assignment.AcademicYearId && r.StudentID.HasValue &&
                            studentIds.Contains(r.StudentID.Value) && (r.TermID == firstTermId || r.TermID == secondTermId))
                .ToListAsync();

            var allResults = await _context.StudentAllResults
                .Where(r => r.SubjectID == subjectId && r.AcademicYearID == assignment.AcademicYearId && r.StudentID.HasValue &&
                            studentIds.Contains(r.StudentID.Value) && (r.TermID == firstTermId || r.TermID == secondTermId))
                .ToListAsync();

            return students.Select(student =>
            {
                var first = results.FirstOrDefault(r => r.StudentID == student.StudentId && r.TermID == firstTermId);
                var second = results.FirstOrDefault(r => r.StudentID == student.StudentId && r.TermID == secondTermId);
                var firstAll = allResults.FirstOrDefault(r => r.StudentID == student.StudentId && r.TermID == firstTermId);
                var secondAll = allResults.FirstOrDefault(r => r.StudentID == student.StudentId && r.TermID == secondTermId);
                return new TeacherStudentGradeDto
                {
                    StudentId = student.StudentId,
                    StudentName = student.StudentName,
                    SubjectId = subjectId,
                    SubjectName = subject.SubjectName,
                    Q1 = first?.Quarter1Score,
                    Q2 = first?.Quarter2Score,
                    Q3 = second?.Quarter3Score,
                    Q4 = second?.Quarter4Score,
                    FinalGrade = secondAll?.FinalSubjectScore ?? firstAll?.FinalSubjectScore,
                    MaxQ1 = subject.MaxQuarterQ1Score ?? subject.MaxQuarterScore,
                    MaxQ2 = subject.MaxQuarterQ2Score ?? subject.MaxQuarterScore,
                    MaxQ3 = subject.MaxQuarterQ3Score ?? subject.MaxQuarterScore,
                    MaxQ4 = subject.MaxQuarterQ4Score ?? subject.MaxQuarterScore,
                    Status = (second?.Status ?? first?.Status ?? SubjectStatus.InProgress).ToString()
                };
            }).ToList();
        }

        public async Task<TeacherGradeUpdateResponseDto?> UpsertGradeAsync(int userId, TeacherGradeUpdateRequestDTO request)
        {
            var teacher = await _context.Teachers.FirstOrDefaultAsync(t => t.UserID == userId && t.IsActive);
            if (teacher == null) return null;

            var assignment = await _context.TeacherAssignments
                .Where(ta => ta.TeacherID == teacher.TeacherID && ta.IsActive && ta.ClassID == request.ClassId &&
                             ta.SubjectID == request.SubjectId && ta.AcademicYearID.HasValue && ta.AcademicYear.IsActive && ta.Subject.IsActive)
                .Select(ta => new { AcademicYearId = ta.AcademicYearID!.Value, ClassName = ta.Class.ClassName, DepartmentId = ta.Class.DepartmentID, Stage = ta.AcademicYear.Stage })
                .FirstOrDefaultAsync();
            if (assignment == null) return null;

            var studentExists = await _context.Students.AnyAsync(s => s.StudentID == request.StudentId && s.ClassID == request.ClassId && s.CurrentAcademicYearID == assignment.AcademicYearId);
            if (!studentExists) throw new ArgumentException("The student is not enrolled in this class for the selected academic year.");

            var subject = await _context.Subjects
                .Where(s => s.SubjectID == request.SubjectId && s.IsActive)
                .Select(s => new { s.SubjectName, s.MaxQuarterQ1Score, s.MaxQuarterQ2Score, s.MaxQuarterQ3Score, s.MaxQuarterQ4Score, s.MaxQuarterScore })
                .FirstOrDefaultAsync();
            if (subject == null) return null;

            ValidateQuarter(request.Q1, subject.MaxQuarterQ1Score ?? subject.MaxQuarterScore, "Q1");
            ValidateQuarter(request.Q2, subject.MaxQuarterQ2Score ?? subject.MaxQuarterScore, "Q2");
            ValidateQuarter(request.Q3, subject.MaxQuarterQ3Score ?? subject.MaxQuarterScore, "Q3");
            ValidateQuarter(request.Q4, subject.MaxQuarterQ4Score ?? subject.MaxQuarterScore, "Q4");

            if (assignment.DepartmentId.HasValue && await _context.QuarterGradesLocks.AnyAsync(l =>
                l.AcademicYearID == assignment.AcademicYearId && l.SubjectID == request.SubjectId &&
                l.DepartmentID == assignment.DepartmentId.Value && l.ClassID == request.ClassId))
            {
                throw new InvalidOperationException("Quarter grades for this class and subject are locked.");
            }

            var termIds = await _context.Terms.Where(t => t.AcademicYearID == assignment.AcademicYearId)
                .OrderBy(t => t.TermID).Select(t => t.TermID).ToListAsync();
            if (termIds.Count == 0) throw new InvalidOperationException("No terms are configured for this academic year.");
            var firstTermId = termIds[0];
            var secondTermId = termIds.Count > 1 ? termIds[1] : firstTermId;
            var now = DateTime.UtcNow;

            var first = await GetOrCreateResultAsync(request.StudentId, request.SubjectId, assignment.AcademicYearId, firstTermId, now);
            if (request.Q1.HasValue) first.Quarter1Score = request.Q1;
            if (request.Q2.HasValue) first.Quarter2Score = request.Q2;
            first.TermTotal = (first.Quarter1Score ?? 0m) + (first.Quarter2Score ?? 0m);
            first.Status = SubjectStatus.InProgress;
            first.LastUpdatedAt = now;

            var second = firstTermId == secondTermId ? first : await GetOrCreateResultAsync(request.StudentId, request.SubjectId, assignment.AcademicYearId, secondTermId, now);
            if (request.Q3.HasValue) second.Quarter3Score = request.Q3;
            if (request.Q4.HasValue) second.Quarter4Score = request.Q4;
            second.TermTotal = firstTermId == secondTermId
                ? (second.Quarter1Score ?? 0m) + (second.Quarter2Score ?? 0m) + (second.Quarter3Score ?? 0m) + (second.Quarter4Score ?? 0m)
                : (second.Quarter3Score ?? 0m) + (second.Quarter4Score ?? 0m);
            second.Status = SubjectStatus.InProgress;
            second.LastUpdatedAt = now;

            await EnsureQuarterSubmissionAsync(request.StudentId, request.SubjectId, assignment.AcademicYearId, firstTermId, userId, now);
            if (secondTermId != firstTermId) await EnsureQuarterSubmissionAsync(request.StudentId, request.SubjectId, assignment.AcademicYearId, secondTermId, userId, now);

            var teacherName = await _context.Users.Where(u => u.UserId == userId).Select(u => u.FullName).FirstOrDefaultAsync();
            _context.GradeActionLogs.Add(new GradeActionLog
            {
                Action = "Updated quarter grades", ActorUserID = userId, ActorName = teacherName,
                StudentID = request.StudentId, SubjectID = request.SubjectId, AcademicYearID = assignment.AcademicYearId,
                DepartmentID = assignment.DepartmentId, ClassID = request.ClassId, TermID = firstTermId,
                Level = assignment.Stage.ToString().ToLowerInvariant(), SubjectName = subject.SubjectName,
                ClassName = assignment.ClassName, Timestamp = now
            });
            await _context.SaveChangesAsync();

            return new TeacherGradeUpdateResponseDto { ClassId = request.ClassId, StudentId = request.StudentId, SubjectId = request.SubjectId, Q1 = first.Quarter1Score, Q2 = first.Quarter2Score, Q3 = second.Quarter3Score, Q4 = second.Quarter4Score, Status = SubjectStatus.InProgress.ToString() };
        }

        private async Task<StudentSubjectTermResult> GetOrCreateResultAsync(int studentId, int subjectId, int academicYearId, int termId, DateTime now)
        {
            var result = await _context.StudentSubjectTermResults.FirstOrDefaultAsync(r => r.StudentID == studentId && r.SubjectID == subjectId && r.AcademicYearID == academicYearId && r.TermID == termId);
            if (result != null) return result;
            result = new StudentSubjectTermResult { StudentID = studentId, SubjectID = subjectId, AcademicYearID = academicYearId, TermID = termId, Status = SubjectStatus.InProgress, CreatedAt = now };
            _context.StudentSubjectTermResults.Add(result);
            return result;
        }

        private async Task EnsureQuarterSubmissionAsync(int studentId, int subjectId, int academicYearId, int termId, int userId, DateTime now)
        {
            var submission = await _context.QuarterGradeSubmissions.FirstOrDefaultAsync(s => s.StudentID == studentId && s.SubjectID == subjectId && s.AcademicYearID == academicYearId && s.TermID == termId);
            if (submission == null)
            {
                _context.QuarterGradeSubmissions.Add(new QuarterGradeSubmission { StudentID = studentId, SubjectID = subjectId, AcademicYearID = academicYearId, TermID = termId, SubmittedAt = now, SubmittedBy = userId });
                return;
            }
            submission.SubmittedAt = now;
            submission.SubmittedBy = userId;
        }

        private static void ValidateQuarter(decimal? score, int? maximum, string quarter)
        {
            if (!score.HasValue || !maximum.HasValue || maximum.Value <= 0) return;
            if (score.Value > maximum.Value) throw new ArgumentException($"{quarter} cannot exceed the configured maximum of {maximum.Value}.");
        }
    }
}
