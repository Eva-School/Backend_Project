using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using GradeManagementSystem.Core.DTOs.Admin.Account;
using GradeManagementSystem.Core.Entities.Domain;
using GradeManagementSystem.Core.Entities.Enums;
using GradeManagementSystem.Core.Entities.Identity;
using GradeManagementSystem.Core.Interfaces;
using GradeManagementSystem.Repository.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace GradeManagementSystem.Services.Services
{
    public class AdminAccountService : IAdminAccountService
    {
        private const long AdminConcurrencyLockKey = 421098;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<ApplicationRole> _roleManager;
        private readonly GradeDbContext _context;
        private readonly IMemoryCache _cache;
        private readonly ILogger<AdminAccountService> _logger;

        public AdminAccountService(
            UserManager<ApplicationUser> userManager,
            RoleManager<ApplicationRole> roleManager,
            GradeDbContext context,
            IMemoryCache cache,
            ILogger<AdminAccountService> logger)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;
            _cache = cache;
            _logger = logger;
        }

        public static string ResolveCanonicalRole(string roleInput)
        {
            if (string.IsNullOrWhiteSpace(roleInput))
            {
                throw new ArgumentException("Role cannot be empty.");
            }

            var clean = roleInput.Trim().ToLowerInvariant().Replace(" ", "").Replace("_", "").Replace("-", "");
            return clean switch
            {
                "admin" => "Admin",
                "studentaffairs" or "studentaffair" or "vice" => "Student Affairs",
                "teacher" => "Teacher",
                "student" => "Student",
                _ => throw new ArgumentException($"Role '{roleInput}' is not supported. Supported roles: Admin, Student Affairs, Teacher, Student.")
            };
        }

        public static string ToFrontendRole(string roleName)
        {
            return roleName.Equals("Student Affairs", StringComparison.OrdinalIgnoreCase)
                ? "StudentAffairs"
                : roleName;
        }

        public async Task<IReadOnlyList<RoleOptionDto>> GetRolesAsync(CancellationToken cancellationToken = default)
        {
            var roles = await _roleManager.Roles
                .AsNoTracking()
                .OrderBy(r => r.RoleId)
                .ToListAsync(cancellationToken);

            return roles.Select(r => new RoleOptionDto
            {
                RoleId = r.RoleId,
                RoleName = r.RoleName,
                NormalizedName = ToFrontendRole(r.RoleName),
                Description = r.Description
            }).ToList();
        }

        public async Task<AccountPagedResultDto<AccountSummaryDto>> GetAccountsAsync(AccountListQueryDto query, CancellationToken cancellationToken = default)
        {
            var usersQuery = _context.Users
                .AsNoTracking()
                .Include(u => u.Role)
                .AsQueryable();

            // Search filter
            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var term = query.Search.Trim().ToLower();
                usersQuery = usersQuery.Where(u =>
                    (u.UserName != null && u.UserName.ToLower().Contains(term)) ||
                    (u.Email != null && u.Email.ToLower().Contains(term)) ||
                    u.FullName.ToLower().Contains(term) ||
                    (u.PhoneNumber != null && u.PhoneNumber.Contains(term)));
            }

            // Role filter
            if (!string.IsNullOrWhiteSpace(query.Role))
            {
                try
                {
                    var canonicalRole = ResolveCanonicalRole(query.Role);
                    usersQuery = usersQuery.Where(u => u.Role.RoleName == canonicalRole);
                }
                catch (ArgumentException)
                {
                    // If an unknown role is requested, return empty results
                    return new AccountPagedResultDto<AccountSummaryDto>
                    {
                        Items = Array.Empty<AccountSummaryDto>(),
                        TotalCount = 0,
                        PageNumber = query.PageNumber,
                        PageSize = query.PageSize
                    };
                }
            }

            // Active status filter
            if (query.IsActive.HasValue)
            {
                usersQuery = usersQuery.Where(u => u.IsActive == query.IsActive.Value);
            }

            var totalCount = await usersQuery.CountAsync(cancellationToken);

            // Allowlisted sorting
            var sortBy = query.SortBy?.Trim().ToLowerInvariant() ?? "createdat";
            var desc = query.SortDescending;

            usersQuery = (sortBy, desc) switch
            {
                ("userid", false) => usersQuery.OrderBy(u => u.UserId),
                ("userid", true) => usersQuery.OrderByDescending(u => u.UserId),
                ("username", false) => usersQuery.OrderBy(u => u.UserName).ThenBy(u => u.UserId),
                ("username", true) => usersQuery.OrderByDescending(u => u.UserName).ThenBy(u => u.UserId),
                ("fullname", false) => usersQuery.OrderBy(u => u.FullName).ThenBy(u => u.UserId),
                ("fullname", true) => usersQuery.OrderByDescending(u => u.FullName).ThenBy(u => u.UserId),
                ("email", false) => usersQuery.OrderBy(u => u.Email).ThenBy(u => u.UserId),
                ("email", true) => usersQuery.OrderByDescending(u => u.Email).ThenBy(u => u.UserId),
                ("role", false) => usersQuery.OrderBy(u => u.Role.RoleName).ThenBy(u => u.UserId),
                ("role", true) => usersQuery.OrderByDescending(u => u.Role.RoleName).ThenBy(u => u.UserId),
                ("isactive", false) => usersQuery.OrderBy(u => u.IsActive).ThenBy(u => u.UserId),
                ("isactive", true) => usersQuery.OrderByDescending(u => u.IsActive).ThenBy(u => u.UserId),
                ("lastloginat", false) => usersQuery.OrderBy(u => u.LastLoginAt).ThenBy(u => u.UserId),
                ("lastloginat", true) => usersQuery.OrderByDescending(u => u.LastLoginAt).ThenBy(u => u.UserId),
                _ => desc ? usersQuery.OrderByDescending(u => u.CreatedAt).ThenBy(u => u.UserId)
                          : usersQuery.OrderBy(u => u.CreatedAt).ThenBy(u => u.UserId),
            };

            var items = await usersQuery
                .Skip((query.PageNumber - 1) * query.PageSize)
                .Take(query.PageSize)
                .Select(u => new AccountSummaryDto
                {
                    UserId = u.UserId,
                    Username = u.UserName ?? string.Empty,
                    FullName = u.FullName,
                    FirstName = u.FirstName,
                    MiddleName = u.MiddleName,
                    LastName = u.LastName,
                    Email = u.Email,
                    PhoneNumber = u.PhoneNumber,
                    Role = u.Role.RoleName,
                    NormalizedRole = u.Role.RoleName == "Student Affairs" ? "StudentAffairs" : u.Role.RoleName,
                    RoleId = u.RoleId,
                    IsActive = u.IsActive,
                    CreatedAt = u.CreatedAt,
                    LastLoginAt = u.LastLoginAt
                })
                .ToListAsync(cancellationToken);

            return new AccountPagedResultDto<AccountSummaryDto>
            {
                Items = items,
                TotalCount = totalCount,
                PageNumber = query.PageNumber,
                PageSize = query.PageSize
            };
        }

        public async Task<AccountDetailDto?> GetAccountByIdAsync(int userId, CancellationToken cancellationToken = default)
        {
            var user = await _context.Users
                .AsNoTracking()
                .Include(u => u.Role)
                .Include(u => u.Teacher).ThenInclude(t => t!.Department)
                .Include(u => u.Student).ThenInclude(s => s!.Department)
                .Include(u => u.Student).ThenInclude(s => s!.CurrentAcademicYear)
                .Include(u => u.Student).ThenInclude(s => s!.Class)
                .FirstOrDefaultAsync(u => u.UserId == userId, cancellationToken);

            if (user == null)
            {
                return null;
            }

            var detail = new AccountDetailDto
            {
                UserId = user.UserId,
                Username = user.UserName ?? string.Empty,
                FullName = user.FullName,
                FirstName = user.FirstName,
                MiddleName = user.MiddleName,
                LastName = user.LastName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                Role = user.Role?.RoleName ?? "Student",
                NormalizedRole = ToFrontendRole(user.Role?.RoleName ?? "Student"),
                RoleId = user.RoleId,
                IsActive = user.IsActive,
                CreatedAt = user.CreatedAt,
                LastLoginAt = user.LastLoginAt,
                EmailConfirmed = user.EmailConfirmed,
                PhoneNumberConfirmed = user.PhoneNumberConfirmed,
                TwoFactorEnabled = user.TwoFactorEnabled,
                LockoutEnd = user.LockoutEnd,
                AccessFailedCount = user.AccessFailedCount
            };

            if (user.Teacher != null)
            {
                detail.TeacherProfile = new TeacherProfileDto
                {
                    TeacherId = user.Teacher.TeacherID,
                    DepartmentId = user.Teacher.DepartmentID,
                    DepartmentName = user.Teacher.Department?.DepartmentName,
                    Qualifications = user.Teacher.Qualifications,
                    EmployeeCode = user.Teacher.EmployeeCode,
                    HireDate = user.Teacher.HireDate,
                    IsActive = user.Teacher.IsActive
                };
            }

            if (user.Student != null)
            {
                detail.StudentProfile = new StudentProfileDto
                {
                    StudentId = user.Student.StudentID,
                    StudentCode = user.Student.StudentCode,
                    NationalId = user.Student.NationalID,
                    Gender = user.Student.Gender.ToString(),
                    EnrollmentDate = user.Student.EnrollmentDate,
                    Status = user.Student.Status,
                    DepartmentId = user.Student.DepartmentID,
                    DepartmentName = user.Student.Department?.DepartmentName,
                    CurrentAcademicYearId = user.Student.CurrentAcademicYearID,
                    AcademicYearName = user.Student.CurrentAcademicYear?.YearName,
                    ClassId = user.Student.ClassID,
                    ClassName = user.Student.Class?.ClassName,
                    Address = user.Student.Address
                };
            }

            return detail;
        }

        public async Task<CreateAccountResultDto> CreateAccountAsync(
            CreateAccountDto request,
            int currentAdminUserId,
            string? ipAddress = null,
            CancellationToken cancellationToken = default)
        {
            var canonicalRole = ResolveCanonicalRole(request.Role);
            var role = await _roleManager.Roles.FirstOrDefaultAsync(r => r.RoleName == canonicalRole, cancellationToken);
            if (role == null)
            {
                throw new InvalidOperationException($"Role '{canonicalRole}' is not configured in the database.");
            }

            // Check username & email uniqueness
            var cleanUsername = request.Username.Trim();
            if (await _userManager.FindByNameAsync(cleanUsername) != null)
            {
                throw new InvalidOperationException($"Username '{cleanUsername}' is already taken.");
            }

            var cleanEmail = request.Email.Trim().ToLowerInvariant();
            if (await _userManager.FindByEmailAsync(cleanEmail) != null)
            {
                throw new InvalidOperationException($"Email '{cleanEmail}' is already registered.");
            }

            // Password handling
            string passwordToUse;
            string? generatedPassword = null;

            if (string.IsNullOrWhiteSpace(request.Password))
            {
                passwordToUse = GenerateSecurePassword();
                generatedPassword = passwordToUse;
            }
            else
            {
                passwordToUse = request.Password;
                await ValidatePasswordPolicyAsync(passwordToUse);
            }

            using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                var cleanMiddle = string.IsNullOrWhiteSpace(request.MiddleName) ? null : request.MiddleName.Trim();
                var cleanFirst = request.FirstName.Trim();
                var cleanLast = request.LastName.Trim();
                var fullName = $"{cleanFirst} {(string.IsNullOrWhiteSpace(cleanMiddle) ? "" : cleanMiddle + " ")}{cleanLast}";

                var user = new ApplicationUser
                {
                    UserName = cleanUsername,
                    Email = cleanEmail,
                    FirstName = cleanFirst,
                    MiddleName = cleanMiddle,
                    LastName = cleanLast,
                    FullName = fullName,
                    PhoneNumber = string.IsNullOrWhiteSpace(request.PhoneNumber) ? null : request.PhoneNumber.Trim(),
                    RoleId = role.RoleId,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    SecurityStamp = Guid.NewGuid().ToString(),
                    EmailConfirmed = false
                };

                var createResult = await _userManager.CreateAsync(user, passwordToUse);
                if (!createResult.Succeeded)
                {
                    var errors = string.Join("; ", createResult.Errors.Select(e => e.Description));
                    throw new InvalidOperationException($"Account creation failed: {errors}");
                }

                // Ensure Id and UserId match
                if (user.Id != user.UserId)
                {
                    user.Id = user.UserId;
                    await _userManager.UpdateAsync(user);
                }

                // Role-specific profile provisioning
                if (canonicalRole == "Teacher")
                {
                    var department = await ResolveDepartmentAsync(request.DepartmentId, cancellationToken);
                    var teacher = new Teacher
                    {
                        UserID = user.UserId,
                        DepartmentID = department.DepartmentID,
                        Qualifications = string.IsNullOrWhiteSpace(request.Qualifications) ? "Faculty" : request.Qualifications.Trim(),
                        EmployeeCode = $"TCH-{Guid.NewGuid():N}"[..12].ToUpperInvariant(),
                        HireDate = DateTime.UtcNow,
                        IsActive = true
                    };
                    _context.Teachers.Add(teacher);
                    await _context.SaveChangesAsync(cancellationToken);
                }
                else if (canonicalRole == "Student")
                {
                    var department = await ResolveDepartmentAsync(request.DepartmentId, cancellationToken);
                    var academicYear = await ResolveAcademicYearAsync(request.AcademicYearId, cancellationToken);
                    var gender = ParseGender(request.Gender);

                    var nationalId = string.IsNullOrWhiteSpace(request.NationalId)
                        ? $"NAT-{Guid.NewGuid():N}"[..14].ToUpperInvariant()
                        : request.NationalId.Trim();

                    var studentCode = string.IsNullOrWhiteSpace(request.StudentCode)
                        ? $"STD-{Guid.NewGuid():N}"[..10].ToUpperInvariant()
                        : request.StudentCode.Trim();

                    var student = new Student
                    {
                        UserID = user.UserId,
                        NationalID = nationalId,
                        StudentCode = studentCode,
                        EnrollmentDate = DateTime.UtcNow.Date,
                        CurrentAcademicYearID = academicYear?.AcademicYearID,
                        DepartmentID = department.DepartmentID,
                        ClassID = request.ClassId,
                        Status = "Active",
                        Gender = gender,
                        Address = request.Address?.Trim()
                    };
                    _context.Students.Add(student);
                    await _context.SaveChangesAsync(cancellationToken);
                }

                await transaction.CommitAsync(cancellationToken);

                await RecordAuditLogAsync(
                    currentAdminUserId,
                    user.UserId,
                    user.UserName,
                    "CreateAccount",
                    "Success",
                    $"Created account with role {canonicalRole}",
                    ipAddress,
                    cancellationToken);

                var createdDetail = await GetAccountByIdAsync(user.UserId, cancellationToken);
                return new CreateAccountResultDto
                {
                    Account = createdDetail!,
                    GeneratedInitialPassword = generatedPassword
                };
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogError(ex, "Failed to create account for username {Username}", cleanUsername);
                throw;
            }
        }

        public async Task<AccountDetailDto> UpdateAccountAsync(
            int userId,
            UpdateAccountProfileDto request,
            int currentAdminUserId,
            string? ipAddress = null,
            CancellationToken cancellationToken = default)
        {
            var user = await _context.Users
                .Include(u => u.Role)
                .Include(u => u.Teacher)
                .Include(u => u.Student)
                .FirstOrDefaultAsync(u => u.UserId == userId, cancellationToken);

            if (user == null)
            {
                throw new KeyNotFoundException($"Account with ID {userId} was not found.");
            }

            var cleanEmail = request.Email.Trim().ToLowerInvariant();
            if (!string.Equals(user.Email, cleanEmail, StringComparison.OrdinalIgnoreCase))
            {
                var existingUser = await _userManager.FindByEmailAsync(cleanEmail);
                if (existingUser != null && existingUser.UserId != userId)
                {
                    throw new InvalidOperationException($"Email '{cleanEmail}' is already registered to another account.");
                }

                user.Email = cleanEmail;
                user.NormalizedEmail = cleanEmail.ToUpperInvariant();
                user.EmailConfirmed = false;
            }

            var cleanFirst = request.FirstName.Trim();
            var cleanMiddle = string.IsNullOrWhiteSpace(request.MiddleName) ? null : request.MiddleName.Trim();
            var cleanLast = request.LastName.Trim();

            user.FirstName = cleanFirst;
            user.MiddleName = cleanMiddle;
            user.LastName = cleanLast;
            user.FullName = $"{cleanFirst} {(string.IsNullOrWhiteSpace(cleanMiddle) ? "" : cleanMiddle + " ")}{cleanLast}";
            user.PhoneNumber = string.IsNullOrWhiteSpace(request.PhoneNumber) ? null : request.PhoneNumber.Trim();

            // Update Teacher-specific profile fields if present
            if (user.Teacher != null)
            {
                if (request.DepartmentId.HasValue && request.DepartmentId.Value > 0)
                {
                    var dept = await ResolveDepartmentAsync(request.DepartmentId, cancellationToken);
                    user.Teacher.DepartmentID = dept.DepartmentID;
                }
                if (!string.IsNullOrWhiteSpace(request.Qualifications))
                {
                    user.Teacher.Qualifications = request.Qualifications.Trim();
                }
            }

            // Update Student-specific profile fields if present
            if (user.Student != null)
            {
                if (request.DepartmentId.HasValue && request.DepartmentId.Value > 0)
                {
                    var dept = await ResolveDepartmentAsync(request.DepartmentId, cancellationToken);
                    user.Student.DepartmentID = dept.DepartmentID;
                }
                if (!string.IsNullOrWhiteSpace(request.NationalId))
                {
                    user.Student.NationalID = request.NationalId.Trim();
                }
                if (!string.IsNullOrWhiteSpace(request.StudentCode))
                {
                    user.Student.StudentCode = request.StudentCode.Trim();
                }
                if (!string.IsNullOrWhiteSpace(request.Gender))
                {
                    user.Student.Gender = ParseGender(request.Gender);
                }
                if (request.Address != null)
                {
                    user.Student.Address = string.IsNullOrWhiteSpace(request.Address) ? null : request.Address.Trim();
                }
            }

            await _context.SaveChangesAsync(cancellationToken);
            _cache.Remove($"auth_user_{userId}");

            await RecordAuditLogAsync(
                currentAdminUserId,
                user.UserId,
                user.UserName ?? "",
                "UpdateProfile",
                "Success",
                "Updated profile details",
                ipAddress,
                cancellationToken);

            var updated = await GetAccountByIdAsync(userId, cancellationToken);
            return updated!;
        }

        public async Task<bool> ChangeRoleAsync(
            int userId,
            ChangeUserRoleDto request,
            int currentAdminUserId,
            string? ipAddress = null,
            CancellationToken cancellationToken = default)
        {
            var targetCanonicalRole = ResolveCanonicalRole(request.NewRole);
            var targetRole = await _roleManager.Roles.FirstOrDefaultAsync(r => r.RoleName == targetCanonicalRole, cancellationToken);
            if (targetRole == null)
            {
                throw new InvalidOperationException($"Role '{targetCanonicalRole}' was not found.");
            }

            using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                // Serialize mutations affecting administrators
                await _context.Database.ExecuteSqlRawAsync($"SELECT pg_advisory_xact_lock({AdminConcurrencyLockKey});", cancellationToken);

                var user = await _context.Users
                    .Include(u => u.Role)
                    .Include(u => u.Teacher)
                    .Include(u => u.Student)
                    .FirstOrDefaultAsync(u => u.UserId == userId, cancellationToken);

                if (user == null)
                {
                    throw new KeyNotFoundException($"Account with ID {userId} was not found.");
                }

                var oldRoleName = user.Role?.RoleName ?? "Unknown";
                if (oldRoleName.Equals(targetCanonicalRole, StringComparison.OrdinalIgnoreCase))
                {
                    // No change needed
                    return true;
                }

                // Invariant 1: Self-demotion block
                if (userId == currentAdminUserId && oldRoleName == "Admin" && targetCanonicalRole != "Admin")
                {
                    throw new InvalidOperationException("Administrators cannot remove the Admin role from their own account.");
                }

                // Invariant 2: Last active Admin protection
                if (oldRoleName == "Admin" && user.IsActive && targetCanonicalRole != "Admin")
                {
                    var activeAdminCount = await _context.Users
                        .CountAsync(u => u.IsActive && u.Role.RoleName == "Admin", cancellationToken);
                    if (activeAdminCount <= 1)
                    {
                        throw new InvalidOperationException("Operation rejected: Cannot change the role of the last remaining active Administrator.");
                    }
                }

                // Role assignment
                user.RoleId = targetRole.RoleId;

                // Handle domain profile transitions
                if (targetCanonicalRole == "Teacher")
                {
                    if (user.Teacher != null)
                    {
                        // Reactivate existing teacher record
                        user.Teacher.IsActive = true;
                        if (request.DepartmentId.HasValue && request.DepartmentId.Value > 0)
                        {
                            var dept = await ResolveDepartmentAsync(request.DepartmentId, cancellationToken);
                            user.Teacher.DepartmentID = dept.DepartmentID;
                        }
                        if (!string.IsNullOrWhiteSpace(request.Qualifications))
                        {
                            user.Teacher.Qualifications = request.Qualifications.Trim();
                        }
                    }
                    else
                    {
                        var dept = await ResolveDepartmentAsync(request.DepartmentId, cancellationToken);
                        user.Teacher = new Teacher
                        {
                            UserID = user.UserId,
                            DepartmentID = dept.DepartmentID,
                            Qualifications = string.IsNullOrWhiteSpace(request.Qualifications) ? "Faculty" : request.Qualifications.Trim(),
                            EmployeeCode = $"TCH-{Guid.NewGuid():N}"[..12].ToUpperInvariant(),
                            HireDate = DateTime.UtcNow,
                            IsActive = true
                        };
                        _context.Teachers.Add(user.Teacher);
                    }
                }
                else if (targetCanonicalRole == "Student")
                {
                    if (user.Student != null)
                    {
                        user.Student.Status = "Active";
                        if (request.DepartmentId.HasValue && request.DepartmentId.Value > 0)
                        {
                            var dept = await ResolveDepartmentAsync(request.DepartmentId, cancellationToken);
                            user.Student.DepartmentID = dept.DepartmentID;
                        }
                    }
                    else
                    {
                        var dept = await ResolveDepartmentAsync(request.DepartmentId, cancellationToken);
                        var year = await ResolveAcademicYearAsync(request.AcademicYearId, cancellationToken);
                        user.Student = new Student
                        {
                            UserID = user.UserId,
                            NationalID = string.IsNullOrWhiteSpace(request.NationalId) ? $"NAT-{Guid.NewGuid():N}"[..14].ToUpperInvariant() : request.NationalId.Trim(),
                            StudentCode = string.IsNullOrWhiteSpace(request.StudentCode) ? $"STD-{Guid.NewGuid():N}"[..10].ToUpperInvariant() : request.StudentCode.Trim(),
                            EnrollmentDate = DateTime.UtcNow.Date,
                            CurrentAcademicYearID = year?.AcademicYearID,
                            DepartmentID = dept.DepartmentID,
                            ClassID = request.ClassId,
                            Status = "Active",
                            Gender = ParseGender(request.Gender),
                            Address = request.Address?.Trim()
                        };
                        _context.Students.Add(user.Student);
                    }
                }

                // If leaving Teacher, soft-deactivate teacher record
                if (oldRoleName == "Teacher" && targetCanonicalRole != "Teacher" && user.Teacher != null)
                {
                    user.Teacher.IsActive = false;
                }

                // If leaving Student, soft-deactivate student record
                if (oldRoleName == "Student" && targetCanonicalRole != "Student" && user.Student != null)
                {
                    user.Student.Status = "Inactive";
                }

                // Revoke existing access and refresh tokens
                user.SecurityStamp = Guid.NewGuid().ToString();
                user.RefreshToken = null;
                user.RefreshTokenExpiryTime = null;

                await _context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                _cache.Remove($"auth_user_{userId}");

                await RecordAuditLogAsync(
                    currentAdminUserId,
                    user.UserId,
                    user.UserName ?? "",
                    "ChangeRole",
                    "Success",
                    $"Role changed from '{oldRoleName}' to '{targetCanonicalRole}'. All active sessions revoked.",
                    ipAddress,
                    cancellationToken);

                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogError(ex, "Failed to change role for user {UserId} to {NewRole}", userId, request.NewRole);
                throw;
            }
        }

        public async Task<bool> SetStatusAsync(
            int userId,
            bool isActive,
            int currentAdminUserId,
            string? ipAddress = null,
            CancellationToken cancellationToken = default)
        {
            using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                await _context.Database.ExecuteSqlRawAsync($"SELECT pg_advisory_xact_lock({AdminConcurrencyLockKey});", cancellationToken);

                var user = await _context.Users
                    .Include(u => u.Role)
                    .Include(u => u.Teacher)
                    .Include(u => u.Student)
                    .FirstOrDefaultAsync(u => u.UserId == userId, cancellationToken);

                if (user == null)
                {
                    throw new KeyNotFoundException($"Account with ID {userId} was not found.");
                }

                if (user.IsActive == isActive)
                {
                    return true;
                }

                // Invariant 1: Self-deactivation block
                if (userId == currentAdminUserId && !isActive)
                {
                    throw new InvalidOperationException("Administrators cannot deactivate their own account.");
                }

                // Invariant 2: Last active Admin protection
                if (user.Role?.RoleName == "Admin" && user.IsActive && !isActive)
                {
                    var activeAdminCount = await _context.Users
                        .CountAsync(u => u.IsActive && u.Role.RoleName == "Admin", cancellationToken);
                    if (activeAdminCount <= 1)
                    {
                        throw new InvalidOperationException("Operation rejected: Cannot deactivate the last remaining active Administrator.");
                    }
                }

                user.IsActive = isActive;

                if (user.Teacher != null)
                {
                    user.Teacher.IsActive = isActive;
                }
                if (user.Student != null)
                {
                    user.Student.Status = isActive ? "Active" : "Inactive";
                }

                if (!isActive)
                {
                    // Revoke sessions immediately on deactivation
                    user.SecurityStamp = Guid.NewGuid().ToString();
                    user.RefreshToken = null;
                    user.RefreshTokenExpiryTime = null;
                }

                await _context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                _cache.Remove($"auth_user_{userId}");

                await RecordAuditLogAsync(
                    currentAdminUserId,
                    user.UserId,
                    user.UserName ?? "",
                    "ToggleStatus",
                    "Success",
                    $"Account status changed to {(isActive ? "Active" : "Inactive")}. {(isActive ? "" : "All active sessions revoked.")}",
                    ipAddress,
                    cancellationToken);

                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogError(ex, "Failed to set status for user {UserId} to {IsActive}", userId, isActive);
                throw;
            }
        }

        public async Task<bool> ResetPasswordAsync(
            int userId,
            ResetPasswordDto request,
            int currentAdminUserId,
            string? ipAddress = null,
            CancellationToken cancellationToken = default)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
            {
                throw new KeyNotFoundException($"Account with ID {userId} was not found.");
            }

            await ValidatePasswordPolicyAsync(request.NewPassword, user);

            var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);
            var resetResult = await _userManager.ResetPasswordAsync(user, resetToken, request.NewPassword);

            if (!resetResult.Succeeded)
            {
                var errors = string.Join("; ", resetResult.Errors.Select(e => e.Description));
                throw new InvalidOperationException($"Password reset failed: {errors}");
            }

            // Immediately revoke existing access and refresh tokens
            await _userManager.UpdateSecurityStampAsync(user);
            user.RefreshToken = null;
            user.RefreshTokenExpiryTime = null;
            await _userManager.UpdateAsync(user);

            _cache.Remove($"auth_user_{userId}");

            await RecordAuditLogAsync(
                currentAdminUserId,
                user.UserId,
                user.UserName ?? "",
                "ResetPassword",
                "Success",
                "Password was reset by administrator. All active sessions revoked.",
                ipAddress,
                cancellationToken);

            return true;
        }

        private async Task ValidatePasswordPolicyAsync(string password, ApplicationUser? user = null)
        {
            var dummyUser = user ?? new ApplicationUser { UserName = "validation_check" };
            foreach (var validator in _userManager.PasswordValidators)
            {
                var result = await validator.ValidateAsync(_userManager, dummyUser, password);
                if (!result.Succeeded)
                {
                    var errors = string.Join("; ", result.Errors.Select(e => e.Description));
                    throw new InvalidOperationException($"Password does not meet requirements: {errors}");
                }
            }
        }

        private string GenerateSecurePassword()
        {
            const string upper = "ABCDEFGHJKLMNPQRSTUVWXYZ";
            const string lower = "abcdefghijkmnopqrstuvwxyz";
            const string digits = "23456789";
            const string special = "!@#$%^&*";
            const string all = upper + lower + digits + special;

            var chars = new char[16];
            chars[0] = upper[RandomNumberGenerator.GetInt32(upper.Length)];
            chars[1] = lower[RandomNumberGenerator.GetInt32(lower.Length)];
            chars[2] = digits[RandomNumberGenerator.GetInt32(digits.Length)];
            chars[3] = special[RandomNumberGenerator.GetInt32(special.Length)];

            for (int i = 4; i < 16; i++)
            {
                chars[i] = all[RandomNumberGenerator.GetInt32(all.Length)];
            }

            // Shuffle
            return new string(chars.OrderBy(_ => RandomNumberGenerator.GetInt32(1000)).ToArray());
        }

        private async Task<Department> ResolveDepartmentAsync(int? departmentId, CancellationToken cancellationToken)
        {
            if (departmentId.HasValue && departmentId.Value > 0)
            {
                var found = await _context.Departments.FirstOrDefaultAsync(d => d.DepartmentID == departmentId.Value, cancellationToken);
                if (found != null) return found;
            }

            var defaultDept = await _context.Departments.FirstOrDefaultAsync(d => d.IsActive, cancellationToken)
                              ?? await _context.Departments.FirstOrDefaultAsync(cancellationToken);

            if (defaultDept == null)
            {
                defaultDept = new Department
                {
                    DepartmentName = "General",
                    Description = "General Department",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };
                _context.Departments.Add(defaultDept);
                await _context.SaveChangesAsync(cancellationToken);
            }

            return defaultDept;
        }

        private async Task<AcademicYear?> ResolveAcademicYearAsync(int? yearId, CancellationToken cancellationToken)
        {
            if (yearId.HasValue && yearId.Value > 0)
            {
                var found = await _context.AcademicYears.FirstOrDefaultAsync(y => y.AcademicYearID == yearId.Value, cancellationToken);
                if (found != null) return found;
            }

            return await _context.AcademicYears
                .Where(y => y.IsActive)
                .OrderByDescending(y => y.AcademicYearID)
                .FirstOrDefaultAsync(cancellationToken);
        }

        private static Gender ParseGender(string? genderInput)
        {
            if (string.IsNullOrWhiteSpace(genderInput)) return Gender.Male;
            return Enum.TryParse<Gender>(genderInput, true, out var parsed) ? parsed : Gender.Male;
        }

        private async Task RecordAuditLogAsync(
            int actorUserId,
            int targetUserId,
            string targetUsername,
            string action,
            string outcome,
            string details,
            string? ipAddress,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var actorUser = await _context.Users.AsNoTracking()
                    .Where(u => u.UserId == actorUserId)
                    .Select(u => u.UserName)
                    .FirstOrDefaultAsync(cancellationToken);

                var log = new AccountAuditLog
                {
                    Action = action,
                    ActorUserId = actorUserId,
                    ActorUsername = actorUser ?? $"User_{actorUserId}",
                    TargetUserId = targetUserId,
                    TargetUsername = targetUsername,
                    Timestamp = DateTime.UtcNow,
                    Outcome = outcome,
                    Details = details,
                    IpAddress = ipAddress
                };

                _context.AccountAuditLogs.Add(log);
                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation(
                    "AUDIT: Actor {Actor} ({ActorId}) performed {Action} on Target {Target} ({TargetId}) - {Outcome}. {Details}",
                    log.ActorUsername, actorUserId, action, targetUsername, targetUserId, outcome, details);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to write account audit log for Action {Action} on User {TargetUserId}", action, targetUserId);
            }
        }
    }
}
