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

        public AuthServiceEmailLoginTests()
        {
            var services = new ServiceCollection();

            var connectionString = "Host=ep-still-water-au7x5jn8.c-10.us-east-1.aws.neon.tech;Database=neondb;Username=neondb_owner;Password=npg_s6hGZPx0WDpm;SSL Mode=Require;Trust Server Certificate=true;Timeout=60;Command Timeout=60;Keepalive=30;";
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
                ["Jwt:Key"] = "ThisIsAVerySecretKeyForGradeManagementSystem2026!",
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

        [Fact]
        public async Task LoginAsync_Succeeds_With_Valid_Email_And_Password()
        {
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
            _serviceProvider.Dispose();
        }

        private sealed class DummyEmailService : IEmailService
        {
            public Task SendEmailAsync(string to, string subject, string body) => Task.CompletedTask;
        }
    }
}
