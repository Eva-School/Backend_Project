using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GradeManagementSystem.Core.Entities.Domain;
using GradeManagementSystem.Core.Entities.Enums;
using GradeManagementSystem.Repository.Data;
using GradeManagementSystem.Services.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;
using Xunit.Abstractions;

namespace GradeManagementSystem.Tests
{
    public class DatabaseAuditTests
    {
        private readonly ITestOutputHelper _output;

        public DatabaseAuditTests(ITestOutputHelper output)
        {
            _output = output;
        }

        private GradeDbContext? CreateDbContext()
        {
            var raw = Environment.GetEnvironmentVariable("TEST_CONNECTION_STRING");
            if (string.IsNullOrWhiteSpace(raw))
            {
                return null;
            }

            var connectionString = PostgresConnectionParser.Parse(raw);
            var options = new DbContextOptionsBuilder<GradeDbContext>()
                .UseNpgsql(connectionString)
                .Options;

            return new GradeDbContext(options);
        }

        [Fact]
        public async Task Audit_Existing_User_Emails()
        {
            await using var context = CreateDbContext();
            if (context == null)
            {
                _output.WriteLine("SKIPPED: 'TEST_CONNECTION_STRING' environment variable is not set. Skipping live DB audit.");
                return;
            }
            var users = await context.Users
                .Select(u => new
                {
                    u.UserId,
                    u.UserName,
                    u.Email,
                    u.NormalizedEmail,
                    u.RoleId,
                    u.IsActive
                })
                .ToListAsync();

            _output.WriteLine($"Total users in DB: {users.Count}");

            var missingOrBlank = users.Where(u => string.IsNullOrWhiteSpace(u.Email)).ToList();
            var missingNormalized = users.Where(u => string.IsNullOrWhiteSpace(u.NormalizedEmail) && !string.IsNullOrWhiteSpace(u.Email)).ToList();
            var inconsistentNormalized = users.Where(u => !string.IsNullOrWhiteSpace(u.Email) && !string.IsNullOrWhiteSpace(u.NormalizedEmail) && u.NormalizedEmail != u.Email.Trim().ToUpperInvariant()).ToList();

            var duplicates = users
                .Where(u => !string.IsNullOrWhiteSpace(u.NormalizedEmail))
                .GroupBy(u => u.NormalizedEmail!)
                .Where(g => g.Count() > 1)
                .ToList();

            _output.WriteLine("=== AUDIT RESULTS ===");
            _output.WriteLine($"Users with missing/blank Email: {missingOrBlank.Count}");
            foreach (var u in missingOrBlank)
            {
                _output.WriteLine($"  [ID {u.UserId}] UserName='{u.UserName}', RoleId={u.RoleId}, IsActive={u.IsActive}");
            }

            _output.WriteLine($"Users with missing NormalizedEmail: {missingNormalized.Count}");
            foreach (var u in missingNormalized)
            {
                _output.WriteLine($"  [ID {u.UserId}] UserName='{u.UserName}', Email='{u.Email}'");
            }

            _output.WriteLine($"Users with inconsistent NormalizedEmail: {inconsistentNormalized.Count}");
            foreach (var u in inconsistentNormalized)
            {
                _output.WriteLine($"  [ID {u.UserId}] Email='{u.Email}', Normalized='{u.NormalizedEmail}'");
            }

            _output.WriteLine($"Duplicate NormalizedEmail groups: {duplicates.Count}");
            foreach (var g in duplicates)
            {
                _output.WriteLine($"  Duplicate key: '{g.Key}' ({g.Count()} accounts):");
                foreach (var u in g)
                {
                    _output.WriteLine($"    - UserID={u.UserId}, UserName='{u.UserName}', RoleId={u.RoleId}");
                }
            }

            foreach (var u in users)
            {
                _output.WriteLine($"USER: ID={u.UserId}, User='{u.UserName}', Email='{u.Email}', Norm='{u.NormalizedEmail}', Role={u.RoleId}, Active={u.IsActive}");
            }

            // Assert that there are no duplicate normalized emails currently in the database
            Assert.Empty(duplicates);
            Assert.Empty(missingOrBlank);
        }

