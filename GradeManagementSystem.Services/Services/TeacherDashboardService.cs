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

            var stageLabel = currentAcademicYear.Stage.ToString().ToLowerInvariant();

            var firstSubject = await _context.TeacherAssignments
                .Where(ta => ta.TeacherID == teacher.TeacherID
                             && ta.IsActive
                             && ta.AcademicYearID == currentAcademicYear.AcademicYearID
                             && ta.Subject.IsActive)
                .OrderBy(ta => ta.SubjectID)
                .Select(ta => ta.Subject.SubjectName)
                .FirstOrDefaultAsync();

            var subtitle = !string.IsNullOrWhiteSpace(firstSubject)
                ? $"{firstSubject} Teacher"
                : "Teacher";

            var user = await _context.Users
                .Where(u => u.UserId == userId)
                .Select(u => new { u.FullName })
                .FirstOrDefaultAsync();

            return new TeacherProfileDto
            {
                Name = user?.FullName ?? "Teacher",
                Subtitle = subtitle,
                CurrentAcademicYear = stageLabel
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
                    Stage = ta.AcademicYear.Stage,
                    SubjectId = ta.SubjectID!.Value,
                    SubjectName = ta.Subject.SubjectName
                })
                .ToListAsync();

            return rows
                .GroupBy(x => x.Stage.ToString().ToLowerInvariant())
                .Select(g => new TeacherSubjectYearGroupDto
                {
                    Year = g.Key,
                    Subjects = g
                        .OrderBy(s => s.SubjectId)
                        .Select(s => new TeacherSubjectDto { Id = s.SubjectId, SubjectName = s.SubjectName })
                        .ToList()
                })
                .OrderBy(gr => gr.Year)
                .ToList();
        }

        public async Task<List<ClassResponseDTO>?> GetClassesAsync(int userId, string year, string subject)
        {
            if (!Enum.TryParse<EducationStage>(year, true, out var stage))
            {
                throw new ArgumentException("Invalid year value. Expected: junior|wheeler|senior.");
            }

            var teacher = await _context.Teachers
                .FirstOrDefaultAsync(t => t.UserID == userId && t.IsActive);

            if (teacher == null)
            {
                return null;
            }

            var academicYear = await _context.AcademicYears
                .Where(ay => ay.IsActive && ay.Stage == stage)
                .OrderByDescending(ay => ay.AcademicYearID)
                .FirstOrDefaultAsync();

            if (academicYear == null)
            {
                return null;
            }

            int? subjectId = null;
            if (int.TryParse(subject, out var parsedSubjectId))
            {
                subjectId = await _context.Subjects
                    .Where(s => s.IsActive
                                && s.AcademicYearID == academicYear.AcademicYearID
                                && s.SubjectID == parsedSubjectId)
                    .Select(s => (int?)s.SubjectID)
                    .FirstOrDefaultAsync();
            }
            else
            {
                subjectId = await _context.Subjects
                    .Where(s => s.IsActive
                                && s.AcademicYearID == academicYear.AcademicYearID
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

        public async Task<List<TeacherStudentGradeDto>?> GetStudentsAsync(int userId, int classId)
        {
            var teacher = await _context.Teachers
                .FirstOrDefaultAsync(t => t.UserID == userId && t.IsActive);

            if (teacher == null)
            {
                return null;
            }

            var assignments = await _context.TeacherAssignments
                .Where(ta => ta.TeacherID == teacher.TeacherID
                             && ta.IsActive
                             && ta.ClassID == classId
                             && ta.AcademicYear.IsActive
                             && ta.Subject.IsActive)
                .Select(ta => new { ta.AcademicYearID, ta.SubjectID })
                .ToListAsync();

            if (!assignments.Any())
            {
                return new List<TeacherStudentGradeDto>();
            }

            var academicYearId = assignments
                .Where(a => a.AcademicYearID.HasValue)
                .Select(a => a.AcademicYearID!.Value)
                .OrderByDescending(id => id)
                .First();

            var subjectIds = assignments
                .Where(a => a.AcademicYearID == academicYearId && a.SubjectID.HasValue)
                .Select(a => a.SubjectID!.Value)
                .Distinct()
                .ToList();

            if (subjectIds.Count != 1)
            {
                throw new ArgumentException("Teacher class has multiple subjects. Current endpoint cannot infer which one to grade.");
            }

            var subjectId = subjectIds[0];

            var term = await _context.Terms
                .Where(t => t.AcademicYearID == academicYearId)
                .OrderBy(t => t.TermID)
                .FirstOrDefaultAsync();

            if (term == null)
            {
                return null;
            }

            var students = await _context.Students
                .Where(s => s.ClassID == classId
                            && s.CurrentAcademicYearID == academicYearId
                            && s.UserID.HasValue)
                .Join(_context.Users,
                      s => s.UserID.Value,
                      u => u.UserId,
                      (s, u) => new
                      {
                          StudentId = s.StudentID,
                          StudentName = u.FullName
                      })
                .ToListAsync();

            var studentIds = students.Select(s => s.StudentId).Distinct().ToList();

            if (!studentIds.Any())
            {
                return new List<TeacherStudentGradeDto>();
            }

            var results = await _context.StudentSubjectTermResults
                .Where(r => r.SubjectID == subjectId
                            && r.TermID == term.TermID
                            && r.AcademicYearID == academicYearId
                            && r.StudentID.HasValue
                            && studentIds.Contains(r.StudentID.Value))
                .Select(r => new
                {
                    StudentId = r.StudentID!.Value,
                    FinalExamScore = r.FinalExamScore,
                    Status = r.Status
                })
                .ToListAsync();

            var resultByStudentId = results.ToDictionary(
                r => r.StudentId,
                r => new { Grade = r.FinalExamScore ?? 0m, Status = r.Status });

            var subjectName = await _context.Subjects
                .Where(sub => sub.SubjectID == subjectId)
                .Select(sub => sub.SubjectName)
                .FirstOrDefaultAsync();

            return students.Select(s =>
            {
                if (!resultByStudentId.TryGetValue(s.StudentId, out var r))
                {
                    return new TeacherStudentGradeDto
                    {
                        StudentId = s.StudentId,
                        StudentName = s.StudentName,
                        SubjectId = subjectId,
                        SubjectName = subjectName ?? "",
                        Grade = 0m,
                        Status = SubjectStatus.InProgress.ToString()
                    };
                }

                return new TeacherStudentGradeDto
                {
                    StudentId = s.StudentId,
                    StudentName = s.StudentName,
                    SubjectId = subjectId,
                    SubjectName = subjectName ?? "",
                    Grade = r.Grade,
                    Status = (r.Status ?? SubjectStatus.InProgress).ToString()
                };
            }).ToList();
        }

        public async Task<TeacherGradeUpdateResponseDto?> UpsertGradeAsync(int userId, TeacherGradeUpdateRequestDTO request)
        {
            var teacher = await _context.Teachers
                .FirstOrDefaultAsync(t => t.UserID == userId && t.IsActive);

            if (teacher == null)
            {
                return null;
            }

            var assignments = await _context.TeacherAssignments
                .Where(ta => ta.TeacherID == teacher.TeacherID
                             && ta.IsActive
                             && ta.ClassID == request.ClassId
                             && ta.AcademicYear.IsActive
                             && ta.Subject.IsActive)
                .Select(ta => new { ta.AcademicYearID, ta.SubjectID })
                .ToListAsync();

            if (!assignments.Any())
            {
                return null;
            }

            var academicYearId = assignments
                .Where(a => a.AcademicYearID.HasValue)
                .Select(a => a.AcademicYearID!.Value)
                .OrderByDescending(id => id)
                .First();

            var subjectIds = assignments
                .Where(a => a.AcademicYearID == academicYearId && a.SubjectID.HasValue)
                .Select(a => a.SubjectID!.Value)
                .Distinct()
                .ToList();

            if (subjectIds.Count != 1)
            {
                throw new ArgumentException("Teacher class has multiple subjects. Current endpoint cannot infer which one to grade.");
            }

            var subjectId = subjectIds[0];

            var term = await _context.Terms
                .Where(t => t.AcademicYearID == academicYearId)
                .OrderBy(t => t.TermID)
                .FirstOrDefaultAsync();

            if (term == null)
            {
                return null;
            }

            var subject = await _context.Subjects
                .Where(s => s.SubjectID == subjectId)
                .Select(s => new { s.MaxFinalScore, s.MaxQuarterScore, s.SubjectName })
                .FirstOrDefaultAsync();

            if (subject == null)
            {
                return null;
            }

            var maxFinalScore = subject.MaxFinalScore ?? 100m;
            var passThreshold = maxFinalScore / 2m;
            var status = request.Grade >= passThreshold ? SubjectStatus.Passed : SubjectStatus.Failed;

            var quarterMax = subject.MaxQuarterScore ?? 25m;
            var quarter1 = decimal.Round(quarterMax * 0.48m, 2, MidpointRounding.AwayFromZero);
            var quarter2 = decimal.Round(quarterMax - quarter1, 2, MidpointRounding.AwayFromZero);

            var existing = await _context.StudentSubjectTermResults
                .FirstOrDefaultAsync(r =>
                    r.StudentID == request.StudentId &&
                    r.SubjectID == subjectId &&
                    r.TermID == term.TermID &&
                    r.AcademicYearID == academicYearId);

            if (existing == null)
            {
                _context.StudentSubjectTermResults.Add(new StudentSubjectTermResult
                {
                    StudentID = request.StudentId,
                    SubjectID = subjectId,
                    TermID = term.TermID,
                    AcademicYearID = academicYearId,
                    Quarter1Score = quarter1,
                    Quarter2Score = quarter2,
                    FinalExamScore = request.Grade,
                    TermTotal = quarter1 + quarter2 + request.Grade,
                    Status = status,
                    CreatedAt = DateTime.UtcNow
                });
            }
            else
            {
                existing.Quarter1Score = existing.Quarter1Score ?? quarter1;
                existing.Quarter2Score = existing.Quarter2Score ?? quarter2;
                existing.FinalExamScore = request.Grade;
                existing.TermTotal = (existing.Quarter1Score ?? 0m) + (existing.Quarter2Score ?? 0m) + request.Grade;
                existing.Status = status;
                existing.LastUpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            // Create quarter submission + audit log for vice dashboard.
            // This endpoint currently updates quarter1/quarter2 of the first term only.
            var existingQuarterSubmission = await _context.QuarterGradeSubmissions
                .FirstOrDefaultAsync(s =>
                    s.StudentID == request.StudentId &&
                    s.SubjectID == subjectId &&
                    s.TermID == term!.TermID &&
                    s.AcademicYearID == academicYearId);

            if (existingQuarterSubmission == null)
            {
                _context.QuarterGradeSubmissions.Add(new QuarterGradeSubmission
                {
                    StudentID = request.StudentId,
                    SubjectID = subjectId,
                    TermID = term.TermID,
                    AcademicYearID = academicYearId,
                    SubmittedAt = DateTime.UtcNow,
                    SubmittedBy = userId
                });
            }
            else
            {
                existingQuarterSubmission.SubmittedAt = DateTime.UtcNow;
                existingQuarterSubmission.SubmittedBy = userId;
            }

            var teacherName = await _context.Users
                .Where(u => u.UserId == userId)
                .Select(u => u.FullName)
                .FirstOrDefaultAsync();

            var classRow = await _context.Classes
                .Where(c => c.ClassID == request.ClassId)
                .Select(c => new { c.ClassName, c.DepartmentID })
                .FirstOrDefaultAsync();

            var stage = await _context.AcademicYears
                .Where(ay => ay.AcademicYearID == academicYearId)
                .Select(ay => ay.Stage)
                .FirstOrDefaultAsync();

            _context.GradeActionLogs.Add(new GradeActionLog
            {
                Action = "Submitted quarter grades",
                ActorUserID = userId,
                ActorName = teacherName,
                StudentID = request.StudentId,
                SubjectID = subjectId,
                AcademicYearID = academicYearId,
                DepartmentID = classRow?.DepartmentID,
                ClassID = request.ClassId,
                TermID = term.TermID,
                Level = stage.ToString().ToLowerInvariant(),
                SubjectName = subject.SubjectName,
                ClassName = classRow?.ClassName,
                Timestamp = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();

            return new TeacherGradeUpdateResponseDto
            {
                ClassId = request.ClassId,
                StudentId = request.StudentId,
                SubjectId = subjectId,
                Grade = request.Grade,
                Status = status.ToString()
            };
        }
    }
}

