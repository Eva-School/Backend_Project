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
    private const string DefaultEmail    = "admin@system.com";

    /// <summary>
    /// The fallback password satisfies ASP.NET Core Identity's default policy
    /// (upper, lower, digit, non-alphanumeric, length ≥ 8).
    /// It is intentionally complex so that leaving it unchanged in production
    /// is still safer than a trivially guessable credential.
    /// </summary>
    private const string DefaultPassword = "Admin@123";

    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var provider = scope.ServiceProvider;

        var userManager = provider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = provider.GetRequiredService<RoleManager<ApplicationRole>>();
        var logger      = provider.GetRequiredService<ILogger<Program>>();
        var hostEnv     = provider.GetService<IHostEnvironment>();

        // ------------------------------------------------------------------
        // Resolve credentials (env vars → safe fallback)
        // ------------------------------------------------------------------
        var adminPasswordEnv = Environment.GetEnvironmentVariable("ADMIN_PASSWORD")?.Trim();
        var username = Environment.GetEnvironmentVariable("ADMIN_USERNAME")?.Trim()
                       ?? DefaultUsername;
        var password = adminPasswordEnv
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
        // Ensure Admin Role exists
        // ------------------------------------------------------------------
        var adminRole = await roleManager.Roles
            .SingleOrDefaultAsync(r => r.RoleName == "Admin");

        if (adminRole is null)
        {
            logger.LogError(
                "AdminSeed: Role 'Admin' does not exist in the database. " +
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
            var needsUpdate = false;
            if (existingAdmin.RoleId != adminRole.RoleId)
            {
                existingAdmin.RoleId = adminRole.RoleId;
                needsUpdate = true;
            }
            if (!existingAdmin.IsActive)
            {
                existingAdmin.IsActive = true;
                needsUpdate = true;
            }
            if (!existingAdmin.EmailConfirmed)
            {
                existingAdmin.EmailConfirmed = true;
                needsUpdate = true;
            }
            if (existingAdmin.LockoutEnd != null)
            {
                existingAdmin.LockoutEnd = null;
                needsUpdate = true;
            }
            if (existingAdmin.AccessFailedCount > 0)
            {
                existingAdmin.AccessFailedCount = 0;
                needsUpdate = true;
            }

            if (!string.Equals(existingAdmin.Email, email, StringComparison.OrdinalIgnoreCase))
            {
                var existingEmailOwner = await userManager.FindByEmailAsync(email);
                if (existingEmailOwner == null || existingEmailOwner.UserId == existingAdmin.UserId)
                {
                    existingAdmin.Email = email;
                    existingAdmin.NormalizedEmail = userManager.NormalizeEmail(email);
                    needsUpdate = true;
                }
            }

            if (needsUpdate)
            {
                await userManager.UpdateAsync(existingAdmin);
            }

            // In development or when explicitly configured via ADMIN_PASSWORD, align password
            var isDev = hostEnv?.IsDevelopment() ?? false;
            var hasExplicitPassword = !string.IsNullOrEmpty(adminPasswordEnv);

            if ((isDev || hasExplicitPassword) && !await userManager.CheckPasswordAsync(existingAdmin, password))
            {
                var token = await userManager.GeneratePasswordResetTokenAsync(existingAdmin);
                var res = await userManager.ResetPasswordAsync(existingAdmin, token, password);
                if (!res.Succeeded)
                {
                    await userManager.RemovePasswordAsync(existingAdmin);
                    await userManager.AddPasswordAsync(existingAdmin, password);
                }
                logger.LogInformation("AdminSeed: Aligned password for Admin account '{Username}'.", username);
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
