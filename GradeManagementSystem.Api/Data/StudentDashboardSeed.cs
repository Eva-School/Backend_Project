using GradeManagementSystem.Core.Entities.Domain;
using GradeManagementSystem.Core.Entities.Enums;
using GradeManagementSystem.Core.Entities.Identity;
using GradeManagementSystem.Repository.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GradeManagementSystem.Api.Data
{
    public static class StudentDashboardSeed
    {
        public static async Task SeedAsync(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<GradeDbContext>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            await context.Database.MigrateAsync();

            var major = await EnsureMajorAsync(context);
            var competency = await EnsureCompetencyAsync(context, major.MajorID);
            var studentUser = await EnsureStudentUserAsync(userManager);
            var student = await EnsureStudentAsync(context, studentUser.UserId, major.MajorID);
            await EnsureTermAsync(context);
            await EnsureSubjectResultsAsync(context, student.StudentID);
            await EnsureCompetencyStatusAsync(context, student.StudentID, competency.CompetencyID);
        }

        private static async Task<Major> EnsureMajorAsync(GradeDbContext context)
        {
            var major = await context.Majors.FirstOrDefaultAsync(m => m.MajorName == "General Track");
            if (major != null)
            {
                return major;
            }

            major = new Major
            {
                MajorName = "General Track",
                DepartmentID = 1,
                Description = "General academic track",
                IsActive = true
            };

            context.Majors.Add(major);
            await context.SaveChangesAsync();
            return major;
        }

        private static async Task<Competency> EnsureCompetencyAsync(GradeDbContext context, int majorId)
        {
            var competency = await context.Competencies.FirstOrDefaultAsync(c => c.CompetencyName == "API");
            if (competency != null)
            {
                return competency;
            }

            competency = new Competency
            {
                CompetencyName = "API",
                MajorID = majorId,
                MaxAttempts = 3,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            context.Competencies.Add(competency);
            await context.SaveChangesAsync();
            return competency;
        }

        private static async Task<ApplicationUser> EnsureStudentUserAsync(UserManager<ApplicationUser> userManager)
        {
            var existing = await userManager.Users.FirstOrDefaultAsync(u => u.UserName == "student");
            if (existing != null)
            {
                return existing;
            }

            var user = new ApplicationUser
            {
                UserName = "student",
                Email = "student@system.com",
                FirstName = "Ahmed",
                LastName = "Ali",
                FullName = "Ahmed Ali",
                RoleId = 4,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                EmailConfirmed = true
            };

            var result = await userManager.CreateAsync(user, "Student@123");
            if (!result.Succeeded)
            {
                throw new InvalidOperationException("Unable to seed student user: " + string.Join(", ", result.Errors.Select(e => e.Description)));
            }

            return user;
        }

        private static async Task<Student> EnsureStudentAsync(GradeDbContext context, int userId, int majorId)
        {
            var existing = await context.Students.FirstOrDefaultAsync(s => s.UserID == userId);
            if (existing != null)
            {
                return existing;
            }

            var seniorYear = await context.AcademicYears
                .Where(y => y.IsActive && y.Stage == EducationStage.Senior)
                .OrderByDescending(y => y.AcademicYearID)
                .FirstAsync();

            var student = new Student
            {
                UserID = userId,
                NationalID = "29901011234567",
                EnrollmentDate = DateTime.UtcNow.Date,
                CurrentAcademicYearID = seniorYear.AcademicYearID,
                MajorID = majorId,
                ClassID = 1,
                Status = "Active",
                Gender = Gender.Male
            };

            context.Students.Add(student);
            await context.SaveChangesAsync();
            return student;
        }

        private static async Task EnsureTermAsync(GradeDbContext context)
        {
            var yearIds = await context.AcademicYears
                .Where(y => y.IsActive)
                .Select(y => y.AcademicYearID)
                .ToListAsync();

            foreach (var yearId in yearIds)
            {
                var hasTerm = await context.Terms.AnyAsync(t => t.AcademicYearID == yearId);
                if (hasTerm)
                {
                    continue;
                }

                context.Terms.Add(new Term
                {
                    AcademicYearID = yearId,
                    TermName = "Term 1",
                    StartDate = new DateTime(2025, 9, 1),
                    EndDate = new DateTime(2026, 1, 31)
                });
            }

            await context.SaveChangesAsync();
        }

        private static async Task EnsureSubjectResultsAsync(GradeDbContext context, int studentId)
        {
            var termByYear = await context.Terms
                .Where(t => t.AcademicYearID.HasValue)
                .GroupBy(t => t.AcademicYearID!.Value)
                .ToDictionaryAsync(g => g.Key, g => g.OrderBy(x => x.TermID).First().TermID);

            var subjects = await context.Subjects
                .Where(s => s.IsActive && s.AcademicYearID.HasValue)
                .ToListAsync();

            foreach (var subject in subjects)
            {
                if (!subject.AcademicYearID.HasValue || !termByYear.ContainsKey(subject.AcademicYearID.Value))
                {
                    continue;
                }

                var yearId = subject.AcademicYearID.Value;
                var termId = termByYear[yearId];

                var exists = await context.StudentSubjectTermResults.AnyAsync(r =>
                    r.StudentID == studentId &&
                    r.SubjectID == subject.SubjectID &&
                    r.TermID == termId &&
                    r.AcademicYearID == yearId);

                if (exists)
                {
                    continue;
                }

                context.StudentSubjectTermResults.Add(new StudentSubjectTermResult
                {
                    StudentID = studentId,
                    SubjectID = subject.SubjectID,
                    TermID = termId,
                    AcademicYearID = yearId,
                    Quarter1Score = 12,
                    Quarter2Score = 13,
                    FinalExamScore = 85,
                    TermTotal = 110,
                    Status = SubjectStatus.Passed,
                    CreatedAt = DateTime.UtcNow
                });
            }

            await context.SaveChangesAsync();
        }

        private static async Task EnsureCompetencyStatusAsync(GradeDbContext context, int studentId, int competencyId)
        {
            var status = await context.StudentCompetencyStatuses
                .FirstOrDefaultAsync(s => s.StudentID == studentId && s.CompetencyID == competencyId);

            if (status == null)
            {
                status = new StudentCompetencyStatus
                {
                    StudentID = studentId,
                    CompetencyID = competencyId,
                    StatusID = "Fail",
                    CurrentAttemptNumber = 1,
                    MaxAllowedAttempts = 3,
                    LastEvaluatedAt = DateTime.UtcNow
                };

                context.StudentCompetencyStatuses.Add(status);
                await context.SaveChangesAsync();
            }

            var hasAttempt = await context.CompetencyAttempts.AnyAsync(a => a.StudentCompetencyStatusID == status.StudentCompetencyStatusID);
            if (!hasAttempt)
            {
                context.CompetencyAttempts.Add(new CompetencyAttempt
                {
                    StudentCompetencyStatusID = status.StudentCompetencyStatusID,
                    StudentID = studentId,
                    AttemptNumber = 1,
                    Result = "Fail",
                    EvaluatedAt = DateTime.UtcNow
                });
                await context.SaveChangesAsync();
            }
        }
    }
}
