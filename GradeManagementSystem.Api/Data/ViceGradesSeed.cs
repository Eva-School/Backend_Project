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

            // 1. Ensure standard departments exist (OM, SD)
            await GetOrCreateDepartmentAsync(context, "OM", "OM Department", isActive: true);
            await GetOrCreateDepartmentAsync(context, "SD", "SD Department", isActive: true);

            // 2. Ensure Terms (Term 1, Term 2) exist for all active academic years
            var activeYears = await context.AcademicYears.Where(y => y.IsActive).ToListAsync();
            foreach (var year in activeYears)
            {
                await EnsureTermsForYearAsync(context, year.AcademicYearID);
            }

            // 3. Ensure standard subjects exist for active academic years
            foreach (var year in activeYears)
            {
                await EnsureSubjectsForYearAsync(context, year.AcademicYearID);
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
