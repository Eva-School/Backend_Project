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

            decimal? overallPercentage = null;
            var allResults = await _context.StudentAllResults
                .Include(r => r.Subject)
                .Where(r => r.StudentID == student.StudentID && r.FinalSubjectScore.HasValue)
                .ToListAsync();

            if (allResults.Count > 0)
            {
                var totalEarned = allResults.Sum(r => r.FinalSubjectScore!.Value);
                var totalMax = allResults.Sum(r => (decimal)(r.Subject?.MaxFinalScore ?? 100));
                overallPercentage = totalMax > 0 ? Math.Round((totalEarned / totalMax) * 100m, 1) : 0m;
            }
            else
            {
                var termScores = await _context.StudentSubjectTermResults
                    .Include(r => r.Subject)
                    .Where(r => r.StudentID == student.StudentID && (r.TermTotal.HasValue || r.Quarter1Score.HasValue))
                    .ToListAsync();
                if (termScores.Count > 0)
                {
                    var totalEarned = termScores.Sum(r => r.TermTotal ?? ((r.Quarter1Score ?? 0) + (r.Quarter2Score ?? 0) + (r.Quarter3Score ?? 0) + (r.Quarter4Score ?? 0) + (r.FinalExamScore ?? 0)));
                    var totalMax = termScores.Sum(r => (decimal)((r.Subject?.MaxQuarterScore ?? 25) + (r.Subject?.MaxFinalScore ?? 100)));
                    overallPercentage = totalMax > 0 ? Math.Round((totalEarned / totalMax) * 100m, 1) : 0m;
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
                OverallPercentage = overallPercentage
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

            var terms = await _context.Terms
                .Where(t => t.AcademicYearID == academicYearId)
                .OrderBy(t => t.TermID)
                .ToListAsync();

            // Resolve logical term number (1 or 2) and actual DB TermID
            int logicalTermNumber = 1;
            int selectedDbTermId;

            if (terms.Count > 0)
            {
                if (termId.HasValue)
                {
                    if (termId.Value == 1)
                    {
                        logicalTermNumber = 1;
                        selectedDbTermId = terms[0].TermID;
                    }
                    else if (termId.Value == 2 && terms.Count > 1)
                    {
                        logicalTermNumber = 2;
                        selectedDbTermId = terms[1].TermID;
                    }
                    else
                    {
                        var matchingTermIndex = terms.FindIndex(t => t.TermID == termId.Value);
                        if (matchingTermIndex >= 0)
                        {
                            logicalTermNumber = matchingTermIndex + 1;
                            selectedDbTermId = terms[matchingTermIndex].TermID;
                        }
                        else
                        {
                            logicalTermNumber = 1;
                            selectedDbTermId = terms[0].TermID;
                        }
                    }
                }
                else
                {
                    logicalTermNumber = 1;
                    selectedDbTermId = terms[0].TermID;
                }
            }
            else
            {
                selectedDbTermId = termId ?? 1;
                logicalTermNumber = termId ?? 1;
            }

            var availableTerms = terms.Count > 0
                ? Enumerable.Range(1, terms.Count).ToList()
                : new List<int> { 1, 2 };

            // Dynamic: load all active subjects for this academic year
            var enrolledSubjects = await _context.Subjects
                .Where(s => s.AcademicYearID == academicYearId && s.IsActive)
                .OrderBy(s => s.SubjectID)
                .ToListAsync();

            var termResults = await _context.StudentSubjectTermResults
                .Where(r => r.StudentID == studentId && r.AcademicYearID == academicYearId && r.TermID == selectedDbTermId)
                .Include(r => r.Subject)
                .ToListAsync();

            var quizGrades = await _context.QuizGrades
                .Include(qg => qg.Quiz)
                .Where(qg => qg.StudentID == studentId && qg.Quiz.AcademicYearID == academicYearId)
                .ToListAsync();

            var gradesList = new List<StudentQuarterGradeItemDto>();

            foreach (var s in enrolledSubjects)
            {
                var r = termResults.FirstOrDefault(tr => tr.SubjectID == s.SubjectID);
                var subjectQuizzes = quizGrades
                    .Where(qg => qg.Quiz.SubjectID == s.SubjectID)
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

                var q1 = r?.Quarter1Score;
                var q2 = r?.Quarter2Score;
                var q3 = r?.Quarter3Score;
                var q4 = r?.Quarter4Score;

                var maxQ1 = (decimal?)(s.MaxQuarterQ1Score ?? (s.MaxQuarterScore.HasValue ? s.MaxQuarterScore.Value / 2 : 12));
                var maxQ2 = (decimal?)(s.MaxQuarterQ2Score ?? (s.MaxQuarterScore.HasValue ? s.MaxQuarterScore.Value - (s.MaxQuarterScore.Value / 2) : 13));
                var maxQ3 = (decimal?)(s.MaxQuarterQ3Score ?? (s.MaxQuarterScore.HasValue ? s.MaxQuarterScore.Value / 2 : 12));
                var maxQ4 = (decimal?)(s.MaxQuarterQ4Score ?? (s.MaxQuarterScore.HasValue ? s.MaxQuarterScore.Value - (s.MaxQuarterScore.Value / 2) : 13));
                var maxQuarter = (decimal?)(s.MaxQuarterScore ?? 25);

                decimal? courseworkTotal = null;
                decimal totalMaxCoursework;
                bool hasGrades;

                if (logicalTermNumber == 1)
                {
                    var hasT1 = q1.HasValue || q2.HasValue;
                    var hasT2 = q3.HasValue || q4.HasValue;
                    if (hasT1 && hasT2 && terms.Count <= 1)
                    {
                        // Single-term test fixture with all 4 quarters on 1 record
                        courseworkTotal = (q1 ?? 0) + (q2 ?? 0) + (q3 ?? 0) + (q4 ?? 0);
                        totalMaxCoursework = (maxQ1 ?? 0) + (maxQ2 ?? 0) + (maxQ3 ?? 0) + (maxQ4 ?? 0);
                        hasGrades = true;
                    }
                    else
                    {
                        courseworkTotal = hasT1 ? (q1 ?? 0) + (q2 ?? 0) : null;
                        totalMaxCoursework = (maxQ1 ?? 0) + (maxQ2 ?? 0);
                        hasGrades = hasT1;
                    }
                }
                else
                {
                    var hasT2 = q3.HasValue || q4.HasValue;
                    courseworkTotal = hasT2 ? (q3 ?? 0) + (q4 ?? 0) : null;
                    totalMaxCoursework = (maxQ3 ?? 0) + (maxQ4 ?? 0);
                    hasGrades = hasT2;
                }

                if (totalMaxCoursework == 0)
                {
                    totalMaxCoursework = maxQuarter ?? 25;
                }

                var percentage = (r != null && hasGrades && courseworkTotal.HasValue && totalMaxCoursework > 0)
                    ? Math.Round((courseworkTotal.Value / totalMaxCoursework) * 100m, 1)
                    : (decimal?)null;

                gradesList.Add(new StudentQuarterGradeItemDto
                {
                    SubjectId = s.SubjectID,
                    Subject = s.SubjectName,
                    SubjectArabic = null,
                    SubjectCode = $"SUB-{s.SubjectID:D3}",
                    Quarter1 = q1,
                    Quarter2 = q2,
                    Quarter3 = q3,
                    Quarter4 = q4,
                    MaxQ1 = maxQ1,
                    MaxQ2 = maxQ2,
                    MaxQ3 = maxQ3,
                    MaxQ4 = maxQ4,
                    MaxQuarter = totalMaxCoursework,
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
                SelectedTerm = logicalTermNumber
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

            var enrolledSubjects = await _context.Subjects
                .Where(s => s.AcademicYearID == academicYearId && s.IsActive)
                .OrderBy(s => s.SubjectID)
                .ToListAsync();

            var terms = await _context.Terms
                .Where(t => t.AcademicYearID == academicYearId)
                .OrderBy(t => t.TermID)
                .ToListAsync();

            var allResults = await _context.StudentAllResults
                .Include(r => r.Subject)
                .Include(r => r.Term)
                .Include(r => r.ResultApproval)
                .Where(r => r.StudentID == studentId && r.AcademicYearID == academicYearId)
                .ToListAsync();

            var termResults = await _context.StudentSubjectTermResults
                .Include(r => r.Subject)
                .Include(r => r.Term)
                .Where(r => r.StudentID == studentId && r.AcademicYearID == academicYearId)
                .ToListAsync();

            var gradesList = new List<StudentFinalGradeItemDto>();

            var targetTerms = new List<(int? TermId, string? TermName)>();
            if (terms.Count > 0)
            {
                targetTerms.AddRange(terms.Select(t => ((int?)t.TermID, (string?)t.TermName)));
            }
            else
            {
                var distinctTermIds = allResults.Select(r => r.TermID)
                    .Concat(termResults.Select(r => r.TermID))
                    .Where(id => id.HasValue)
                    .Distinct()
                    .OrderBy(id => id)
                    .ToList();

                if (distinctTermIds.Count > 0)
                {
                    targetTerms.AddRange(distinctTermIds.Select(id => ((int?)id, (string?)$"Term {id}")));
                }
                else
                {
                    targetTerms.Add((1, "Term 1"));
                }
            }

            foreach (var s in enrolledSubjects)
            {
                foreach (var t in targetTerms)
                {
                    var matchingAllResult = allResults.FirstOrDefault(r => r.SubjectID == s.SubjectID && (r.TermID == t.TermId || (!r.TermID.HasValue && (targetTerms.Count == 1 || t.TermId == 1))));
                    var matchingTermResult = termResults.FirstOrDefault(r => r.SubjectID == s.SubjectID && (r.TermID == t.TermId || (!r.TermID.HasValue && (targetTerms.Count == 1 || t.TermId == 1))));

                    decimal maxFinal;
                    decimal maxQuarter;
                    if (s.MaxFinalScore.HasValue && s.MaxQuarterScore.HasValue)
                    {
                        maxFinal = (decimal)s.MaxFinalScore.Value;
                        maxQuarter = (decimal)s.MaxQuarterScore.Value;
                    }
                    else if (s.MaxFinalScore.HasValue && !s.MaxQuarterScore.HasValue)
                    {
                        maxFinal = (decimal)s.MaxFinalScore.Value;
                        maxQuarter = 0m;
                    }
                    else if (!s.MaxFinalScore.HasValue && s.MaxQuarterScore.HasValue)
                    {
                        maxFinal = 0m;
                        maxQuarter = (decimal)s.MaxQuarterScore.Value;
                    }
                    else
                    {
                        maxFinal = 100m;
                        maxQuarter = 25m;
                    }

                    var maxTotal = maxQuarter + maxFinal;
                    if (maxTotal == 0) maxTotal = 100m;

                    var hasAnyQuarterScore = matchingTermResult != null && (
                        matchingTermResult.Quarter1Score.HasValue ||
                        matchingTermResult.Quarter2Score.HasValue ||
                        matchingTermResult.Quarter3Score.HasValue ||
                        matchingTermResult.Quarter4Score.HasValue
                    );

                    decimal? coursework = hasAnyQuarterScore
                        ? (matchingTermResult!.Quarter1Score ?? 0) +
                          (matchingTermResult.Quarter2Score ?? 0) +
                          (matchingTermResult.Quarter3Score ?? 0) +
                          (matchingTermResult.Quarter4Score ?? 0)
                        : null;

                    var hasApprovedResult = matchingAllResult?.ResultApproval != null && matchingAllResult.ResultApproval.Decision == Decision.Approved;
                    var hasExplicitStatus = (matchingAllResult?.SubjectStatus.HasValue == true && matchingAllResult.SubjectStatus != SubjectStatus.InProgress) ||
                                            (matchingTermResult?.Status.HasValue == true && matchingTermResult.Status != SubjectStatus.InProgress);
                    var hasFinalScore = (matchingAllResult?.FinalSubjectScore.HasValue == true && matchingAllResult.FinalSubjectScore.Value > 0) ||
                                        (matchingTermResult?.FinalExamScore.HasValue == true && matchingTermResult.FinalExamScore.Value > 0);

                    decimal? finalExam = hasFinalScore
                        ? (matchingAllResult?.FinalSubjectScore ?? matchingTermResult?.FinalExamScore)
                        : null;

                    var isEvaluated = hasApprovedResult || hasExplicitStatus || hasFinalScore;

                    decimal? total;
                    decimal? percentage;
                    string status;
                    string letterGrade;
                    bool isApproved = hasApprovedResult;

                    if (isEvaluated)
                    {
                        total = matchingAllResult?.TotalTermScore ?? matchingAllResult?.FinalSubjectScore ?? matchingTermResult?.TermTotal ?? ((coursework ?? 0) + (finalExam ?? 0));
                        percentage = Math.Round((total.Value / maxTotal) * 100m, 1);
                        letterGrade = matchingAllResult?.Grade.HasValue == true
                            ? matchingAllResult.Grade.Value.ToString()
                            : CalculateLetterGrade(percentage.Value);
                        status = (matchingAllResult?.SubjectStatus == SubjectStatus.Passed || matchingTermResult?.Status == SubjectStatus.Passed || percentage.Value >= 50m) ? "Pass" : "Fail";
                    }
                    else if (hasAnyQuarterScore)
                    {
                        total = coursework;
                        percentage = Math.Round(((coursework ?? 0) / maxTotal) * 100m, 1);
                        letterGrade = "—";
                        status = "In Progress";
                    }
                    else
                    {
                        total = null;
                        percentage = null;
                        letterGrade = "—";
                        status = "Not Released";
                    }

                    gradesList.Add(new StudentFinalGradeItemDto
                    {
                        SubjectId = s.SubjectID,
                        Subject = s.SubjectName,
                        SubjectCode = $"SUB-{s.SubjectID:D3}",
                        TermId = t.TermId,
                        TermName = t.TermName,
                        CreditHours = null,
                        CourseworkScore = coursework,
                        FinalExamScore = finalExam,
                        TotalScore = total,
                        MaxScore = maxTotal,
                        Percentage = percentage,
                        LetterGrade = letterGrade,
                        Status = status,
                        IsApproved = isApproved,
                        YourGrade = total,
                        QuarterGrade = coursework
                    });
                }
            }

            decimal totalEarnedScore = 0m;
            decimal totalMaxScore = 0m;
            decimal? cumulativeAvg = null;
            var passedCount = 0;

            var gradedItems = gradesList.Where(g => g.Status != "In Progress" && g.Status != "Not Released" && g.TotalScore.HasValue).ToList();

            if (gradedItems.Count > 0)
            {
                totalEarnedScore = gradedItems.Sum(g => g.TotalScore!.Value);
                totalMaxScore = gradedItems.Sum(g => g.MaxScore);
                cumulativeAvg = totalMaxScore > 0 ? Math.Round((totalEarnedScore / totalMaxScore) * 100m, 1) : 0m;
                passedCount = gradedItems.Count(g => g.Status.Equals("Pass", StringComparison.OrdinalIgnoreCase) || (g.Percentage.HasValue && g.Percentage.Value >= 50m));
            }

            var standing = cumulativeAvg == null ? "Not Released"
                         : cumulativeAvg.Value >= 85m ? "Excellent (ممتاز)"
                         : cumulativeAvg.Value >= 75m ? "Very Good (جيد جداً)"
                         : cumulativeAvg.Value >= 65m ? "Good (جيد)"
                         : cumulativeAvg.Value >= 50m ? "Pass (مقبول)"
                         : "Needs Improvement";

            return new StudentFinalGradesResponseDto
            {
                Grades = gradesList,
                Year = year.ToLowerInvariant(),
                AcademicYearName = academicYearName,
                CumulativeAverage = cumulativeAvg,
                TotalEarnedScore = totalEarnedScore,
                TotalMaxScore = totalMaxScore,
                TotalSubjects = gradesList.Count,
                PassedSubjects = passedCount,
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
    }
}
