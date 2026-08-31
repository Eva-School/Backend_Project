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

            // 1. Clean up legacy/removed academic years and sample data
            await CleanupRemovedYearsAndSampleDataAsync(context, userManager);

            // 2. Ensure standard departments exist (OM, SD)
            await GetOrCreateDepartmentAsync(context, "OM", "OM Department", isActive: true);
            await GetOrCreateDepartmentAsync(context, "SD", "SD Department", isActive: true);

            // 3. Ensure Terms (Term 1, Term 2) exist for all active academic years
            var activeYears = await context.AcademicYears.Where(y => y.IsActive).ToListAsync();
            foreach (var year in activeYears)
            {
                await EnsureTermsForYearAsync(context, year.AcademicYearID);
            }

            // 4. Ensure standard subjects exist for active academic years
            foreach (var year in activeYears)
            {
                await EnsureSubjectsForYearAsync(context, year.AcademicYearID);
            }
        }

        private static async Task CleanupRemovedYearsAndSampleDataAsync(GradeDbContext context, UserManager<ApplicationUser> userManager)
        {
            // 1. Competency attempts & statuses
            var attempts = await context.CompetencyAttempts.ToListAsync();
            if (attempts.Count > 0)
            {
                context.CompetencyAttempts.RemoveRange(attempts);
            }

            var compStatuses = await context.StudentCompetencyStatuses.ToListAsync();
            if (compStatuses.Count > 0)
            {
                context.StudentCompetencyStatuses.RemoveRange(compStatuses);
            }

            // 2. Quiz grades
            var quizGrades = await context.QuizGrades.ToListAsync();
            if (quizGrades.Count > 0)
            {
                context.QuizGrades.RemoveRange(quizGrades);
            }

            // 3. Action logs
            var sampleLogs = await context.GradeActionLogs.ToListAsync();
            if (sampleLogs.Count > 0)
            {
                context.GradeActionLogs.RemoveRange(sampleLogs);
            }

            // 4. Result approvals & results
            var sampleResultApprovals = await context.ResultApprovals.ToListAsync();
            if (sampleResultApprovals.Count > 0)
            {
                context.ResultApprovals.RemoveRange(sampleResultApprovals);
            }

            var sampleAllResults = await context.StudentAllResults.ToListAsync();
            if (sampleAllResults.Count > 0)
            {
                context.StudentAllResults.RemoveRange(sampleAllResults);
            }

            var sampleSubmissions = await context.QuarterGradeSubmissions.ToListAsync();
            if (sampleSubmissions.Count > 0)
            {
                context.QuarterGradeSubmissions.RemoveRange(sampleSubmissions);
            }

            var sampleLocks = await context.QuarterGradesLocks.ToListAsync();
            if (sampleLocks.Count > 0)
            {
                context.QuarterGradesLocks.RemoveRange(sampleLocks);
            }

            var sampleTermResults = await context.StudentSubjectTermResults.ToListAsync();
            if (sampleTermResults.Count > 0)
            {
                context.StudentSubjectTermResults.RemoveRange(sampleTermResults);
            }

            // 5. Student promotions, guardians, previous schools
            var promotions = await context.StudentPromotions.ToListAsync();
            if (promotions.Count > 0)
            {
                context.StudentPromotions.RemoveRange(promotions);
            }

            var guardians = await context.Guardians.ToListAsync();
            if (guardians.Count > 0)
            {
                context.Guardians.RemoveRange(guardians);
            }

            var prevSchools = await context.PreviousSchools.ToListAsync();
            if (prevSchools.Count > 0)
            {
                context.PreviousSchools.RemoveRange(prevSchools);
            }

            // 6. Delete all students
            var allStudents = await context.Students.ToListAsync();
            if (allStudents.Count > 0)
            {
                context.Students.RemoveRange(allStudents);
            }

            // 7. Delete teacher assignments
            var teacherAssignments = await context.TeacherAssignments.ToListAsync();
            if (teacherAssignments.Count > 0)
            {
                context.TeacherAssignments.RemoveRange(teacherAssignments);
            }

            // 8. Delete all sample classes
            var classes = await context.Classes.ToListAsync();
            if (classes.Count > 0)
            {
                context.Classes.RemoveRange(classes);
            }

            await context.SaveChangesAsync();

            // 9. Remove sample student users (except core roles)
            var sampleUserEmails = new[] { "s1_4@system.com", "s2_4@system.com", "s3_4@system.com", "s4_4@system.com", "student_fail@system.com" };
            foreach (var email in sampleUserEmails)
            {
                var u = await userManager.FindByEmailAsync(email);
                if (u != null)
                {
                    await userManager.DeleteAsync(u);
                }
            }

            // 10. Remove legacy 2022-2023 and 2023-2024 academic years
            var removedYears = await context.AcademicYears
                .Where(y => y.YearName == "2022-2023" || y.YearName == "2023-2024")
                .ToListAsync();
            if (removedYears.Count > 0)
            {
                var removedYearIds = removedYears.Select(y => y.AcademicYearID).ToList();
                var termsToRemove = await context.Terms
                    .Where(t => t.AcademicYearID.HasValue && removedYearIds.Contains(t.AcademicYearID.Value))
                    .ToListAsync();
                if (termsToRemove.Count > 0)
                {
                    context.Terms.RemoveRange(termsToRemove);
                }

                context.AcademicYears.RemoveRange(removedYears);
                await context.SaveChangesAsync();
            }
        }

        private static async Task<Department> GetOrCreateDepartmentAsync(GradeDbContext context, string name, string description, bool isActive)
        {
            var existing = await context.Departments.FirstOrDefaultAsync(d => d.IsActive && d.DepartmentName == name);
            if (existing != null)
            {
                return existing;
            }

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
                    StartDate = DateTime.SpecifyKind(new DateTime(DateTime.UtcNow.Year, 9, 1), DateTimeKind.Utc),
                    EndDate = DateTime.SpecifyKind(new DateTime(DateTime.UtcNow.Year + 1, 1, 31), DateTimeKind.Utc)
                });
            }

            if (!terms.Any(t => t.TermName == "Term 2"))
            {
                context.Terms.Add(new Term
                {
                    AcademicYearID = academicYearId,
                    TermName = "Term 2",
                    StartDate = DateTime.SpecifyKind(new DateTime(DateTime.UtcNow.Year + 1, 2, 1), DateTimeKind.Utc),
                    EndDate = DateTime.SpecifyKind(new DateTime(DateTime.UtcNow.Year + 1, 6, 30), DateTimeKind.Utc)
                });
            }

            await context.SaveChangesAsync();
        }

        private static async Task EnsureSubjectsForYearAsync(GradeDbContext context, int academicYearId)
        {
            var existing = await context.Subjects
                .Where(s => s.AcademicYearID == academicYearId && s.IsActive)
                .ToListAsync();

            if (existing.Count > 0)
            {
                return;
            }

            var math = new Subject
            {
                SubjectName = "Math",
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
    }
}
