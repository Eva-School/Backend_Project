using GradeManagementSystem.Core.Entities.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GradeManagementSystem.Api.Data;

/// <summary>
/// Development-only credentials for exercising each role-specific frontend flow.
/// This seed is intentionally called only by Program when ASPNETCORE_ENVIRONMENT is Development.
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
        new("admin", "Admin@123", "admin@local.test", "System", "Admin", "Admin"),
        new("studentaffairs", "StudentAffairs@123", "studentaffairs@local.test", "Student", "Affairs", "Student Affairs"),
        new("teacher", "Teacher@123", "teacher@system.com", "Ahmed", "Karim", "Teacher"),
        new("student", "Student@123", "student@system.com", "Ahmed", "Ali", "Student"),
    ];

    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();

        foreach (var account in Accounts)
        {
            var role = await roleManager.Roles
                .SingleOrDefaultAsync(item => item.RoleName == account.RoleName)
                ?? throw new InvalidOperationException($"Required role '{account.RoleName}' was not found.");

            var user = await userManager.Users.SingleOrDefaultAsync(item => item.UserName == account.Username);
            if (user is null)
            {
                user = new ApplicationUser
                {
                    UserName = account.Username,
                    Email = account.Email,
                    FirstName = account.FirstName,
                    LastName = account.LastName,
                    FullName = $"{account.FirstName} {account.LastName}",
                    RoleId = role.RoleId,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    EmailConfirmed = true,
                };

                var result = await userManager.CreateAsync(user, account.Password);
                if (!result.Succeeded)
                {
                    throw new InvalidOperationException(
                        $"Unable to seed local test account '{account.Username}': {string.Join(", ", result.Errors.Select(error => error.Description))}");
                }

                continue;
            }

            if (user.RoleId != role.RoleId)
            {
                user.RoleId = role.RoleId;
                var result = await userManager.UpdateAsync(user);
                if (!result.Succeeded)
                {
                    throw new InvalidOperationException(
                        $"Unable to align local test account '{account.Username}' with role '{account.RoleName}'.");
                }
            }
        }
    }
}
