using GradeManagementSystem.Core.Entities.Domain;
using GradeManagementSystem.Core.Entities.Enums;
using GradeManagementSystem.Core.Entities.Identity;
using GradeManagementSystem.Repository.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GradeManagementSystem.Api.Data;

/// <summary>
/// Development-only credentials for exercising each role-specific frontend flow.
/// Ensures all 4 standard test accounts exist with the expected credentials and domain profiles.
/// </summary>
public static class LocalTestAccountsSeed
{
    private sealed record TestAccount(
        string Username,
        string Password,
        string Email,
        string FirstName,
        string LastName,
        string RoleName);

    private static readonly TestAccount[] Accounts =
    [
        new("admin", "Admin@123", "admin@system.com", "System", "Admin", "Admin"),
        new("studentAffairs", "StudentAffairs@123", "studentaffairs@system.com", "Student", "Affairs", "Student Affairs"),
        new("teacher", "Teacher@123", "teacher@system.com", "Ahmed", "Karim", "Teacher"),
        new("student", "Student@123", "student@system.com", "Ahmed", "Ali", "Student"),
    ];

    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
        var context = scope.ServiceProvider.GetRequiredService<GradeDbContext>();
        var logger = scope.ServiceProvider.GetService<Microsoft.Extensions.Logging.ILogger<Program>>();

        foreach (var account in Accounts)
        {
            var role = await roleManager.Roles
                .SingleOrDefaultAsync(item => item.RoleName == account.RoleName)
                ?? throw new InvalidOperationException($"Required role '{account.RoleName}' was not found.");

            var normalizedUsername = account.Username.ToUpperInvariant();
            var user = await userManager.Users
                .SingleOrDefaultAsync(item => item.NormalizedUserName == normalizedUsername || (item.UserName != null && item.UserName.ToLower() == account.Username.ToLower()));

            if (user is null)
            {
                var existingEmailOwner = await userManager.FindByEmailAsync(account.Email);
                if (existingEmailOwner != null)
                {
                    logger?.LogWarning("Cannot create test account '{Username}': email '{Email}' is already assigned to UserID {OtherId}.", account.Username, account.Email, existingEmailOwner.UserId);
                    continue;
                }

                user = new ApplicationUser
                {
                    UserName = account.Username,
                    Email = account.Email,
                    NormalizedEmail = userManager.NormalizeEmail(account.Email),
                    FirstName = account.FirstName,
                    LastName = account.LastName,
                    FullName = $"{account.FirstName} {account.LastName}",
                    RoleId = role.RoleId,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    EmailConfirmed = true,
                    SecurityStamp = Guid.NewGuid().ToString(),
                };

                var result = await userManager.CreateAsync(user, account.Password);
                if (!result.Succeeded)
                {
                    throw new InvalidOperationException(
                        $"Unable to seed local test account '{account.Username}': {string.Join(", ", result.Errors.Select(error => error.Description))}");
                }
            }
            else
            {
                var needsUpdate = false;
                if (user.RoleId != role.RoleId)
                {
                    user.RoleId = role.RoleId;
                    needsUpdate = true;
                }
                if (!user.IsActive)
                {
                    user.IsActive = true;
                    needsUpdate = true;
                }
                if (!user.EmailConfirmed)
                {
                    user.EmailConfirmed = true;
                    needsUpdate = true;
                }
                if (user.LockoutEnd != null)
                {
                    user.LockoutEnd = null;
                    needsUpdate = true;
                }
                if (user.AccessFailedCount > 0)
                {
                    user.AccessFailedCount = 0;
                    needsUpdate = true;
                }

                if (!string.Equals(user.Email, account.Email, StringComparison.OrdinalIgnoreCase))
                {
                    var existingEmailOwner = await userManager.FindByEmailAsync(account.Email);
                    if (existingEmailOwner == null || existingEmailOwner.UserId == user.UserId)
                    {
                        user.Email = account.Email;
                        user.NormalizedEmail = userManager.NormalizeEmail(account.Email);
                        needsUpdate = true;
                    }
                    else
                    {
                        logger?.LogWarning("Cannot update email for test account '{Username}' to '{Email}': email is already in use by UserID {OtherId}.", account.Username, account.Email, existingEmailOwner.UserId);
                    }
                }

                if (needsUpdate)
                {
                    await userManager.UpdateAsync(user);
                }

                if (!await userManager.CheckPasswordAsync(user, account.Password))
                {
                    var token = await userManager.GeneratePasswordResetTokenAsync(user);
                    var resetRes = await userManager.ResetPasswordAsync(user, token, account.Password);
                    if (!resetRes.Succeeded)
                    {
                        await userManager.RemovePasswordAsync(user);
                        await userManager.AddPasswordAsync(user, account.Password);
                    }
                }
            }

            // Ensure Teacher domain profile
            if (role.Name == "Teacher" || role.RoleName == "Teacher")
            {
                var teacher = await context.Teachers.FirstOrDefaultAsync(t => t.UserID == user.UserId);
                if (teacher == null)
                {
                    var dept = await context.Departments.FirstOrDefaultAsync(d => d.IsActive)
                               ?? await context.Departments.FirstOrDefaultAsync();
                    if (dept == null)
                    {
                        dept = new Department { DepartmentName = "General", Description = "General Department", IsActive = true };
                        context.Departments.Add(dept);
                        await context.SaveChangesAsync();
                    }

                    teacher = new Teacher
                    {
                        UserID = user.UserId,
                        DepartmentID = dept.DepartmentID,
                        Qualifications = "Bachelor of Science",
                        EmployeeCode = $"TCH-{user.UserId:D4}",
                        HireDate = DateTime.SpecifyKind(DateTime.UtcNow.Date, DateTimeKind.Utc),
                        IsActive = true,
                    };
                    context.Teachers.Add(teacher);
                    await context.SaveChangesAsync();
                }
                else if (!teacher.IsActive)
                {
                    teacher.IsActive = true;
                    await context.SaveChangesAsync();
                }

                var hasAssignment = await context.TeacherAssignments
                    .AnyAsync(ta => ta.TeacherID == teacher.TeacherID && ta.IsActive);
                if (!hasAssignment)
                {
                    var academicYear = await context.AcademicYears
                        .Where(y => y.IsActive)
                        .OrderByDescending(y => y.AcademicYearID)
                        .FirstOrDefaultAsync()
                        ?? await context.AcademicYears.FirstOrDefaultAsync();

                    var targetClass = academicYear != null
                        ? await context.Classes.FirstOrDefaultAsync(c => c.IsActive && c.AcademicYearID == academicYear.AcademicYearID)
                        : await context.Classes.FirstOrDefaultAsync();

                    var targetSubject = academicYear != null
                        ? await context.Subjects.FirstOrDefaultAsync(s => s.IsActive && s.AcademicYearID == academicYear.AcademicYearID)
                        : await context.Subjects.FirstOrDefaultAsync();

                    if (academicYear != null && targetClass != null && targetSubject != null)
                    {
                        context.TeacherAssignments.Add(new TeacherAssignment
                        {
                            TeacherID = teacher.TeacherID,
                            ClassID = targetClass.ClassID,
                            SubjectID = targetSubject.SubjectID,
                            AcademicYearID = academicYear.AcademicYearID,
                            IsActive = true
                        });
                        await context.SaveChangesAsync();
                    }
                }
            }

            // Ensure Student domain profile
            if (role.Name == "Student" || role.RoleName == "Student")
            {
                var student = await context.Students.FirstOrDefaultAsync(s => s.UserID == user.UserId);
                if (student == null)
                {
                    var academicYear = await context.AcademicYears.FirstOrDefaultAsync(y => y.IsActive)
                                       ?? await context.AcademicYears.FirstOrDefaultAsync();
                    if (academicYear == null)
                    {
                        academicYear = new AcademicYear { YearName = "2025-2026", Stage = EducationStage.Senior, IsActive = true };
                        context.AcademicYears.Add(academicYear);
                        await context.SaveChangesAsync();
                    }

                    var defaultClass = await context.Classes.FirstOrDefaultAsync();

                    student = new Student
                    {
                        UserID = user.UserId,
                        NationalID = $"3000101{user.UserId:D7}",
                        StudentCode = $"STU-{user.UserId:D4}",
                        EnrollmentDate = DateTime.SpecifyKind(DateTime.UtcNow.Date, DateTimeKind.Utc),
                        CurrentAcademicYearID = academicYear.AcademicYearID,
                        ClassID = defaultClass?.ClassID,
                        Status = "Active",
                        Gender = Gender.Male,
                    };
                    context.Students.Add(student);
                    await context.SaveChangesAsync();
                }
                else if (student.Status != "Active")
                {
                    student.Status = "Active";
                    await context.SaveChangesAsync();
                }
            }
        }

