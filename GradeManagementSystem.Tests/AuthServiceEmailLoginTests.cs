using System;
using System.IO;
using System.Security.Claims;
using System.Threading.Tasks;
using AutoMapper;
using GradeManagementSystem.Core.DTOs.Auth;
using GradeManagementSystem.Core.Entities.Identity;
using GradeManagementSystem.Core.Interfaces;
using GradeManagementSystem.Repository.Data;
using GradeManagementSystem.Services.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace GradeManagementSystem.Tests
{
    public class AuthServiceEmailLoginTests : IDisposable
    {
        private readonly ServiceProvider _serviceProvider;
        private readonly GradeDbContext _context;
        private readonly AuthService _authService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly bool _isConfigured;

        public AuthServiceEmailLoginTests()
        {
            var raw = Environment.GetEnvironmentVariable("TEST_CONNECTION_STRING");
            if (string.IsNullOrWhiteSpace(raw))
            {
                _isConfigured = false;
                _serviceProvider = new ServiceCollection().BuildServiceProvider();
                _context = null!;
                _userManager = null!;
                _authService = null!;
                return;
            }

            _isConfigured = true;
            var connectionString = PostgresConnectionParser.Parse(raw);

            var services = new ServiceCollection();
            services.AddDbContext<GradeDbContext>(options =>
                options.UseNpgsql(connectionString));

            services.AddIdentity<ApplicationUser, ApplicationRole>(options =>
            {
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = true;
                options.Password.RequiredLength = 8;
                options.User.RequireUniqueEmail = true;
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
            })
            .AddEntityFrameworkStores<GradeDbContext>()
            .AddDefaultTokenProviders();

            services.AddLogging(builder => builder.AddProvider(NullLoggerProvider.Instance));

            var configDict = new System.Collections.Generic.Dictionary<string, string?>
            {
                ["Jwt:Key"] = "TestSecretKeyForTestingPurposesOnly12345678!",
                ["Jwt:Issuer"] = "GradeManagementSystem",
                ["Jwt:Audience"] = "GradeManagementSystemFrontend",
                ["Jwt:DurationInMinutes"] = "60",
                ["Jwt:RefreshTokenExpirationDays"] = "7"
            };
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(configDict)
                .Build();
            services.AddSingleton<IConfiguration>(configuration);

            services.AddAutoMapper(cfg => { });
            services.AddScoped<IEmailService, DummyEmailService>();
            services.AddScoped<AuthService>();

            _serviceProvider = services.BuildServiceProvider();
            _context = _serviceProvider.GetRequiredService<GradeDbContext>();
            _userManager = _serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            _authService = _serviceProvider.GetRequiredService<AuthService>();
        }

        private bool CheckDatabaseConfigured()
        {
            return _isConfigured;
        }

        [Fact]
        public async Task LoginAsync_Succeeds_With_Valid_Email_And_Password()
        {
            if (!CheckDatabaseConfigured()) return;
            var request = new LoginRequest
            {
                Email = "admin@system.com",
                Password = "Admin@123"
            };

            var result = await _authService.LoginAsync(request);

            Assert.NotNull(result);
            Assert.Equal("Admin", result.Role);
            Assert.False(string.IsNullOrEmpty(result.AccessToken));
            Assert.False(string.IsNullOrEmpty(result.RefreshToken));
        }

        [Fact]
        public async Task LoginAsync_Succeeds_Case_Insensitive_Email()
        {
            if (!CheckDatabaseConfigured()) return;
            var request = new LoginRequest
            {
                Email = "ADMIN@SYSTEM.COM",
                Password = "Admin@123"
            };

            var result = await _authService.LoginAsync(request);

            Assert.NotNull(result);
            Assert.Equal("Admin", result.Role);
        }

        [Fact]
        public async Task LoginAsync_Succeeds_With_Surrounding_Whitespace_In_Email()
        {
            if (!CheckDatabaseConfigured()) return;
            var request = new LoginRequest
            {
                Email = "   admin@system.com   ",
                Password = "Admin@123"
            };

            var result = await _authService.LoginAsync(request);

            Assert.NotNull(result);
            Assert.Equal("Admin", result.Role);
        }

        [Fact]
        public async Task LoginAsync_Returns_Null_For_Unknown_Email()
        {
            if (!CheckDatabaseConfigured()) return;
            var request = new LoginRequest
            {
                Email = "nonexistent.user@system.com",
                Password = "Admin@123"
            };

            var result = await _authService.LoginAsync(request);

            Assert.Null(result);
        }

        [Fact]
        public async Task LoginAsync_Returns_Null_For_Incorrect_Password()
        {
            if (!CheckDatabaseConfigured()) return;
            var request = new LoginRequest
            {
                Email = "admin@system.com",
                Password = "WrongPassword@999"
            };

            var result = await _authService.LoginAsync(request);

            Assert.Null(result);
        }

        [Fact]
        public async Task LoginAsync_Does_Not_Trim_Passwords()
        {
            if (!CheckDatabaseConfigured()) return;
            // Password with intentional leading/trailing space should NOT match "Admin@123"
            var request = new LoginRequest
            {
                Email = "admin@system.com",
                Password = " Admin@123 "
            };

            var result = await _authService.LoginAsync(request);

            Assert.Null(result);
        }

        public void Dispose()
        {
            _serviceProvider?.Dispose();
        }

        private sealed class DummyEmailService : IEmailService
        {
            public Task SendEmailAsync(string to, string subject, string body) => Task.CompletedTask;
        }
    }
}
