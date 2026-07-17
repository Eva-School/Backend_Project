using GradeManagementSystem.Core.Entities.Domain;
using GradeManagementSystem.Core.Entities.Enums;
using GradeManagementSystem.Core.Entities.Identity;
using GradeManagementSystem.Repository.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GradeManagementSystem.Api.Data
{
    public static class ViceGradesSeed
    {
        public static async Task SeedAsync(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<GradeDbContext>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();

            var studentRole = await roleManager.Roles.FirstOrDefaultAsync(r => r.RoleName == "Student");
            if (studentRole == null)
            {
                throw new InvalidOperationException("Student role not found.");
            }

            var deptOm = await GetOrCreateDepartmentAsync(context, "OM", "OM Department", isActive: true);
            var deptSd = await GetOrCreateDepartmentAsync(context, "SD", "SD Department", isActive: true);

            // Ensure we have terms for each active academic year: Term 1 and Term 2.
            var activeYears = await context.AcademicYears.Where(y => y.IsActive).ToListAsync();
            foreach (var year in activeYears)
            {
                await EnsureTermsForYearAsync(context, year.AcademicYearID);
            }

            // Ensure subjects exist for each active year.
            foreach (var year in activeYears)
            {
                await EnsureSubjectsForYearAsync(context, year.AcademicYearID);
            }

            // Ensure classes and students for OM/SD.
            var seedStudentsByYear = await SeedStudentsForYearAndDepartmentsAsync(context, userManager, studentRole.RoleId, activeYears, deptOm.DepartmentID, deptSd.DepartmentID);

            // Ensure quarter results, quarter submissions, and quarter locks.
            await SeedQuarterResultsAsync(context, seedStudentsByYear);

            // Ensure final results, approvals and action logs.
            await SeedFinalResultsAndApprovalsAsync(context, seedStudentsByYear);
            await SeedActionLogsAsync(context, seedStudentsByYear);
        }

        private static async Task<Department> GetOrCreateDepartmentAsync(GradeDbContext context, string name, string description, bool isActive)
        {
            var existing = await context.Departments.FirstOrDefaultAsync(d => d.IsActive && d.DepartmentName == name);
            if (existing != null)
            {
                return existing;
            }

            // If it exists but is inactive (or with old description), update it instead of inserting a duplicate.
            var anyExisting = await context.Departments.FirstOrDefaultAsync(d => d.DepartmentName == name);
            if (anyExisting != null)
            {
                anyExisting.IsActive = isActive;
                anyExisting.Description = description;
                await context.SaveChangesAsync();
                return anyExisting;
            }

            var dept = new Department
            {
                DepartmentName = name,
                Description = description,
                IsActive = isActive,
                CreatedAt = DateTime.UtcNow
            };
            context.Departments.Add(dept);
            await context.SaveChangesAsync();
            return dept;
        }

        private static async Task EnsureTermsForYearAsync(GradeDbContext context, int academicYearId)
        {
            var terms = await context.Terms
                .Where(t => t.AcademicYearID == academicYearId)
                .OrderBy(t => t.TermID)
                .ToListAsync();

            if (!terms.Any(t => t.TermName == "Term 1"))
            {
                context.Terms.Add(new Term
                {
                    AcademicYearID = academicYearId,
                    TermName = "Term 1",
                    StartDate = new DateTime(DateTime.UtcNow.Year, 9, 1),
                    EndDate = new DateTime(DateTime.UtcNow.Year + 1, 1, 31)
                });
            }

            if (!terms.Any(t => t.TermName == "Term 2"))
            {
                context.Terms.Add(new Term
                {
                    AcademicYearID = academicYearId,
                    TermName = "Term 2",
                    StartDate = new DateTime(DateTime.UtcNow.Year + 1, 2, 1),
                    EndDate = new DateTime(DateTime.UtcNow.Year + 1, 6, 30)
                });
            }

            await context.SaveChangesAsync();
        }

        private static async Task EnsureSubjectsForYearAsync(GradeDbContext context, int academicYearId)
        {
            var hasAny = await context.Subjects.AnyAsync(s => s.IsActive && s.AcademicYearID == academicYearId);
            if (hasAny)
            {
                // Ensure MaxQuarterQ fields are populated for existing subjects.
                var subjects = await context.Subjects.Where(s => s.IsActive && s.AcademicYearID == academicYearId).ToListAsync();
                foreach (var s in subjects)
                {
                    if (s.MaxQuarterQ1Score == null || s.MaxQuarterQ2Score == null || s.MaxQuarterQ3Score == null || s.MaxQuarterQ4Score == null)
                    {
                        s.MaxQuarterQ1Score = 12;
                        s.MaxQuarterQ2Score = 13;
                        s.MaxQuarterQ3Score = 12;
                        s.MaxQuarterQ4Score = 13;
                        s.MaxQuarterScore = 25;
                    }
                }
                await context.SaveChangesAsync();
                return;
            }

            var math = new Subject
            {
                SubjectName = "Mathematics",
                AcademicYearID = academicYearId,
                IsActive = true,
                MaxFinalScore = 100,
                MaxQuarterScore = 25,
                MaxQuarterQ1Score = 12,
                MaxQuarterQ2Score = 13,
                MaxQuarterQ3Score = 12,
                MaxQuarterQ4Score = 13
            };

            var eng = new Subject
            {
                SubjectName = "English",
                AcademicYearID = academicYearId,
                IsActive = true,
                MaxFinalScore = 100,
                MaxQuarterScore = 25,
                MaxQuarterQ1Score = 12,
                MaxQuarterQ2Score = 13,
                MaxQuarterQ3Score = 12,
                MaxQuarterQ4Score = 13
            };

            context.Subjects.AddRange(math, eng);
            await context.SaveChangesAsync();
        }

        private static async Task<Dictionary<int, List<(Student student, ApplicationUser user, Class cls)>>> SeedStudentsForYearAndDepartmentsAsync(
            GradeDbContext context,
            UserManager<ApplicationUser> userManager,
            int studentRoleId,
            List<AcademicYear> activeYears,
            int deptOmId,
            int deptSdId)
        {
            var result = new Dictionary<int, List<(Student student, ApplicationUser user, Class cls)>>();

            foreach (var year in activeYears)
            {
                // Create 2 classes per year (OM + SD) and 2 students per class.
                var classNameOm = $"{GetYearToken(year.Stage)}-{deptOmId}-1";
                var classNameSd = $"{GetYearToken(year.Stage)}-{deptSdId}-1";

                var classOm = await GetOrCreateClassAsync(context, classNameOm, year.AcademicYearID, deptOmId, capacity: 30);
                var classSd = await GetOrCreateClassAsync(context, classNameSd, year.AcademicYearID, deptSdId, capacity: 30);

                var list = new List<(Student student, ApplicationUser user, Class cls)>();

                list.Add(await GetOrCreateStudentAsync(context, userManager, studentRoleId,
                    firstName: "Ahmed",
                    middleName: "M",
                    lastName: "Ali",
                    studentCode: $"{year.AcademicYearID}001",
                    email: $"s1_{year.AcademicYearID}@system.com",
                    phone: "01000000001",
                    yearStage: year.Stage,
                    classId: classOm.ClassID,
                    gender: Gender.Male));

                list.Add(await GetOrCreateStudentAsync(context, userManager, studentRoleId,
                    firstName: "Omar",
                    middleName: null,
                    lastName: "Hassan",
                    studentCode: $"{year.AcademicYearID}002",
                    email: $"s2_{year.AcademicYearID}@system.com",
                    phone: "01000000002",
                    yearStage: year.Stage,
                    classId: classOm.ClassID,
                    gender: Gender.Male));

                list.Add(await GetOrCreateStudentAsync(context, userManager, studentRoleId,
                    firstName: "Yousef",
                    middleName: "K",
                    lastName: "Saleh",
                    studentCode: $"{year.AcademicYearID}101",
                    email: $"s3_{year.AcademicYearID}@system.com",
                    phone: "01000000003",
                    yearStage: year.Stage,
                    classId: classSd.ClassID,
                    gender: Gender.Male));

                list.Add(await GetOrCreateStudentAsync(context, userManager, studentRoleId,
                    firstName: "Sara",
                    middleName: null,
                    lastName: "Nasser",
                    studentCode: $"{year.AcademicYearID}102",
                    email: $"s4_{year.AcademicYearID}@system.com",
                    phone: "01000000004",
                    yearStage: year.Stage,
                    classId: classSd.ClassID,
                    gender: Gender.Female));

                result[year.AcademicYearID] = list;
            }

            return result;
        }

        private static async Task<(Student student, ApplicationUser user, Class cls)> GetOrCreateStudentAsync(
            GradeDbContext context,
            UserManager<ApplicationUser> userManager,
            int studentRoleId,
            string firstName,
            string? middleName,
            string lastName,
            string studentCode,
            string email,
            string phone,
            EducationStage yearStage,
            int classId,
            Gender gender)
        {
            var userExisting = await userManager.Users.FirstOrDefaultAsync(u => u.Email == email || u.UserName == studentCode);
            ApplicationUser user;

            if (userExisting != null)
            {
                user = userExisting;
            }
            else
            {
                user = new ApplicationUser
                {
                    UserName = studentCode,
                    Email = email,
                    FirstName = firstName,
                    MiddleName = middleName,
                    LastName = lastName,
                    FullName = $"{firstName} {(string.IsNullOrWhiteSpace(middleName) ? "" : middleName + " ")}{lastName}",
                    PhoneNumber = phone,
                    RoleId = studentRoleId,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    EmailConfirmed = true
                };

                var created = await userManager.CreateAsync(user, "Student@123");
                if (!created.Succeeded)
                {
                    throw new InvalidOperationException("Unable to create student user in seed.");
                }
            }

            var studentExisting = await context.Students.FirstOrDefaultAsync(s => s.NationalID == studentCode);
            if (studentExisting != null)
            {
                return (studentExisting, user, await context.Classes.FirstAsync(c => c.ClassID == classId));
            }

            var student = new Student
            {
                UserID = user.UserId,
                NationalID = studentCode,
                EnrollmentDate = DateTime.UtcNow.Date,
                CurrentAcademicYearID = await context.AcademicYears.Where(a => a.IsActive && a.Stage == yearStage).OrderByDescending(a => a.AcademicYearID).Select(a => a.AcademicYearID).FirstOrDefaultAsync(),
                MajorID = null,
                ClassID = classId,
                Status = "Active",
                Gender = gender
            };

            context.Students.Add(student);
            await context.SaveChangesAsync();

            var cls = await context.Classes.FirstAsync(c => c.ClassID == classId);
            return (student, user, cls);
        }

        private static async Task<Class> GetOrCreateClassAsync(GradeDbContext context, string className, int academicYearId, int departmentId, int capacity)
        {
            var existing = await context.Classes.FirstOrDefaultAsync(c => c.IsActive && c.ClassName == className);
            if (existing != null)
            {
                // Ensure department and year are set (older seed might have null DepartmentID).
                existing.AcademicYearID = academicYearId;
                existing.DepartmentID = departmentId;
                existing.Capacity = capacity;
                await context.SaveChangesAsync();
                return existing;
            }

            var cls = new Class
            {
                ClassName = className,
                AcademicYearID = academicYearId,
                DepartmentID = departmentId,
                Capacity = capacity,
                IsActive = true
            };
            context.Classes.Add(cls);
            await context.SaveChangesAsync();
            return cls;
        }

        private static string GetYearToken(EducationStage stage)
        {
            return stage.ToString().ToLowerInvariant();
        }

        private static async Task SeedQuarterResultsAsync(
            GradeDbContext context,
            Dictionary<int, List<(Student student, ApplicationUser user, Class cls)>> seedStudentsByYear)
        {
            var now = DateTime.UtcNow;

            foreach (var kvp in seedStudentsByYear)
            {
                var academicYearId = kvp.Key;
                var students = kvp.Value;

                var terms = await context.Terms
                    .AsNoTracking()
                    .Where(t => t.AcademicYearID == academicYearId)
                    .OrderBy(t => t.TermID)
                    .Select(t => new { t.TermID, t.TermName })
                    .ToListAsync();

                if (terms.Count < 2)
                {
                    continue;
                }

                var term1 = terms[0].TermID;
                var term2 = terms[1].TermID;

                var subjects = await context.Subjects
                    .AsNoTracking()
                    .Where(s => s.IsActive && s.AcademicYearID == academicYearId)
                    .ToListAsync();

                foreach (var sub in subjects)
                {
                    int q1Max = sub.MaxQuarterQ1Score ?? 12;
                    int q2Max = sub.MaxQuarterQ2Score ?? 13;
                    int q3Max = sub.MaxQuarterQ3Score ?? 12;
                    int q4Max = sub.MaxQuarterQ4Score ?? 13;

                    foreach (var tuple in students)
                    {
                        var st = tuple.student;
                        var classId = tuple.cls.ClassID;
                        var deptId = tuple.cls.DepartmentID ?? 0;

                        // term1 (q1/q2)
                        if (!await context.StudentSubjectTermResults.AnyAsync(r =>
                            r.StudentID == st.StudentID &&
                            r.SubjectID == sub.SubjectID &&
                            r.TermID == term1 &&
                            r.AcademicYearID == academicYearId))
                        {
                            context.StudentSubjectTermResults.Add(new StudentSubjectTermResult
                            {
                                StudentID = st.StudentID,
                                SubjectID = sub.SubjectID,
                                TermID = term1,
                                AcademicYearID = academicYearId,
                                Quarter1Score = (decimal)q1Max,
                                Quarter2Score = (decimal)q2Max,
                                Quarter3Score = null,
                                Quarter4Score = null,
                                FinalExamScore = null,
                                TermTotal = (decimal)(q1Max + q2Max),
                                Status = SubjectStatus.InProgress,
                                CreatedAt = now
                            });
                        }

                        // term2 (q3/q4)
                        if (!await context.StudentSubjectTermResults.AnyAsync(r =>
                            r.StudentID == st.StudentID &&
                            r.SubjectID == sub.SubjectID &&
                            r.TermID == term2 &&
                            r.AcademicYearID == academicYearId))
                        {
                            context.StudentSubjectTermResults.Add(new StudentSubjectTermResult
                            {
                                StudentID = st.StudentID,
                                SubjectID = sub.SubjectID,
                                TermID = term2,
                                AcademicYearID = academicYearId,
                                Quarter1Score = null,
                                Quarter2Score = null,
                                Quarter3Score = (decimal)q3Max,
                                Quarter4Score = (decimal)q4Max,
                                FinalExamScore = null,
                                TermTotal = (decimal)(q3Max + q4Max),
                                Status = SubjectStatus.InProgress,
                                CreatedAt = now
                            });
                        }

                        // Quarter submissions: seed for some students (to make dashboard pending meaningful).
                        if (st.StudentID % 2 == 0)
                        {
                            await EnsureQuarterSubmissionAsync(context, st.StudentID, sub.SubjectID, academicYearId, term1, now);
                            if (term2 != term1)
                            {
                                await EnsureQuarterSubmissionAsync(context, st.StudentID, sub.SubjectID, academicYearId, term2, now);
                            }
                        }
                    }
                }

                // Quarter lock: lock OM + primary subject for the first OM class in this year.
                var omClass = students.FirstOrDefault(s => s.cls.DepartmentID == seedStudentsByYear.First().Value.First().cls.DepartmentID);
                // More deterministic: take first student class with DepartmentID of first student (OM) and lock.
                var lockStudent = students.FirstOrDefault();
                if (lockStudent.student != null)
                {
                    var primarySubjectId = await context.Subjects
                        .AsNoTracking()
                        .Where(s => s.IsActive && s.AcademicYearID == academicYearId)
                        .OrderBy(s => s.SubjectID)
                        .Select(s => s.SubjectID)
                        .FirstOrDefaultAsync();

                    if (primarySubjectId != 0)
                    {
                        var lockExists = await context.QuarterGradesLocks.AnyAsync(l =>
                            l.AcademicYearID == academicYearId &&
                            l.SubjectID == primarySubjectId &&
                            l.DepartmentID == (lockStudent.cls.DepartmentID ?? 0) &&
                            l.ClassID == lockStudent.cls.ClassID);

                        if (!lockExists)
                        {
                            context.QuarterGradesLocks.Add(new QuarterGradesLock
                            {
                                AcademicYearID = academicYearId,
                                SubjectID = primarySubjectId,
                                DepartmentID = lockStudent.cls.DepartmentID ?? 0,
                                ClassID = lockStudent.cls.ClassID,
                                LockedAt = now,
                                LockedBy = null
                            });
                        }
                    }
                }

                await context.SaveChangesAsync();
            }
        }

        private static async Task EnsureQuarterSubmissionAsync(GradeDbContext context, int studentId, int subjectId, int academicYearId, int termId, DateTime submittedAt)
        {
            var existing = await context.QuarterGradeSubmissions.FirstOrDefaultAsync(s =>
                s.StudentID == studentId &&
                s.SubjectID == subjectId &&
                s.AcademicYearID == academicYearId &&
                s.TermID == termId);

            if (existing != null)
            {
                existing.SubmittedAt = submittedAt;
                return;
            }

            context.QuarterGradeSubmissions.Add(new QuarterGradeSubmission
            {
                StudentID = studentId,
                SubjectID = subjectId,
                AcademicYearID = academicYearId,
                TermID = termId,
                SubmittedAt = submittedAt,
                SubmittedBy = null
            });
        }

        private static async Task SeedFinalResultsAndApprovalsAsync(
            GradeDbContext context,
            Dictionary<int, List<(Student student, ApplicationUser user, Class cls)>> seedStudentsByYear)
        {
            var now = DateTime.UtcNow;
            foreach (var kvp in seedStudentsByYear)
            {
                var academicYearId = kvp.Key;
                var students = kvp.Value;

                var terms = await context.Terms
                    .AsNoTracking()
                    .Where(t => t.AcademicYearID == academicYearId)
                    .OrderBy(t => t.TermID)
                    .ToListAsync();

                if (terms.Count < 1)
                {
                    continue;
                }

                var term1 = terms[0].TermID;
                var term2 = terms.Count > 1 ? terms[1].TermID : terms[0].TermID;

                var subjects = await context.Subjects
                    .AsNoTracking()
                    .Where(s => s.IsActive && s.AcademicYearID == academicYearId)
                    .ToListAsync();

                foreach (var sub in subjects)
                {
                    var maxFinal = sub.MaxFinalScore ?? 100m;
                    var passThreshold = maxFinal / 2m;

                    foreach (var (student, _, cls) in students)
                    {
                        var deptId = cls.DepartmentID ?? 0;

                        foreach (var (termId, scoreBase) in new[]
                        {
                            (term1, 80m),
                            (term2, 70m)
                        })
                        {
                            var baseScore = scoreBase + (student.StudentID % 10);
                            var status = baseScore >= passThreshold ? SubjectStatus.Passed : SubjectStatus.Failed;
                            var overall = baseScore >= passThreshold ? OverallTermStatus.Passed : OverallTermStatus.Failed;

                            var allRes = await context.StudentAllResults.FirstOrDefaultAsync(r =>
                                r.StudentID == student.StudentID &&
                                r.SubjectID == sub.SubjectID &&
                                r.TermID == termId &&
                                r.AcademicYearID == academicYearId);

                            if (allRes == null)
                            {
                                allRes = new StudentAllResults
                                {
                                    StudentID = student.StudentID,
                                    SubjectID = sub.SubjectID,
                                    TermID = termId,
                                    AcademicYearID = academicYearId,
                                    FinalSubjectScore = baseScore,
                                    TotalTermScore = baseScore,
                                    SubjectStatus = status,
                                    OverallTermStatus = overall,
                                    Grade = baseScore >= passThreshold ? GradeLevel.Pass : GradeLevel.Fail,
                                    GeneratedAt = now
                                };
                                context.StudentAllResults.Add(allRes);
                                await context.SaveChangesAsync();
                            }

                            // Approvals: create different states per student.
                            var decision = student.StudentID % 3 == 0 ? (Decision?)null : (student.StudentID % 3 == 1 ? Decision.Pending : Decision.Approved);

                            var approval = await context.ResultApprovals.FirstOrDefaultAsync(a => a.AllResultID == allRes.AllResultID);

                            if (decision == null)
                            {
                                // draft: no approval row
                                if (approval != null)
                                {
                                    context.ResultApprovals.Remove(approval);
                                }
                            }
                            else if (approval == null)
                            {
                                context.ResultApprovals.Add(new ResultApproval
                                {
                                    AllResultID = allRes.AllResultID,
                                    Decision = decision.Value,
                                    Notes = "",
                                    ApprovedBy = null,
                                    ApprovalDate = decision.Value == Decision.Approved ? now : null
                                });
                            }
                            else
                            {
                                approval.Decision = decision.Value;
                                approval.ApprovalDate = decision.Value == Decision.Approved ? now : approval.ApprovalDate;
                            }
                        }
                    }
                }

                await context.SaveChangesAsync();
            }
        }

        private static async Task SeedActionLogsAsync(
            GradeManagementSystem.Repository.Data.GradeDbContext context,
            Dictionary<int, List<(Student student, ApplicationUser user, Class cls)>> seedStudentsByYear)
        {
            var now = DateTime.UtcNow;
            var logs = new List<GradeActionLog>();

            foreach (var kvp in seedStudentsByYear)
            {
                var academicYearId = kvp.Key;
                var year = await context.AcademicYears.FirstOrDefaultAsync(a => a.AcademicYearID == academicYearId);
                if (year == null) continue;

                var stageLabel = year.Stage.ToString().ToLowerInvariant();
                var primarySubjectId = await context.Subjects
                    .AsNoTracking()
                    .Where(s => s.IsActive && s.AcademicYearID == academicYearId)
                    .OrderBy(s => s.SubjectID)
                    .Select(s => s.SubjectID)
                    .FirstOrDefaultAsync();

                var primarySubject = await context.Subjects.FirstOrDefaultAsync(s => s.SubjectID == primarySubjectId);

                foreach (var (student, _, cls) in kvp.Value.Take(2))
                {
                    var baseIndex = logs.Count;

                    // Create multiple log entries per student.
                    logs.Add(new GradeActionLog
                    {
                        Action = "Submitted quarter grades",
                        ActorUserID = null,
                        ActorName = "Mr. Ahmed Ali",
                        StudentID = student.StudentID,
                        SubjectID = primarySubjectId,
                        AcademicYearID = academicYearId,
                        DepartmentID = cls.DepartmentID,
                        ClassID = cls.ClassID,
                        TermID = null,
                        Level = stageLabel,
                        SubjectName = primarySubject?.SubjectName,
                        ClassName = cls.ClassName,
                        BeforeFinalScore = null,
                        AfterFinalScore = null,
                        Timestamp = now.AddMinutes(-(baseIndex + 1) * 30)
                    });

                    logs.Add(new GradeActionLog
                    {
                        Action = "Updated final grades",
                        ActorUserID = null,
                        ActorName = "Mr. Ahmed Ali",
                        StudentID = student.StudentID,
                        SubjectID = primarySubjectId,
                        AcademicYearID = academicYearId,
                        DepartmentID = cls.DepartmentID,
                        ClassID = cls.ClassID,
                        TermID = null,
                        Level = stageLabel,
                        SubjectName = primarySubject?.SubjectName,
                        ClassName = cls.ClassName,
                        BeforeFinalScore = 60m,
                        AfterFinalScore = 85m,
                        Timestamp = now.AddMinutes(-(baseIndex + 2) * 30)
                    });

                    logs.Add(new GradeActionLog
                    {
                        Action = "Submitted final grades",
                        ActorUserID = null,
                        ActorName = "Mr. Ahmed Ali",
                        StudentID = student.StudentID,
                        SubjectID = primarySubjectId,
                        AcademicYearID = academicYearId,
                        DepartmentID = cls.DepartmentID,
                        ClassID = cls.ClassID,
                        TermID = null,
                        Level = stageLabel,
                        SubjectName = primarySubject?.SubjectName,
                        ClassName = cls.ClassName,
                        BeforeFinalScore = null,
                        AfterFinalScore = null,
                        Timestamp = now.AddMinutes(-(baseIndex + 3) * 30)
                    });
                }
            }

            // Ensure we don't duplicate too much by upserting by latest timestamps is hard; for seed, we just add when table is empty.
            var existingCount = await context.GradeActionLogs.CountAsync();
            if (existingCount == 0)
            {
                context.GradeActionLogs.AddRange(logs);
                await context.SaveChangesAsync();
            }
        }
    }
}
