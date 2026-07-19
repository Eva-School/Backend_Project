using AutoMapper;
using GradeManagementSystem.Core.DTOs.Auth;
using GradeManagementSystem.Core.DTOs.Teacher;
using GradeManagementSystem.Core.Entities.Domain;
using GradeManagementSystem.Core.Entities.Identity;
using GradeManagementSystem.Core.Interfaces;
using GradeManagementSystem.Repository.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace GradeManagementSystem.Services.Services
{
    public class AuthService : IAuthService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<ApplicationRole> _roleManager;
        private readonly IConfiguration _configuration;
        private readonly IMapper _mapper;
        private readonly IEmailService _emailService;
        private readonly GradeDbContext _context;
        private readonly ILogger<AuthService> _logger;

        public AuthService(
            UserManager<ApplicationUser> userManager,
            RoleManager<ApplicationRole> roleManager,
            IConfiguration configuration,
            IMapper mapper,
            IEmailService emailService,
            GradeDbContext context,
            ILogger<AuthService> logger)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _configuration = configuration;
            _mapper = mapper;
            _emailService = emailService;
            _context = context;
            _logger = logger;
        }

        public async Task<AuthResponse?> LoginAsync(LoginRequest request)
        {
            // Include Role to get the RoleName
            var user = await _userManager.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.UserName == request.Username);

            if (user == null || !user.IsActive || !await _userManager.CheckPasswordAsync(user, request.Password))
            {
                return null;
            }

            // Get Role Name from the navigation property
            var role = user.Role?.RoleName ?? "Student";

            var accessToken = GenerateJwtToken(user, role);
            var refreshToken = GenerateRefreshToken();

            user.RefreshToken = refreshToken;
            user.RefreshTokenExpiryTime = DateTime.Now.AddDays(Convert.ToDouble(_configuration["Jwt:RefreshTokenExpirationDays"]));
            user.LastLoginAt = DateTime.Now;

            await _userManager.UpdateAsync(user);

            return new AuthResponse
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                Role = role
            };
        }

        public async Task<AuthResponse?> RefreshTokenAsync(string refreshToken)
        {
            var user = await _userManager.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.RefreshToken == refreshToken);

            if (user == null || !user.IsActive || user.RefreshTokenExpiryTime <= DateTime.UtcNow)
            {
                return null;
            }

            var role = user.Role?.RoleName ?? "Student";

            var newAccessToken = GenerateJwtToken(user, role);
            var newRefreshToken = GenerateRefreshToken();

            user.RefreshToken = newRefreshToken;
            await _userManager.UpdateAsync(user);

            return new AuthResponse
            {
                AccessToken = newAccessToken,
                RefreshToken = newRefreshToken,
                Role = role
            };
        }

        public async Task<bool> LogoutAsync(string refreshToken)
        {
            var user = await _userManager.Users.FirstOrDefaultAsync(u => u.RefreshToken == refreshToken);

            // If user not found with this refresh token, logout fails (invalid token)
            if (user == null) return false;

            user.RefreshToken = null;
            user.RefreshTokenExpiryTime = null;
            await _userManager.UpdateAsync(user);
            return true;
        }

        public async Task<UserInfoResponse?> GetUserInfoAsync(int userId)
        {
            var user = await _userManager.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.UserId == userId);

            if (user == null) return null;

            var response = _mapper.Map<UserInfoResponse>(user);
            response.Role = user.Role?.RoleName ?? "Student";

            return response;
        }

        private string GenerateJwtToken(ApplicationUser user, string role)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new Claim(ClaimTypes.Name, user.UserName ?? ""),
                new Claim(ClaimTypes.Role, role)
            };

            var jwtKey = _configuration["Jwt:Key"]
                ?? throw new InvalidOperationException("JWT signing key is not configured.");
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var expires = DateTime.Now.AddMinutes(Convert.ToDouble(_configuration["Jwt:DurationInMinutes"]));

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: expires,
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public async Task<object> RegisterTeacherAsync(TeacherRegisterRequest request)
        {
            // 1. Resolve or Generate Username and Password
            string username;
            if (!string.IsNullOrWhiteSpace(request.Username))
            {
                username = request.Username.Trim();
                var userByName = await _userManager.FindByNameAsync(username);
                if (userByName != null)
                {
                    return new { success = false, message = "Username is already taken" };
                }
            }
            else
            {
                string baseUsername = $"{request.FullName.FirstName.ToLower()}.{request.FullName.LastName.ToLower()}".Replace(" ", "");
                username = baseUsername + RandomNumberGenerator.GetInt32(100, 1000);
                int retries = 5;
                while (await _userManager.FindByNameAsync(username) != null && retries > 0)
                {
                    username = baseUsername + RandomNumberGenerator.GetInt32(100, 1000);
                    retries--;
                }
            }

            string password = !string.IsNullOrWhiteSpace(request.Password) 
                ? request.Password 
                : GenerateRandomPassword();

            var existingUser = await _userManager.FindByEmailAsync(request.Email);
            if (existingUser != null)
            {
                return new { success = false, message = "Email is already registered" };
            }

            // 2. Find Role and Department
            // This endpoint creates teachers only. Never trust a client-supplied
            // role here, otherwise Student Affairs could create an Admin account.
            var role = await _roleManager.Roles.FirstOrDefaultAsync(r => r.RoleName == "Teacher");
            if (role == null)
            {
                return new { success = false, message = "Teacher role is not configured" };
            }

            var deptStr = request.Department?.Trim() ?? "";
            var department = await _context.Departments.FirstOrDefaultAsync(d =>
                d.DepartmentName.ToLower() == deptStr.ToLower() ||
                d.Description.Contains(deptStr) ||
                d.DepartmentID.ToString() == deptStr)
                ?? await _context.Departments.FirstOrDefaultAsync(d => d.IsActive)
                ?? await _context.Departments.FirstOrDefaultAsync();

            if (department == null)
            {
                department = new Department
                {
                    DepartmentName = string.IsNullOrWhiteSpace(deptStr) ? "General" : deptStr,
                    Description = "General Department",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };
                _context.Departments.Add(department);
                await _context.SaveChangesAsync();
            }

            var cleanPhone = System.Text.RegularExpressions.Regex.Replace(request.Phone ?? "", @"\D", "");
            if (cleanPhone.Length < 8) cleanPhone = cleanPhone.PadLeft(8, '0');
            if (cleanPhone.Length > 15) cleanPhone = cleanPhone.Substring(0, 15);

            // Start Transaction to ensure both user and teacher are created or neither
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // 3. Create ApplicationUser
                var user = new ApplicationUser
                {
                    UserName = username,
                    Email = request.Email,
                    FirstName = request.FullName.FirstName,
                    MiddleName = request.FullName.MiddleName,
                    LastName = request.FullName.LastName,
                    FullName = $"{request.FullName.FirstName} {(string.IsNullOrEmpty(request.FullName.MiddleName) ? "" : request.FullName.MiddleName + " ")}{request.FullName.LastName}",
                    PhoneNumber = cleanPhone,
                    RoleId = role.RoleId,
                    IsActive = true,
                    CreatedAt = DateTime.Now
                };

                var result = await _userManager.CreateAsync(user, password);
                if (!result.Succeeded)
                {
                    await transaction.RollbackAsync();
                    return new { success = false, message = string.Join(", ", result.Errors.Select(e => e.Description)) };
                }

                // 4. If Role is Teacher, create Teacher record
                int? teacherId = null;
                if (role.RoleName.Equals("Teacher", StringComparison.OrdinalIgnoreCase))
                {
                    var teacher = new Teacher
                    {
                        UserID = user.UserId,
                        HireDate = request.HireDate,
                        DepartmentID = department.DepartmentID,
                        Qualifications = request.Qualifications.Trim(),
                        IsActive = true,
                        EmployeeCode = "TCH-" + Guid.NewGuid().ToString().Substring(0, 8).ToUpper()
                    };

                    _context.Teachers.Add(teacher);
                    await _context.SaveChangesAsync();
                    teacherId = teacher.TeacherID;
                }

                // Commit transaction if everything is successful
                await transaction.CommitAsync();

                // 5. Send Email (After commit, so we are sure data is saved)
                string emailBody = $@"
                    <h3>Welcome to Grade Management System</h3>
                    <p>Hello {user.FullName},</p>
                    <p>Your account has been created successfully.</p>
                    <p><b>Username:</b> {username}</p>
                    <p><b>Password:</b> {password}</p>
                    <p>Please change your password after your first login.</p>";

                try
                {
                    await _emailService.SendEmailAsync(user.Email, "Your Account Credentials", emailBody);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Teacher account {UserId} was created, but the credentials email could not be sent.", user.UserId);
                }

                return new { success = true, data = new TeacherResponse { Id = (teacherId ?? user.UserId).ToString(), FullName = user.FullName } };
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Teacher registration failed for {Email}.", request.Email);
                return new { success = false, message = "Teacher registration could not be completed." };
            }
        }

        private string GenerateRandomPassword()
        {
            // Simple random password generator meeting identity requirements
            string upper = "ABCDEFGHJKLMNOPQRSTUVWXYZ";
            string lower = "abcdefghijkmnopqrstuvwxyz";
            string digits = "0123456789";
            string nonAlphanumeric = "!@#$%^&*";
            return new string(new[]
            {
                upper[RandomNumberGenerator.GetInt32(upper.Length)],
                lower[RandomNumberGenerator.GetInt32(lower.Length)],
                digits[RandomNumberGenerator.GetInt32(digits.Length)],
                nonAlphanumeric[RandomNumberGenerator.GetInt32(nonAlphanumeric.Length)],
                lower[RandomNumberGenerator.GetInt32(lower.Length)],
                digits[RandomNumberGenerator.GetInt32(digits.Length)],
                upper[RandomNumberGenerator.GetInt32(upper.Length)],
                lower[RandomNumberGenerator.GetInt32(lower.Length)]
            });
        }

        private string GenerateRefreshToken()
        {
            var randomNumber = new byte[32];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }
    }
}