        // Ensure baseline seed notifications exist so notification bell displays realistic data
        if (!await context.Notifications.AnyAsync())
        {
            var adminUser = await userManager.FindByNameAsync("admin");
            var adminId = adminUser?.UserId;

            context.Notifications.AddRange(
                new AppNotification
                {
                    Type = "announcement",
                    Title = "Welcome to Eva School Portal",
                    Message = "The academic term has commenced. Review your schedules, assignments, and curriculum updates.",
                    Priority = "medium",
                    TargetRole = null, // broadcast to all
                    CreatedByUserID = adminId,
                    CreatedAt = DateTime.UtcNow.AddHours(-2)
                },
                new AppNotification
                {
                    Type = "reminder",
                    Title = "Quarter 1 Grade Entry Open",
                    Message = "Quarter 1 grade submission is now open for all assigned subjects. Please submit before the deadline.",
                    Priority = "high",
                    TargetRole = "Teacher",
                    CreatedByUserID = adminId,
                    CreatedAt = DateTime.UtcNow.AddMinutes(-45)
                },
                new AppNotification
                {
                    Type = "grade",
                    Title = "Academic Progress Overview Available",
                    Message = "Your semester performance overview has been updated. You can check your subjects and competencies.",
                    Priority = "low",
                    TargetRole = "Student",
                    CreatedByUserID = adminId,
                    CreatedAt = DateTime.UtcNow.AddMinutes(-30)
                }
            );
            await context.SaveChangesAsync();
        }
    }
}