        [Fact]
        public async Task Audit_Student_Records()
        {
            await using var context = CreateDbContext();
            if (context == null)
            {
                _output.WriteLine("SKIPPED: 'TEST_CONNECTION_STRING' not set.");
                return;
            }

            var years = await context.AcademicYears.ToListAsync();
            _output.WriteLine($"=== ACADEMIC YEARS ({years.Count}) ===");
            foreach (var y in years)
            {
                _output.WriteLine($"  ID={y.AcademicYearID}, Name='{y.YearName}', Stage={y.Stage}, IsActive={y.IsActive}");
            }

            var classes = await context.Classes.Include(c => c.AcademicYear).ToListAsync();
            _output.WriteLine($"=== CLASSES ({classes.Count}) ===");
            foreach (var c in classes)
            {
                _output.WriteLine($"  ID={c.ClassID}, Name='{c.ClassName}', YearID={c.AcademicYearID}, Stage={c.AcademicYear?.Stage.ToString()}");
            }

            var subjects = await context.Subjects.Include(s => s.AcademicYear).ToListAsync();
            _output.WriteLine($"=== SUBJECTS ({subjects.Count}) ===");
            foreach (var s in subjects)
            {
                _output.WriteLine($"  ID={s.SubjectID}, Name='{s.SubjectName}', YearID={s.AcademicYearID}, Stage={s.AcademicYear?.Stage.ToString()}, MaxQuarter={s.MaxQuarterScore}, MaxFinal={s.MaxFinalScore}");
            }

            var students = await context.Students.Include(s => s.Class).Include(s => s.CurrentAcademicYear).ToListAsync();
            _output.WriteLine($"=== STUDENTS ({students.Count}) ===");
            foreach (var st in students)
            {
                _output.WriteLine($"  StudentID={st.StudentID}, UserID={st.UserID}, Name='{st.NameEnglish}', Code='{st.StudentCode}', Class='{st.Class?.ClassName}' (ClassID={st.ClassID}), Year='{st.CurrentAcademicYear?.YearName}' (YearID={st.CurrentAcademicYearID}, Stage={st.CurrentAcademicYear?.Stage.ToString()})");
            }

            var termResults = await context.StudentSubjectTermResults.Include(r => r.Subject).Include(r => r.Term).ToListAsync();
            _output.WriteLine($"=== STUDENT SUBJECT TERM RESULTS ({termResults.Count}) ===");
            foreach (var tr in termResults)
            {
                _output.WriteLine($"  StudentID={tr.StudentID}, YearID={tr.AcademicYearID}, Subject='{tr.Subject?.SubjectName}' (ID={tr.SubjectID}), Term={tr.TermID}, Q1={tr.Quarter1Score}, Q2={tr.Quarter2Score}, Q3={tr.Quarter3Score}, Q4={tr.Quarter4Score}, FinalExam={tr.FinalExamScore}, TermTotal={tr.TermTotal}");
            }

            var allResults = await context.StudentAllResults.Include(r => r.Subject).ToListAsync();
            _output.WriteLine($"=== STUDENT ALL RESULTS ({allResults.Count}) ===");
            foreach (var ar in allResults)
            {
                _output.WriteLine($"  StudentID={ar.StudentID}, Subject='{ar.Subject?.SubjectName}' (ID={ar.SubjectID}), Term={ar.TermID}, Score={ar.FinalSubjectScore}, TotalTerm={ar.TotalTermScore}, Grade={ar.Grade}, Status={ar.SubjectStatus}");
            }

            var terms = await context.Terms.ToListAsync();
            _output.WriteLine($"=== TERMS ({terms.Count}) ===");
            foreach (var t in terms)
            {
                _output.WriteLine($"  TermID={t.TermID}, Name='{t.TermName}', YearID={t.AcademicYearID}");
            }

            var assignments = await context.TeacherAssignments.Include(ta => ta.Subject).Include(ta => ta.Class).ToListAsync();
            _output.WriteLine($"=== TEACHER ASSIGNMENTS ({assignments.Count}) ===");
            foreach (var ta in assignments)
            {
                _output.WriteLine($"  ID={ta.TeacherAssignmentID}, Class='{ta.Class?.ClassName}' (ClassID={ta.ClassID}), Subject='{ta.Subject?.SubjectName}' (SubjectID={ta.SubjectID}), TeacherID={ta.TeacherID}, Active={ta.IsActive}");
            }

            var comps = await context.Competencies.Include(c => c.Major).ToListAsync();
            _output.WriteLine($"=== COMPETENCIES ({comps.Count}) ===");
            foreach (var c in comps)
            {
                _output.WriteLine($"  ID={c.CompetencyID}, Name='{c.CompetencyName}', Major='{c.Major?.MajorName}' (MajorID={c.MajorID})");
            }

            var compStatuses = await context.StudentCompetencyStatuses.Include(s => s.Competency).ToListAsync();
            _output.WriteLine($"=== STUDENT COMPETENCY STATUSES ({compStatuses.Count}) ===");
            foreach (var cs in compStatuses)
            {
                _output.WriteLine($"  StudentID={cs.StudentID}, Competency='{cs.Competency?.CompetencyName}', Status={cs.StatusID}, Attempt={cs.CurrentAttemptNumber}");
            }

            var depts = await context.Departments.ToListAsync();
            _output.WriteLine($"=== DEPARTMENTS ({depts.Count}) ===");
            foreach (var d in depts)
            {
                _output.WriteLine($"  ID={d.DepartmentID}, Name='{d.DepartmentName}'");
            }

            var majors = await context.Majors.ToListAsync();
            _output.WriteLine($"=== MAJORS ({majors.Count}) ===");
            foreach (var m in majors)
            {
                _output.WriteLine($"  ID={m.MajorID}, Name='{m.MajorName}', DeptID={m.DepartmentID}");
            }
        }

