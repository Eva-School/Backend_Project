using GradeManagementSystem.Core.DTOs.Student;
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
    public class StudentDashboardService : IStudentDashboardService
    {
        private readonly GradeDbContext _context;

        public StudentDashboardService(GradeDbContext context)
        {
            _context = context;
        }

        public Task<IEnumerable<StudentCardDto>> GetCardsAsync()
        {
            IEnumerable<StudentCardDto> cards = new[]
            {
                new StudentCardDto
                {
                    Id = "quarter-grades",
                    Title = "Quarter Grades",
                    Description = "Review continuous coursework, quizzes, and quarterly assessments.",
                    Route = "/student/quarter"
                },
                new StudentCardDto
                {
                    Id = "final-grades",
                    Title = "Final Grades",
                    Description = "Review official transcripts, final exams, and academic standing.",
                    Route = "/student/final"
                },
                new StudentCardDto
                {
                    Id = "competencies",
                    Title = "Competencies",
                    Description = "Track technical Jadarat mastery, attempts, and teacher evaluations.",
                    Route = "/student/jadarat"
                },
                new StudentCardDto
                {
                    Id = "academic-progress",
                    Title = "Academic Progress",
                    Description = "Analyze performance trends across academic terms and subjects.",
                    Route = "/student/progress"
                }
            };

            return Task.FromResult(cards);
        }

        public async Task<StudentProfileDto?> GetProfileAsync(int userId)
        {
            var student = await _context.Students
                .Include(s => s.CurrentAcademicYear)
                .Include(s => s.Class)
                .Include(s => s.Major)
                .Include(s => s.Department)
                .FirstOrDefaultAsync(s => s.UserID == userId);

            if (student == null)
            {
                return null;
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId);

            // Calculate metrics
            var totalEnrolledSubjects = await _context.StudentSubjectTermResults
                .Where(r => r.StudentID == student.StudentID && r.AcademicYearID == student.CurrentAcademicYearID)
                .Select(r => r.SubjectID)
                .Distinct()
                .CountAsync();

            if (totalEnrolledSubjects == 0 && student.ClassID.HasValue)
            {
                totalEnrolledSubjects = await _context.TeacherAssignments
                    .Where(ta => ta.ClassID == student.ClassID.Value && ta.IsActive)
                    .Select(ta => ta.SubjectID)
                    .Distinct()
                    .CountAsync();
            }

            var competencies = await _context.StudentCompetencyStatuses
                .Where(s => s.StudentID == student.StudentID)
                .ToListAsync();

            var totalCompetencies = competencies.Count;
            var completedCompetencies = competencies.Count(c =>
                !string.IsNullOrEmpty(c.StatusID) &&
                (c.StatusID.Equals("Pass", StringComparison.OrdinalIgnoreCase) ||
                 c.StatusID.Equals("Passed", StringComparison.OrdinalIgnoreCase) ||
                 c.StatusID.Equals("Competent", StringComparison.OrdinalIgnoreCase) ||
                 c.StatusID.Equals("Completed", StringComparison.OrdinalIgnoreCase)));

            decimal? overallGpa = null;
            var allResults = await _context.StudentAllResults
                .Where(r => r.StudentID == student.StudentID && r.FinalSubjectScore.HasValue)
                .ToListAsync();

            if (allResults.Count > 0)
            {
                var avgScore = allResults.Average(r => r.FinalSubjectScore!.Value);
                overallGpa = Math.Round((avgScore / 100m) * 4.0m, 2);
            }
            else
            {
                var termScores = await _context.StudentSubjectTermResults
                    .Where(r => r.StudentID == student.StudentID && r.TermTotal.HasValue)
                    .ToListAsync();
                if (termScores.Count > 0)
                {
                    var avgScore = termScores.Average(r => r.TermTotal!.Value);
                    overallGpa = Math.Round((avgScore / 100m) * 4.0m, 2);
                }
            }

            var yearStageStr = student.CurrentAcademicYear?.Stage.ToString() ?? "Junior";
            var yearLabel = ToYearLabel(yearStageStr);

            return new StudentProfileDto
            {
                StudentId = student.StudentID,
                UserId = student.UserID,
                StudentCode = student.StudentCode ?? $"STD-{student.StudentID:D5}",
                NationalId = student.NationalID ?? string.Empty,
                Name = !string.IsNullOrWhiteSpace(student.NameEnglish) ? student.NameEnglish : (user?.FullName ?? "Student"),
                NameArabic = student.NameArabic,
                Email = student.Email ?? user?.Email,
                Phone = student.StudentPhone ?? user?.PhoneNumber,
                Year = yearLabel,
                CurrentAcademicYear = yearStageStr.ToLowerInvariant(),
                AcademicYearName = student.CurrentAcademicYear?.YearName ?? string.Empty,
                ClassName = student.Class?.ClassName ?? "Unassigned",
                Section = null,
                DepartmentName = student.Department?.DepartmentName ?? "General",
                MajorName = student.Major?.MajorName ?? "General",
                Address = student.Address ?? string.Empty,
                AddressArabic = student.AddressArabic,
                FatherName = student.FatherName,
                FatherPhone = student.FatherPhone,
                RelativeName = student.RelativeName,
                RelativePhone = student.RelativePhone,
                EnrollmentDate = student.EnrollmentDate,
                Status = student.Status ?? "Active",
                Subtitle = "Your academic overview",
                TotalEnrolledSubjects = totalEnrolledSubjects,
                CompletedCompetencies = completedCompetencies,
                TotalCompetencies = totalCompetencies,
                OverallGpa = overallGpa
            };
        }

        public async Task<bool> UpdateProfileAsync(int userId, UpdateStudentContactDto request)
        {
            var student = await _context.Students.FirstOrDefaultAsync(s => s.UserID == userId);
            if (student == null)
            {
                return false;
            }

            if (request.Phone != null)
            {
                student.StudentPhone = request.Phone.Trim();
                var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId);
                if (user != null)
                {
                    user.PhoneNumber = request.Phone.Trim();
                }
            }

            if (request.Address != null)
            {
                student.Address = request.Address.Trim();
            }

            if (request.AddressArabic != null)
            {
                student.AddressArabic = request.AddressArabic.Trim();
            }

            if (request.RelativeName != null)
            {
                student.RelativeName = request.RelativeName.Trim();
            }

            if (request.RelativePhone != null)
            {
                student.RelativePhone = request.RelativePhone.Trim();
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<StudentYearOptionDto>> GetYearsAsync()
        {
            var years = await _context.AcademicYears
                .Where(y => y.IsActive)
                .Select(y => y.Stage)
                .Distinct()
                .ToListAsync();

            return years
                .OrderBy(y => y)
                .Select(stage => new StudentYearOptionDto
                {
                    Id = stage.ToString().ToLowerInvariant(),
                    Number = ((int)stage + 1).ToString(),
                    Title = stage.ToString()
                })
                .ToList();
        }

        public async Task<StudentQuarterGradesResponseDto?> GetQuarterGradesAsync(int userId, string year, int? termId = null)
        {
            var contextInfo = await ResolveContextAsync(userId, year);
            if (contextInfo == null)
            {
                return null;
            }

            var studentId = contextInfo.Value.StudentId;
            var academicYearId = contextInfo.Value.AcademicYearId;

            var academicYear = await _context.AcademicYears.FindAsync(academicYearId);
            var academicYearName = academicYear?.YearName ?? year;

            var availableTerms = await _context.StudentSubjectTermResults
                .Where(r => r.StudentID == studentId && r.AcademicYearID == academicYearId && r.TermID.HasValue)
                .Select(r => r.TermID!.Value)
                .Distinct()
                .OrderBy(t => t)
                .ToListAsync();

            if (availableTerms.Count == 0)
            {
                availableTerms = new List<int> { 1, 2 };
            }

            var selectedTerm = termId ?? availableTerms.FirstOrDefault();

            var query = _context.StudentSubjectTermResults
                .Where(r => r.StudentID == studentId && r.AcademicYearID == academicYearId);

            if (termId.HasValue)
            {
                query = query.Where(r => r.TermID == termId.Value);
            }

            var termResults = await query
                .Include(r => r.Subject)
                .ToListAsync();

            var quizGrades = await _context.QuizGrades
                .Include(qg => qg.Quiz)
                .Where(qg => qg.StudentID == studentId && qg.Quiz.AcademicYearID == academicYearId)
                .ToListAsync();

            var gradesList = new List<StudentQuarterGradeItemDto>();

            foreach (var r in termResults)
            {
                var subjectQuizzes = quizGrades
                    .Where(qg => qg.Quiz.SubjectID == r.SubjectID)
                    .OrderBy(qg => qg.Quiz.QuizDate)
                    .Select(qg => new StudentQuizItemDto
                    {
                        QuizId = qg.QuizID,
                        Title = qg.Quiz.Title,
                        Score = qg.Score,
                        MaxScore = qg.Quiz.MaxScore,
                        QuizDate = qg.Quiz.QuizDate,
                        Notes = qg.Notes
                    })
                    .ToList();

                var q1 = r.Quarter1Score;
                var q2 = r.Quarter2Score;
                var q3 = r.Quarter3Score;
                var q4 = r.Quarter4Score;

                var courseworkTotal = (q1 ?? 0) + (q2 ?? 0) + (q3 ?? 0) + (q4 ?? 0);

                var maxQ1 = (decimal?)r.Subject.MaxQuarterQ1Score;
                var maxQ2 = (decimal?)r.Subject.MaxQuarterQ2Score;
                var maxQ3 = (decimal?)r.Subject.MaxQuarterQ3Score;
                var maxQ4 = (decimal?)r.Subject.MaxQuarterQ4Score;
                var maxQuarter = (decimal?)(r.Subject.MaxQuarterScore ?? 100);

                var totalMaxCoursework = (maxQ1 ?? 0) + (maxQ2 ?? 0) + (maxQ3 ?? 0) + (maxQ4 ?? 0);
                if (totalMaxCoursework == 0)
                {
                    totalMaxCoursework = maxQuarter ?? 100;
                }

                var percentage = totalMaxCoursework > 0 ? Math.Round((courseworkTotal / totalMaxCoursework) * 100m, 1) : 0m;

                gradesList.Add(new StudentQuarterGradeItemDto
                {
                    SubjectId = r.SubjectID ?? 0,
                    Subject = r.Subject.SubjectName,
                    SubjectArabic = null,
                    SubjectCode = $"SUB-{r.SubjectID:D3}",
                    Quarter1 = q1,
                    Quarter2 = q2,
                    Quarter3 = q3,
                    Quarter4 = q4,
                    MaxQ1 = maxQ1,
                    MaxQ2 = maxQ2,
                    MaxQ3 = maxQ3,
                    MaxQ4 = maxQ4,
                    MaxQuarter = maxQuarter,
                    CourseworkTotal = courseworkTotal,
                    YourGrade = courseworkTotal,
                    QuarterGrade = courseworkTotal,
                    Percentage = percentage,
                    Quizzes = subjectQuizzes
                });
            }

            return new StudentQuarterGradesResponseDto
            {
                Grades = gradesList,
                Year = year.ToLowerInvariant(),
                AcademicYearName = academicYearName,
                AvailableTerms = availableTerms,
                SelectedTerm = selectedTerm
            };
        }

        public async Task<StudentFinalGradesResponseDto?> GetFinalGradesAsync(int userId, string year)
        {
            var contextInfo = await ResolveContextAsync(userId, year);
            if (contextInfo == null)
            {
                return null;
            }

            var studentId = contextInfo.Value.StudentId;
            var academicYearId = contextInfo.Value.AcademicYearId;

            var academicYear = await _context.AcademicYears.FindAsync(academicYearId);
            var academicYearName = academicYear?.YearName ?? year;

            var allResults = await _context.StudentAllResults
                .Include(r => r.Subject)
                .Include(r => r.ResultApproval)
                .Where(r => r.StudentID == studentId && r.AcademicYearID == academicYearId)
                .ToListAsync();

            var termResults = await _context.StudentSubjectTermResults
                .Include(r => r.Subject)
                .Where(r => r.StudentID == studentId && r.AcademicYearID == academicYearId)
                .ToListAsync();

            var gradesList = new List<StudentFinalGradeItemDto>();

            if (allResults.Count > 0)
            {
                foreach (var r in allResults)
                {
                    var matchingTermResult = termResults.FirstOrDefault(tr => tr.SubjectID == r.SubjectID);
                    var coursework = matchingTermResult != null
                        ? (matchingTermResult.Quarter1Score ?? 0) + (matchingTermResult.Quarter2Score ?? 0) + (matchingTermResult.Quarter3Score ?? 0) + (matchingTermResult.Quarter4Score ?? 0)
                        : 0m;

                    var finalExam = matchingTermResult?.FinalExamScore ?? (r.FinalSubjectScore ?? 0);
                    var total = r.TotalTermScore ?? r.FinalSubjectScore ?? (coursework + finalExam);
                    var maxFinal = (decimal)(r.Subject.MaxFinalScore ?? 100);
                    var maxQuarter = (decimal)(r.Subject.MaxQuarterScore ?? 100);
                    var maxTotal = maxQuarter + maxFinal;
                    if (maxTotal == 0) maxTotal = 100;

                    var percentage = Math.Round((total / maxTotal) * 100m, 1);
                    var letterGrade = r.Grade?.ToString() ?? CalculateLetterGrade(percentage);
                    var isApproved = r.ResultApproval?.Decision == Decision.Approved;

                    gradesList.Add(new StudentFinalGradeItemDto
                    {
                        SubjectId = r.SubjectID ?? 0,
                        Subject = r.Subject.SubjectName,
                        SubjectCode = $"SUB-{r.SubjectID:D3}",
                        CreditHours = 3,
                        CourseworkScore = coursework,
                        FinalExamScore = finalExam,
                        TotalScore = total,
                        MaxScore = maxTotal,
                        Percentage = percentage,
                        LetterGrade = letterGrade,
                        Status = r.SubjectStatus?.ToString() ?? (percentage >= 60 ? "Pass" : "Fail"),
                        IsApproved = isApproved,
                        YourGrade = total,
                        QuarterGrade = coursework
                    });
                }
            }
            else
            {
                foreach (var r in termResults)
                {
                    var coursework = (r.Quarter1Score ?? 0) + (r.Quarter2Score ?? 0) + (r.Quarter3Score ?? 0) + (r.Quarter4Score ?? 0);
                    var finalExam = r.FinalExamScore ?? 0;
                    var total = r.TermTotal ?? (coursework + finalExam);

                    var maxFinal = (decimal)(r.Subject.MaxFinalScore ?? 100);
                    var maxQuarter = (decimal)(r.Subject.MaxQuarterScore ?? 100);
                    var maxTotal = maxQuarter + maxFinal;
                    if (maxTotal == 0) maxTotal = 100;

                    var percentage = Math.Round((total / maxTotal) * 100m, 1);
                    var letterGrade = CalculateLetterGrade(percentage);

                    gradesList.Add(new StudentFinalGradeItemDto
                    {
                        SubjectId = r.SubjectID ?? 0,
                        Subject = r.Subject.SubjectName,
                        SubjectCode = $"SUB-{r.SubjectID:D3}",
                        CreditHours = 3,
                        CourseworkScore = coursework,
                        FinalExamScore = finalExam,
                        TotalScore = total,
                        MaxScore = maxTotal,
                        Percentage = percentage,
                        LetterGrade = letterGrade,
                        Status = r.Status?.ToString() ?? (percentage >= 60 ? "Pass" : "Fail"),
                        IsApproved = false,
                        YourGrade = total,
                        QuarterGrade = coursework
                    });
                }
            }

            decimal? termGpa = null;
            decimal? cumulativeAvg = null;
            var totalCredits = gradesList.Count * 3;

            if (gradesList.Count > 0)
            {
                cumulativeAvg = Math.Round(gradesList.Average(g => g.Percentage), 1);
                var totalQualityPoints = gradesList.Sum(g => LetterGradeToPoints(g.LetterGrade) * 3);
                termGpa = totalCredits > 0 ? Math.Round(totalQualityPoints / totalCredits, 2) : 0m;
            }

            var standing = (termGpa ?? 0) >= 3.5m ? "Excellent Standing"
                         : (termGpa ?? 0) >= 3.0m ? "Very Good Standing"
                         : (termGpa ?? 0) >= 2.0m ? "Good Standing"
                         : "Academic Warning";

            return new StudentFinalGradesResponseDto
            {
                Grades = gradesList,
                Year = year.ToLowerInvariant(),
                AcademicYearName = academicYearName,
                TermGpa = termGpa,
                CumulativeAverage = cumulativeAvg,
                TotalCredits = totalCredits,
                Standing = standing
            };
        }

        public async Task<StudentCompetenciesResponseDto?> GetJadaratGradesAsync(int userId, string year)
        {
            var contextInfo = await ResolveContextAsync(userId, year);
            if (contextInfo == null)
            {
                return null;
            }

            var studentId = contextInfo.Value.StudentId;
            var academicYear = await _context.AcademicYears.FindAsync(contextInfo.Value.AcademicYearId);

            var statuses = await _context.StudentCompetencyStatuses
                .Include(s => s.Competency)
                    .ThenInclude(c => c.Major)
                .Include(s => s.CompetencyAttempts)
                    .ThenInclude(a => a.Evaluator)
                .Where(s => s.StudentID == studentId)
                .ToListAsync();

            var teacherIds = statuses
                .SelectMany(s => s.CompetencyAttempts)
                .Where(a => a.EvaluatedBy.HasValue)
                .Select(a => a.EvaluatedBy!.Value)
                .Distinct()
                .ToList();

            var teacherUsers = await _context.Teachers
                .Where(t => teacherIds.Contains(t.TeacherID) && t.UserID.HasValue)
                .Join(_context.Users, t => t.UserID, u => u.UserId, (t, u) => new { t.TeacherID, u.FullName })
                .ToDictionaryAsync(x => x.TeacherID, x => x.FullName);

            var grades = new List<StudentCompetencyGradeItemDto>();

            foreach (var s in statuses)
            {
                var attempts = s.CompetencyAttempts
                    .OrderBy(a => a.AttemptNumber)
                    .Select(a => new CompetencyAttemptHistoryDto
                    {
                        AttemptId = a.AttemptID,
                        AttemptNumber = a.AttemptNumber ?? 1,
                        Result = a.Result ?? "Pending",
                        EvaluatedAt = a.EvaluatedAt,
                        EvaluatedByName = a.EvaluatedBy.HasValue && teacherUsers.ContainsKey(a.EvaluatedBy.Value)
                            ? teacherUsers[a.EvaluatedBy.Value]
                            : (a.Evaluator != null ? $"Teacher #{a.EvaluatedBy}" : null)
                    })
                    .ToList();

                var lastAttempt = attempts.LastOrDefault();
                var evaluatorName = lastAttempt?.EvaluatedByName;

                var currentStatus = s.StatusID ?? "Pending";
                var currentAttemptNumber = s.CurrentAttemptNumber ?? 1;
                var maxAttempts = s.MaxAllowedAttempts ?? s.Competency.MaxAttempts ?? 3;

                grades.Add(new StudentCompetencyGradeItemDto
                {
                    CompetencyId = s.CompetencyID ?? 0,
                    Jadarat = s.Competency.CompetencyName,
                    MajorName = s.Competency.Major?.MajorName,
                    CurrentStatus = currentStatus,
                    CurrentAttempt = currentAttemptNumber,
                    MaxAttempts = maxAttempts,
                    LastEvaluatedAt = s.LastEvaluatedAt ?? lastAttempt?.EvaluatedAt,
                    EvaluatorName = evaluatorName,
                    Your_Attemps = currentStatus,
                    Attemps = $"Attempt {currentAttemptNumber} of {maxAttempts}",
                    AttemptHistory = attempts
                });
            }

            var total = grades.Count;
            var passed = grades.Count(g =>
                g.CurrentStatus.Equals("Pass", StringComparison.OrdinalIgnoreCase) ||
                g.CurrentStatus.Equals("Passed", StringComparison.OrdinalIgnoreCase) ||
                g.CurrentStatus.Equals("Competent", StringComparison.OrdinalIgnoreCase));
            var pending = total - passed;

            return new StudentCompetenciesResponseDto
            {
                Grades = grades,
                Year = year.ToLowerInvariant(),
                AcademicYearName = academicYear?.YearName ?? year,
                TotalCompetencies = total,
                PassedCompetencies = passed,
                PendingCompetencies = pending
            };
        }

        public async Task<StudentEnrollmentDetailsDto?> GetEnrollmentDetailsAsync(int userId)
        {
            var student = await _context.Students
                .Include(s => s.Class)
                .Include(s => s.CurrentAcademicYear)
                .FirstOrDefaultAsync(s => s.UserID == userId);

            if (student == null)
            {
                return null;
            }

            var teachersList = new List<StudentEnrolledTeacherDto>();

            if (student.ClassID.HasValue)
            {
                var assignments = await _context.TeacherAssignments
                    .Include(ta => ta.Subject)
                    .Include(ta => ta.Teacher)
                    .Where(ta => ta.ClassID == student.ClassID.Value && ta.IsActive)
                    .ToListAsync();

                var teacherUserIds = assignments
                    .Where(ta => ta.Teacher != null && ta.Teacher.UserID.HasValue)
                    .Select(ta => ta.Teacher!.UserID!.Value)
                    .Distinct()
                    .ToList();

                var usersMap = await _context.Users
                    .Where(u => teacherUserIds.Contains(u.UserId))
                    .ToDictionaryAsync(u => u.UserId, u => new { u.FullName, u.Email });

                foreach (var ta in assignments)
                {
                    var teacherName = "Assigned Teacher";
                    string? teacherEmail = null;

                    if (ta.Teacher?.UserID != null && usersMap.TryGetValue(ta.Teacher.UserID.Value, out var userDetails))
                    {
                        teacherName = userDetails.FullName;
                        teacherEmail = userDetails.Email;
                    }

                    teachersList.Add(new StudentEnrolledTeacherDto
                    {
                        SubjectId = ta.SubjectID ?? 0,
                        SubjectName = ta.Subject?.SubjectName ?? "Subject",
                        SubjectCode = $"SUB-{ta.SubjectID:D3}",
                        TeacherName = teacherName,
                        TeacherEmail = teacherEmail
                    });
                }
            }

            return new StudentEnrollmentDetailsDto
            {
                StudentId = student.StudentID,
                ClassId = student.ClassID,
                ClassName = student.Class?.ClassName ?? "Unassigned",
                Section = string.Empty,
                AcademicYearName = student.CurrentAcademicYear?.YearName ?? string.Empty,
                Stage = student.CurrentAcademicYear?.Stage.ToString() ?? string.Empty,
                Teachers = teachersList
            };
        }

        public async Task<IEnumerable<StudentProgressPointDto>> GetProgressAsync(int userId, string year)
        {
            var contextInfo = await ResolveContextAsync(userId, year);
            if (contextInfo == null)
            {
                return Enumerable.Empty<StudentProgressPointDto>();
            }

            var results = await _context.StudentSubjectTermResults
                .AsNoTracking()
                .Where(r => r.StudentID == contextInfo.Value.StudentId && r.AcademicYearID == contextInfo.Value.AcademicYearId)
                .Select(r => new
                {
                    r.Subject.SubjectName,
                    r.Quarter1Score,
                    r.Quarter2Score,
                    r.Quarter3Score,
                    r.Quarter4Score,
                    FinalExamScore = _context.StudentAllResults
                        .Where(ar => ar.StudentID == r.StudentID && ar.SubjectID == r.SubjectID && ar.TermID == r.TermID && ar.AcademicYearID == r.AcademicYearID && ar.ResultApproval != null && ar.ResultApproval.Decision == Decision.Approved)
                        .Select(ar => ar.FinalSubjectScore)
                        .FirstOrDefault() ?? r.FinalExamScore,
                    r.Subject.MaxQuarterQ1Score,
                    r.Subject.MaxQuarterQ2Score,
                    r.Subject.MaxQuarterQ3Score,
                    r.Subject.MaxQuarterQ4Score,
                    r.Subject.MaxQuarterScore,
                    r.Subject.MaxFinalScore
                })
                .ToListAsync();

            var grouped = results
                .GroupBy(r => r.SubjectName)
                .OrderBy(g => g.Key);

            return grouped.Select(group =>
            {
                var quarterPercentages = group.SelectMany(item => new[]
                {
                    ToPercentage(item.Quarter1Score, item.MaxQuarterQ1Score ?? item.MaxQuarterScore),
                    ToPercentage(item.Quarter2Score, item.MaxQuarterQ2Score ?? item.MaxQuarterScore),
                    ToPercentage(item.Quarter3Score, item.MaxQuarterQ3Score ?? item.MaxQuarterScore),
                    ToPercentage(item.Quarter4Score, item.MaxQuarterQ4Score ?? item.MaxQuarterScore)
                })
                .Where(value => value.HasValue)
                .Select(value => value!.Value)
                .ToList();

                var finalPercentages = group
                    .Select(item => ToPercentage(item.FinalExamScore, item.MaxFinalScore))
                    .Where(value => value.HasValue)
                    .Select(value => value!.Value)
                    .ToList();

                return new StudentProgressPointDto
                {
                    Subject = group.Key,
                    QuarterAverage = quarterPercentages.Count == 0 ? 0 : Math.Round(quarterPercentages.Average(), 1),
                    FinalExam = finalPercentages.Count == 0 ? 0 : Math.Round(finalPercentages.Average(), 1)
                };
            }).ToList();
        }

        public async Task<StudentReportDto?> GetReportAsync(int userId, string year)
        {
            if (string.IsNullOrWhiteSpace(year))
            {
                return null;
            }

            var student = await _context.Students
                .AsNoTracking()
                .Where(item => item.UserID == userId)
                .Select(item => new
                {
                    item.StudentID,
                    ClassName = item.Class != null ? item.Class.ClassName : "Unassigned"
                })
                .FirstOrDefaultAsync();
            if (student == null)
            {
                return null;
            }

            var academicYear = await _context.AcademicYears
                .AsNoTracking()
                .Where(item => item.IsActive && item.YearName == year.Trim())
                .OrderByDescending(item => item.AcademicYearID)
                .FirstOrDefaultAsync();

            if (academicYear == null && Enum.TryParse<EducationStage>(year, true, out var stage))
            {
                academicYear = await _context.AcademicYears
                    .AsNoTracking()
                    .Where(item => item.IsActive && item.Stage == stage)
                    .OrderByDescending(item => item.AcademicYearID)
                    .FirstOrDefaultAsync();
            }
            if (academicYear == null)
            {
                return null;
            }

            var resultRows = await _context.StudentSubjectTermResults
                .AsNoTracking()
                .Where(item => item.StudentID == student.StudentID && item.AcademicYearID == academicYear.AcademicYearID)
                .Select(item => new
                {
                    Subject = item.Subject.SubjectName,
                    item.Quarter1Score,
                    item.Quarter2Score,
                    item.Quarter3Score,
                    item.Quarter4Score,
                    FinalExamScore = _context.StudentAllResults
                        .Where(ar => ar.StudentID == item.StudentID && ar.SubjectID == item.SubjectID && ar.TermID == item.TermID && ar.AcademicYearID == item.AcademicYearID && ar.ResultApproval != null && ar.ResultApproval.Decision == Decision.Approved)
                        .Select(ar => ar.FinalSubjectScore)
                        .FirstOrDefault() ?? item.FinalExamScore,
                    item.Subject.MaxQuarterQ1Score,
                    item.Subject.MaxQuarterQ2Score,
                    item.Subject.MaxQuarterQ3Score,
                    item.Subject.MaxQuarterQ4Score,
                    item.Subject.MaxQuarterScore,
                    item.Subject.MaxFinalScore
                })
                .ToListAsync();

            var grades = resultRows
                .GroupBy(item => item.Subject)
                .OrderBy(group => group.Key)
                .Select(group =>
                {
                    var q1 = group.Average(item => item.Quarter1Score ?? 0m);
                    var q2 = group.Average(item => item.Quarter2Score ?? 0m);
                    var q3 = group.Average(item => item.Quarter3Score ?? 0m);
                    var q4 = group.Average(item => item.Quarter4Score ?? 0m);
                    var final = group.Average(item => item.FinalExamScore ?? 0m);
                    var points = group.SelectMany(item => new[]
                    {
                        new { Score = item.Quarter1Score, Maximum = (decimal)(item.MaxQuarterQ1Score ?? item.MaxQuarterScore ?? 100) },
                        new { Score = item.Quarter2Score, Maximum = (decimal)(item.MaxQuarterQ2Score ?? item.MaxQuarterScore ?? 100) },
                        new { Score = item.Quarter3Score, Maximum = (decimal)(item.MaxQuarterQ3Score ?? item.MaxQuarterScore ?? 100) },
                        new { Score = item.Quarter4Score, Maximum = (decimal)(item.MaxQuarterQ4Score ?? item.MaxQuarterScore ?? 100) },
                        new { Score = item.FinalExamScore, Maximum = (decimal)(item.MaxFinalScore ?? 100) }
                    }).Where(item => item.Score.HasValue).ToList();
                    var average = points.Count == 0
                        ? 0m
                        : Math.Round(points.Sum(item => item.Score!.Value) * 100m / points.Sum(item => item.Maximum), 1);

                    return new StudentReportGradeDto
                    {
                        Subject = group.Key,
                        Q1 = q1,
                        Q2 = q2,
                        Q3 = q3,
                        Q4 = q4,
                        Final = final,
                        Average = average
                    };
                })
                .ToList();

            var studentName = await _context.Users
                .AsNoTracking()
                .Where(item => item.UserId == userId)
                .Select(item => item.FullName)
                .FirstOrDefaultAsync() ?? "Student";

            return new StudentReportDto
            {
                StudentName = studentName,
                StudentId = student.StudentID.ToString(),
                ClassName = student.ClassName,
                Year = academicYear.YearName,
                Grades = grades
            };
        }

        private async Task<(int StudentId, int AcademicYearId)?> ResolveContextAsync(int userId, string year)
        {
            if (!Enum.TryParse<EducationStage>(year, true, out var stage))
            {
                return null;
            }

            var studentId = await _context.Students
                .Where(s => s.UserID == userId)
                .Select(s => s.StudentID)
                .FirstOrDefaultAsync();

            if (studentId == 0)
            {
                return null;
            }

            var academicYearId = await _context.AcademicYears
                .Where(y => y.IsActive && y.Stage == stage)
                .OrderByDescending(y => y.AcademicYearID)
                .Select(y => y.AcademicYearID)
                .FirstOrDefaultAsync();

            if (academicYearId == 0)
            {
                return null;
            }

            return (studentId, academicYearId);
        }

        private static string ToYearLabel(string stage)
        {
            return stage.ToLowerInvariant() switch
            {
                "junior" => "Year 1",
                "wheeler" => "Year 2",
                "senior" => "Year 3",
                _ => "Year"
            };
        }

        private static decimal? ToPercentage(decimal? score, int? maximum)
        {
            if (!score.HasValue)
            {
                return null;
            }

            var denominator = maximum.GetValueOrDefault(100);
            return denominator <= 0 ? 0m : Math.Round(score.Value * 100m / denominator, 1);
        }

        private static string CalculateLetterGrade(decimal percentage)
        {
            return percentage switch
            {
                >= 90m => "A+",
                >= 85m => "A",
                >= 80m => "B+",
                >= 75m => "B",
                >= 70m => "C+",
                >= 65m => "C",
                >= 60m => "D",
                _ => "F"
            };
        }

        private static decimal LetterGradeToPoints(string letterGrade)
        {
            return letterGrade.ToUpperInvariant() switch
            {
                "A+" => 4.0m,
                "A" => 3.75m,
                "B+" => 3.3m,
                "B" => 3.0m,
                "C+" => 2.5m,
                "C" => 2.0m,
                "D" => 1.0m,
                _ => 0.0m
            };
        }
    }
}
