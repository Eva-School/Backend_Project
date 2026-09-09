using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GradeManagementSystem.Repository.Data;
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

        private GradeDbContext CreateDbContext()
        {
            var connectionString = "Host=ep-still-water-au7x5jn8.c-10.us-east-1.aws.neon.tech;Database=neondb;Username=neondb_owner;Password=npg_s6hGZPx0WDpm;SSL Mode=Require;Trust Server Certificate=true;Timeout=60;Command Timeout=60;Keepalive=30;";
            var options = new DbContextOptionsBuilder<GradeDbContext>()
                .UseNpgsql(connectionString)
                .Options;

            return new GradeDbContext(options);
        }

        [Fact]
        public async Task Audit_Existing_User_Emails()
        {
            await using var context = CreateDbContext();
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
    }
}