        [Fact]
        public async Task Seed_Realistic_Student_Data()
        {
            await using var context = CreateDbContext();
            if (context == null)
            {
                _output.WriteLine("SKIPPED: 'TEST_CONNECTION_STRING' not set.");
                return;
            }

            var student = await context.Students
                .Include(s => s.Class)
                .Include(s => s.CurrentAcademicYear)
                .FirstOrDefaultAsync(s => s.StudentID == 2);

            Assert.NotNull(student);
            var studentId = student.StudentID;
            var yearId = student.CurrentAcademicYearID ?? 8;
            var classId = student.ClassID ?? 4;

            // 1. Update Subject 13 (Arabic) max scores
            var arabic = await context.Subjects.FindAsync(13);
            if (arabic != null)
            {
                arabic.MaxQuarterScore = 25;
                arabic.MaxQuarterQ1Score = 6;
                arabic.MaxQuarterQ2Score = 6;
                arabic.MaxQuarterQ3Score = 6;
                arabic.MaxQuarterQ4Score = 7;
                arabic.MaxFinalScore = 100;
            }

            // Ensure Teacher Assignments exist for Class 4
            var teacher = await context.Teachers.FirstOrDefaultAsync();
            var teacherId = teacher?.TeacherID ?? 1;

            var existingAssignments = await context.TeacherAssignments
                .Where(ta => ta.ClassID == classId)
                .ToListAsync();

            foreach (var subjectId in new[] { 11, 12, 13 })
            {
                if (!existingAssignments.Any(a => a.SubjectID == subjectId))
                {
                    context.TeacherAssignments.Add(new TeacherAssignment
                    {
                        TeacherID = teacherId,
                        ClassID = classId,
                        SubjectID = subjectId,
                        AcademicYearID = yearId,
                        IsActive = true,
                        AssignedAt = DateTime.UtcNow
                    });
                }
            }

            // 2. Clear old test entries for Student 2
            var oldTermResults = await context.StudentSubjectTermResults
                .Where(r => r.StudentID == studentId)
                .ToListAsync();
            context.StudentSubjectTermResults.RemoveRange(oldTermResults);

            var oldAllResults = await context.StudentAllResults
                .Include(r => r.ResultApproval)
                .Where(r => r.StudentID == studentId)
                .ToListAsync();
            foreach (var ar in oldAllResults)
            {
                if (ar.ResultApproval != null)
                {
                    context.ResultApprovals.Remove(ar.ResultApproval);
                }
            }
            context.StudentAllResults.RemoveRange(oldAllResults);

            await context.SaveChangesAsync();

            // 3. Seed realistic Term Results
            var term1Results = new[]
            {
                new StudentSubjectTermResult
                {
                    StudentID = studentId,
                    AcademicYearID = yearId,
                    SubjectID = 11, // Math
                    TermID = 11,
                    Quarter1Score = 5.5m,
                    Quarter2Score = 5.0m,
                    Quarter3Score = 5.5m,
                    Quarter4Score = 6.0m,
                    FinalExamScore = 88.0m,
                    TermTotal = 110.0m,
                    Status = SubjectStatus.Passed,
                    CreatedAt = DateTime.UtcNow,
                    LastUpdatedAt = DateTime.UtcNow
                },
                new StudentSubjectTermResult
                {
                    StudentID = studentId,
                    AcademicYearID = yearId,
                    SubjectID = 12, // English
                    TermID = 11,
                    Quarter1Score = 5.0m,
                    Quarter2Score = 5.5m,
                    Quarter3Score = 5.0m,
                    Quarter4Score = 6.5m,
                    FinalExamScore = 85.0m,
                    TermTotal = 107.0m,
                    Status = SubjectStatus.Passed,
                    CreatedAt = DateTime.UtcNow,
                    LastUpdatedAt = DateTime.UtcNow
                },
                new StudentSubjectTermResult
                {
                    StudentID = studentId,
                    AcademicYearID = yearId,
                    SubjectID = 13, // Arabic
                    TermID = 11,
                    Quarter1Score = 5.0m,
                    Quarter2Score = 5.0m,
                    Quarter3Score = 5.5m,
                    Quarter4Score = 6.0m,
                    FinalExamScore = 82.0m,
                    TermTotal = 103.5m,
                    Status = SubjectStatus.Passed,
                    CreatedAt = DateTime.UtcNow,
                    LastUpdatedAt = DateTime.UtcNow
                },
                // Term 2 (TermID = 12)
                new StudentSubjectTermResult
                {
                    StudentID = studentId,
                    AcademicYearID = yearId,
                    SubjectID = 11, // Math
                    TermID = 12,
                    Quarter1Score = 6.0m,
                    Quarter2Score = 5.5m,
                    Quarter3Score = 5.5m,
                    Quarter4Score = 6.5m,
                    FinalExamScore = 92.0m,
                    TermTotal = 115.5m,
                    Status = SubjectStatus.Passed,
                    CreatedAt = DateTime.UtcNow,
                    LastUpdatedAt = DateTime.UtcNow
                },
                new StudentSubjectTermResult
                {
                    StudentID = studentId,
                    AcademicYearID = yearId,
                    SubjectID = 12, // English
                    TermID = 12,
                    Quarter1Score = 5.5m,
                    Quarter2Score = 5.5m,
                    Quarter3Score = 6.0m,
                    Quarter4Score = 6.5m,
                    FinalExamScore = 88.0m,
                    TermTotal = 111.5m,
                    Status = SubjectStatus.Passed,
                    CreatedAt = DateTime.UtcNow,
                    LastUpdatedAt = DateTime.UtcNow
                },
                new StudentSubjectTermResult
                {
                    StudentID = studentId,
                    AcademicYearID = yearId,
                    SubjectID = 13, // Arabic
                    TermID = 12,
                    Quarter1Score = 5.0m,
                    Quarter2Score = 5.5m,
                    Quarter3Score = 5.0m,
                    Quarter4Score = 6.0m,
                    FinalExamScore = 85.0m,
                    TermTotal = 106.5m,
                    Status = SubjectStatus.Passed,
                    CreatedAt = DateTime.UtcNow,
                    LastUpdatedAt = DateTime.UtcNow
                }
            };
            context.StudentSubjectTermResults.AddRange(term1Results);

            // 4. Seed StudentAllResults & ResultApprovals
            var allResultSeeds = new[]
            {
                (SubjectId: 11, TermId: 11, FinalScore: 88.0m, TotalTerm: 110.0m, Grade: GradeLevel.EE),
                (SubjectId: 12, TermId: 11, FinalScore: 85.0m, TotalTerm: 107.0m, Grade: GradeLevel.ME),
                (SubjectId: 13, TermId: 11, FinalScore: 82.0m, TotalTerm: 103.5m, Grade: GradeLevel.GE),
                (SubjectId: 11, TermId: 12, FinalScore: 92.0m, TotalTerm: 115.5m, Grade: GradeLevel.EE),
                (SubjectId: 12, TermId: 12, FinalScore: 88.0m, TotalTerm: 111.5m, Grade: GradeLevel.EE),
                (SubjectId: 13, TermId: 12, FinalScore: 85.0m, TotalTerm: 106.5m, Grade: GradeLevel.ME),
            };

            foreach (var item in allResultSeeds)
            {
                var ar = new StudentAllResults
                {
                    StudentID = studentId,
                    AcademicYearID = yearId,
                    SubjectID = item.SubjectId,
                    TermID = item.TermId,
                    FinalSubjectScore = item.FinalScore,
                    TotalTermScore = item.TotalTerm,
                    SubjectStatus = SubjectStatus.Passed,
                    OverallTermStatus = OverallTermStatus.Passed,
                    Grade = item.Grade,
                    GeneratedAt = DateTime.UtcNow,
                    ApprovedAt = DateTime.UtcNow
                };
                context.StudentAllResults.Add(ar);
                await context.SaveChangesAsync();

                context.ResultApprovals.Add(new ResultApproval
                {
                    AllResultID = ar.AllResultID,
                    Decision = Decision.Approved,
                    Notes = "Approved by Academic Committee",
                    ApprovalDate = DateTime.UtcNow,
                    ApprovedBy = 1
                });
            }

            // 5. Seed Quizzes & QuizGrades
            var existingQuizzes = await context.Quizzes.Where(q => q.ClassID == classId).ToListAsync();
            var quiz1 = existingQuizzes.FirstOrDefault(q => q.SubjectID == 11 && q.Title == "Algebra & Functions Quiz")
                ?? new Quiz
                {
                    ClassID = classId,
                    SubjectID = 11,
                    AcademicYearID = yearId,
                    CreatedByTeacherID = teacherId,
                    Title = "Algebra & Functions Quiz",
                    MaxScore = 10,
                    QuizDate = DateTime.UtcNow.AddDays(-14),
                    CreatedAt = DateTime.UtcNow.AddDays(-14)
                };
            if (quiz1.QuizID == 0) { context.Quizzes.Add(quiz1); await context.SaveChangesAsync(); }

            var quiz2 = existingQuizzes.FirstOrDefault(q => q.SubjectID == 11 && q.Title == "Calculus Foundations Quiz")
                ?? new Quiz
                {
                    ClassID = classId,
                    SubjectID = 11,
                    AcademicYearID = yearId,
                    CreatedByTeacherID = teacherId,
                    Title = "Calculus Foundations Quiz",
                    MaxScore = 15,
                    QuizDate = DateTime.UtcNow.AddDays(-7),
                    CreatedAt = DateTime.UtcNow.AddDays(-7)
                };
            if (quiz2.QuizID == 0) { context.Quizzes.Add(quiz2); await context.SaveChangesAsync(); }

            var quiz3 = existingQuizzes.FirstOrDefault(q => q.SubjectID == 12 && q.Title == "Vocabulary & Reading Comprehension")
                ?? new Quiz
                {
                    ClassID = classId,
                    SubjectID = 12,
                    AcademicYearID = yearId,
                    CreatedByTeacherID = teacherId,
                    Title = "Vocabulary & Reading Comprehension",
                    MaxScore = 10,
                    QuizDate = DateTime.UtcNow.AddDays(-12),
                    CreatedAt = DateTime.UtcNow.AddDays(-12)
                };
            if (quiz3.QuizID == 0) { context.Quizzes.Add(quiz3); await context.SaveChangesAsync(); }

            var quiz4 = existingQuizzes.FirstOrDefault(q => q.SubjectID == 13 && q.Title == "النحو والقواعد التطبيقية")
                ?? new Quiz
                {
                    ClassID = classId,
                    SubjectID = 13,
                    AcademicYearID = yearId,
                    CreatedByTeacherID = teacherId,
                    Title = "النحو والقواعد التطبيقية",
                    MaxScore = 10,
                    QuizDate = DateTime.UtcNow.AddDays(-10),
                    CreatedAt = DateTime.UtcNow.AddDays(-10)
                };
            if (quiz4.QuizID == 0) { context.Quizzes.Add(quiz4); await context.SaveChangesAsync(); }

            // Ensure quiz grades for student 2
            var existingQuizGrades = await context.QuizGrades.Where(qg => qg.StudentID == studentId).ToListAsync();
            context.QuizGrades.RemoveRange(existingQuizGrades);

            context.QuizGrades.AddRange(new[]
            {
                new QuizGrade { QuizID = quiz1.QuizID, StudentID = studentId, Score = 9.5m, Notes = "Excellent analytical skills", GradedAt = DateTime.UtcNow.AddDays(-13) },
                new QuizGrade { QuizID = quiz2.QuizID, StudentID = studentId, Score = 14.0m, Notes = "Solid step-by-step reasoning", GradedAt = DateTime.UtcNow.AddDays(-6) },
                new QuizGrade { QuizID = quiz3.QuizID, StudentID = studentId, Score = 9.0m, Notes = "High lexical comprehension", GradedAt = DateTime.UtcNow.AddDays(-11) },
                new QuizGrade { QuizID = quiz4.QuizID, StudentID = studentId, Score = 8.5m, Notes = "مستوى متميز وإتقان للقواعد", GradedAt = DateTime.UtcNow.AddDays(-9) }
            });

            // 6. Seed Competencies
            var majorId = student.MajorID;
            if (!majorId.HasValue)
            {
                var defaultMajor = await context.Majors.FirstOrDefaultAsync();
                majorId = defaultMajor?.MajorID;
                student.MajorID = majorId;
            }

            var competencies = await context.Competencies.ToListAsync();
            if (competencies.Count == 0)
            {
                var comp1 = new Competency { CompetencyName = "Critical Thinking & Complex Problem Solving", MajorID = majorId, MaxAttempts = 3, IsActive = true, CreatedAt = DateTime.UtcNow };
                var comp2 = new Competency { CompetencyName = "Technical Communication & Scientific Presentation", MajorID = majorId, MaxAttempts = 3, IsActive = true, CreatedAt = DateTime.UtcNow };
                var comp3 = new Competency { CompetencyName = "Applied Mathematics & Computational Analysis", MajorID = majorId, MaxAttempts = 3, IsActive = true, CreatedAt = DateTime.UtcNow };
                var comp4 = new Competency { CompetencyName = "Collaborative Engineering & Professional Ethics", MajorID = majorId, MaxAttempts = 3, IsActive = true, CreatedAt = DateTime.UtcNow };

                context.Competencies.AddRange(comp1, comp2, comp3, comp4);
                await context.SaveChangesAsync();
                competencies = await context.Competencies.ToListAsync();
            }

            // Competency statuses for student
            var existingCompStatuses = await context.StudentCompetencyStatuses
                .Include(s => s.CompetencyAttempts)
                .Where(s => s.StudentID == studentId)
                .ToListAsync();

            foreach (var cs in existingCompStatuses)
            {
                context.CompetencyAttempts.RemoveRange(cs.CompetencyAttempts);
            }
            context.StudentCompetencyStatuses.RemoveRange(existingCompStatuses);
            await context.SaveChangesAsync();

            foreach (var comp in competencies)
            {
                var compStatus = new StudentCompetencyStatus
                {
                    StudentID = studentId,
                    CompetencyID = comp.CompetencyID,
                    StatusID = "Competent",
                    CurrentAttemptNumber = 1,
                    MaxAllowedAttempts = 3,
                    LastEvaluatedAt = DateTime.UtcNow.AddDays(-5)
                };
                context.StudentCompetencyStatuses.Add(compStatus);
                await context.SaveChangesAsync();

                context.CompetencyAttempts.Add(new CompetencyAttempt
                {
                    StudentCompetencyStatusID = compStatus.StudentCompetencyStatusID,
                    StudentID = studentId,
                    AttemptNumber = 1,
                    Result = "Competent",
                    EvaluatedBy = teacherId,
                    EvaluatedAt = DateTime.UtcNow.AddDays(-5)
                });
            }

            await context.SaveChangesAsync();
            _output.WriteLine("SUCCESS: Seeded realistic student data successfully!");
        }

