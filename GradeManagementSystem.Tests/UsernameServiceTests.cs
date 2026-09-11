using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GradeManagementSystem.Core.Entities.Identity;
using GradeManagementSystem.Repository.Data;
using GradeManagementSystem.Services.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace GradeManagementSystem.Tests
{
    public class UsernameServiceTests
    {
        private (GradeDbContext Context, UserManager<ApplicationUser> UserManager, UsernameService Service) CreateService()
        {
            var services = new ServiceCollection();
            services.AddDbContext<GradeDbContext>(options =>
                options.UseInMemoryDatabase(Guid.NewGuid().ToString()));

            services.AddIdentity<ApplicationUser, ApplicationRole>(options =>
            {
                options.User.RequireUniqueEmail = true;
            })
            .AddEntityFrameworkStores<GradeDbContext>()
            .AddDefaultTokenProviders();

            services.AddLogging();

            var sp = services.BuildServiceProvider();
            var context = sp.GetRequiredService<GradeDbContext>();
            var userManager = sp.GetRequiredService<UserManager<ApplicationUser>>();
            var service = new UsernameService(userManager);

            return (context, userManager, service);
        }

        private static ApplicationUser CreateTestUser(string username, string email, UserManager<ApplicationUser> userManager)
        {
            return new ApplicationUser
            {
                UserName = username,
                Email = email,
                NormalizedUserName = userManager.NormalizeName(username),
                NormalizedEmail = userManager.NormalizeEmail(email),
                FirstName = "Test",
                LastName = "User",
                FullName = "Test User",
                IsActive = true
            };
        }

        [Theory]
        [InlineData("userA@example.com", "userA")]
        [InlineData("john.doe@school.edu", "john.doe")]
        [InlineData("sarah-connor_99@test.org", "sarah-connor_99")]
        [InlineData("User.Name+marketing@domain.com", "User.Name")]
        [InlineData("test+filter+more@domain.com", "test")]
        [InlineData(".leading.trailing.@domain.com", "leading.trailing")]
        [InlineData("double..dot...test@domain.com", "double.dot.test")]
        [InlineData("special!#$chars%&*@domain.com", "specialchars")]
        [InlineData("a@domain.com", "a")]
        [InlineData("ed@domain.com", "ed")]
        [InlineData("", "user")]
        [InlineData("   ", "user")]
        [InlineData("@domain.com", "user")]
        [InlineData("!@#$%^@domain.com", "user")]
        [InlineData("...---___@domain.com", "user")]
        public void ExtractBaseUsernameFromEmail_ExtractsAndSanitizesCorrectly(string email, string expected)
        {
            var (_, _, service) = CreateService();

            var result = service.ExtractBaseUsernameFromEmail(email);

            Assert.Equal(expected, result);
        }

        [Fact]
        public async Task GenerateUniqueUsernameFromEmailAsync_ReturnsBaseUsername_WhenNoCollision()
        {
            var (_, _, service) = CreateService();

            var username = await service.GenerateUniqueUsernameFromEmailAsync("userA@example.com");

            Assert.Equal("userA", username);
        }

        [Fact]
        public async Task GenerateUniqueUsernameFromEmailAsync_AppendsNumericSuffix_WhenCollisionExists()
        {
            var (_, userManager, service) = CreateService();

            var existingUser = CreateTestUser("userA", "other@example.com", userManager);
            await userManager.CreateAsync(existingUser);

            var username = await service.GenerateUniqueUsernameFromEmailAsync("userA@example.com");

            Assert.Equal("userA1", username);
        }

        [Fact]
        public async Task GenerateUniqueUsernameFromEmailAsync_AppendsNextSuffix_WhenMultipleCollisionsExist()
        {
            var (_, userManager, service) = CreateService();

            var user1 = CreateTestUser("userA", "other1@example.com", userManager);
            var user2 = CreateTestUser("userA1", "other2@example.com", userManager);
            await userManager.CreateAsync(user1);
            await userManager.CreateAsync(user2);

            var username = await service.GenerateUniqueUsernameFromEmailAsync("userA@example.com");

            Assert.Equal("userA2", username);
        }

        [Fact]
        public async Task GenerateUniqueUsernameFromEmailAsync_IsCaseInsensitiveForCollisions()
        {
            var (_, userManager, service) = CreateService();

            var existingUser = CreateTestUser("USERA", "existing@example.com", userManager);
            await userManager.CreateAsync(existingUser);

            // Request lowercase "usera@example.com"
            var username = await service.GenerateUniqueUsernameFromEmailAsync("usera@example.com");

            Assert.Equal("usera1", username);
        }

        [Fact]
        public async Task GenerateUniqueUsernameFromEmailAsync_RespectsReservedInBatch()
        {
            var (_, _, service) = CreateService();
            var reserved = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            var user1 = await service.GenerateUniqueUsernameFromEmailAsync("userA@example.com", reservedInBatch: reserved);
            var user2 = await service.GenerateUniqueUsernameFromEmailAsync("userA@example.com", reservedInBatch: reserved);
            var user3 = await service.GenerateUniqueUsernameFromEmailAsync("userA@example.com", reservedInBatch: reserved);

            Assert.Equal("userA", user1);
            Assert.Equal("userA1", user2);
            Assert.Equal("userA2", user3);

            Assert.Contains("userA", reserved);
            Assert.Contains("userA1", reserved);
            Assert.Contains("userA2", reserved);
        }

        [Fact]
        public async Task IsUsernameAvailableAsync_ReturnsCorrectStatus()
        {
            var (_, userManager, service) = CreateService();
            var reserved = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "reservedUser" };

            var existingUser = CreateTestUser("ExistingUser", "existing@example.com", userManager);
            await userManager.CreateAsync(existingUser);

            Assert.False(await service.IsUsernameAvailableAsync("existinguser"));
            Assert.False(await service.IsUsernameAvailableAsync("EXISTINGUSER"));
            Assert.False(await service.IsUsernameAvailableAsync("reserveduser", reservedInBatch: reserved));
            Assert.True(await service.IsUsernameAvailableAsync("brandNewUser", reservedInBatch: reserved));
            Assert.False(await service.IsUsernameAvailableAsync(""));
        }
    }
}
