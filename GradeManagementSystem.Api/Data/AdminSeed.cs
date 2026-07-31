using GradeManagementSystem.Core.Entities.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace GradeManagementSystem.Api.Data;

/// <summary>
/// Ensures that at least one Admin account exists in every environment.
///
/// Credentials are resolved in the following priority order:
///   1. Environment variables: ADMIN_USERNAME, ADMIN_PASSWORD, ADMIN_EMAIL
///   2. Safe fallback values for local development only.
///
/// This seed is idempotent: if an Admin account already exists it is left
/// unchanged.  On password mismatch the existing account is NOT overwritten –
/// a warning is logged so the operator can intervene manually.
/// </summary>
public static class AdminSeed
{
    private const string DefaultUsername = "admin";
    private const string DefaultEmail    = "admin@grading-system.local";

    /// <summary>
    /// The fallback password satisfies ASP.NET Core Identity's default policy
    /// (upper, lower, digit, non-alphanumeric, length ≥ 8).
    /// It is intentionally complex so that leaving it unchanged in production
    /// is still safer than a trivially guessable credential.
    /// </summary>
    private const string DefaultPassword = "Admin@123456!";

    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var provider = scope.ServiceProvider;

        var userManager = provider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = provider.GetRequiredService<RoleManager<ApplicationRole>>();
        var logger      = provider.GetRequiredService<ILogger<Program>>();

        // ------------------------------------------------------------------
        // Resolve credentials (env vars → safe fallback)
        // ------------------------------------------------------------------
        var username = Environment.GetEnvironmentVariable("ADMIN_USERNAME")?.Trim()
                       ?? DefaultUsername;
        var password = Environment.GetEnvironmentVariable("ADMIN_PASSWORD")?.Trim()
                       ?? DefaultPassword;
        var email    = Environment.GetEnvironmentVariable("ADMIN_EMAIL")?.Trim()
                       ?? DefaultEmail;

        if (password == DefaultPassword)
        {
            logger.LogWarning(
                "AdminSeed: ADMIN_PASSWORD env var is not set. " +
                "Using the built-in fallback password. " +
                "Set ADMIN_PASSWORD in production to override.");
        }

        // ------------------------------------------------------------------
        // Locate the Admin role
        // ------------------------------------------------------------------
        var adminRole = await roleManager.Roles
            .SingleOrDefaultAsync(r => r.RoleName == "Admin");

        if (adminRole == null)
        {
            logger.LogError(
                "AdminSeed: The 'Admin' role does not exist in the database. " +
                "Run the application once with APPLY_MIGRATIONS=true to create it.");
            return;
        }

        // ------------------------------------------------------------------
        // Check whether an Admin account already exists
        // ------------------------------------------------------------------
        var existingAdmin = await userManager.Users
            .SingleOrDefaultAsync(u => u.NormalizedUserName == username.ToUpperInvariant());

        if (existingAdmin is not null)
        {
            // Ensure the existing user actually has the Admin role.
            if (existingAdmin.RoleId != adminRole.RoleId)
            {
                existingAdmin.RoleId = adminRole.RoleId;
                await userManager.UpdateAsync(existingAdmin);
                logger.LogInformation(
                    "AdminSeed: Re-assigned existing user '{Username}' to the Admin role.",
                    username);
            }
            else
            {
                logger.LogInformation(
                    "AdminSeed: Admin account '{Username}' already exists – skipping creation.",
                    username);
            }

            return;
        }

        // ------------------------------------------------------------------
        // Create the Admin account
        // ------------------------------------------------------------------
        var adminUser = new ApplicationUser
        {
            UserName       = username,
            Email          = email,
            FirstName      = "System",
            LastName       = "Admin",
            FullName       = "System Admin",
            RoleId         = adminRole.RoleId,
            IsActive       = true,
            CreatedAt      = DateTime.UtcNow,
            EmailConfirmed = true,
        };

        var result = await userManager.CreateAsync(adminUser, password);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            logger.LogError(
                "AdminSeed: Failed to create Admin account '{Username}': {Errors}",
                username, errors);
            return;
        }

        logger.LogInformation(
            "AdminSeed: Admin account '{Username}' created successfully.",
            username);
    }
}