        [Fact]
        public async Task Verify_Student_Dashboard_Endpoints()
        {
            await using var context = CreateDbContext();
            if (context == null)
            {
                _output.WriteLine("SKIPPED: 'TEST_CONNECTION_STRING' not set.");
                return;
            }

            var service = new StudentDashboardService(context);

            _output.WriteLine("=== 1. VERIFY FINAL GRADES (UserId=6, Senior) ===");
            var finalGrades = await service.GetFinalGradesAsync(6, "senior");
            Assert.NotNull(finalGrades);
            _output.WriteLine($"Standing: '{finalGrades.Standing}'");
            _output.WriteLine($"CumulativeAverage: {finalGrades.CumulativeAverage}%");
            _output.WriteLine($"TotalEarnedScore: {finalGrades.TotalEarnedScore} / {finalGrades.TotalMaxScore}");
            _output.WriteLine($"PassedSubjects: {finalGrades.PassedSubjects} / {finalGrades.TotalSubjects}");
            _output.WriteLine($"Grades Count: {finalGrades.Grades.Count}");
            foreach (var g in finalGrades.Grades)
            {
                _output.WriteLine($"  Subject='{g.Subject}' ({g.SubjectCode}), Term='{g.TermName}', Coursework={g.CourseworkScore}, Final={g.FinalExamScore}, Total={g.TotalScore}/{g.MaxScore} ({g.Percentage}%), Letter={g.LetterGrade}, Status={g.Status}, Approved={g.IsApproved}");
            }

            // Assertions on final grades
            Assert.DoesNotContain("دور ثان", finalGrades.Standing);
            Assert.DoesNotContain("راسب", finalGrades.Standing);
            Assert.Equal(6, finalGrades.Grades.Count);
            Assert.Equal(6, finalGrades.PassedSubjects);
            Assert.True(finalGrades.CumulativeAverage > 75m);

            _output.WriteLine("=== 2. VERIFY QUARTER GRADES (UserId=6, Senior, Term=11) ===");
            var quarterGrades = await service.GetQuarterGradesAsync(6, "senior", 11);
            Assert.NotNull(quarterGrades);
            _output.WriteLine($"SelectedTerm: {quarterGrades.SelectedTerm}");
            _output.WriteLine($"Grades Count: {quarterGrades.Grades.Count}");
            foreach (var q in quarterGrades.Grades)
            {
                _output.WriteLine($"  Subject='{q.Subject}', Q1={q.Quarter1}, Q2={q.Quarter2}, Q3={q.Quarter3}, Q4={q.Quarter4}, CourseworkTotal={q.CourseworkTotal}/{q.MaxQuarter}, Quizzes={q.Quizzes.Count}");
            }

            Assert.Equal(3, quarterGrades.Grades.Count);

            _output.WriteLine("=== 3. VERIFY JADARAT (UserId=6, Senior) ===");
            var jadarat = await service.GetJadaratGradesAsync(6, "senior");
            Assert.NotNull(jadarat);
            _output.WriteLine($"Total: {jadarat.TotalCompetencies}, Passed: {jadarat.PassedCompetencies}, Pending: {jadarat.PendingCompetencies}");
            foreach (var j in jadarat.Grades)
            {
                _output.WriteLine($"  Competency='{j.Jadarat}', Status='{j.CurrentStatus}', Attempts='{j.Attemps}', Evaluator='{j.EvaluatorName}'");
            }

            Assert.True(jadarat.TotalCompetencies >= 4);
            Assert.Equal(jadarat.TotalCompetencies, jadarat.PassedCompetencies);

            _output.WriteLine("=== 4. VERIFY PROGRESS (UserId=6, Senior) ===");
            var progress = await service.GetProgressAsync(6, "senior");
            Assert.NotNull(progress);
            foreach (var p in progress)
            {
                _output.WriteLine($"  Progress: Subject='{p.Subject}', QAvg={p.QuarterAverage}, Final={p.FinalExam}");
            }

            _output.WriteLine("=== 5. VERIFY REPORT (UserId=6, Senior) ===");
            var report = await service.GetReportAsync(6, "senior");
            Assert.NotNull(report);
            _output.WriteLine($"StudentName: '{report.StudentName}', StudentId: '{report.StudentId}', Class: '{report.ClassName}', Year: '{report.Year}', GradesCount: {report.Grades.Count}");

            _output.WriteLine("=== 5. VERIFY CARDS ===");
            var cards = await service.GetCardsAsync();
            Assert.NotNull(cards);
            foreach (var c in cards)
            {
                _output.WriteLine($"  Card: ID='{c.Id}', Title='{c.Title}', Route='{c.Route}'");
            }
        }
    }
}
