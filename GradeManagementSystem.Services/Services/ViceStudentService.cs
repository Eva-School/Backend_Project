using GradeManagementSystem.Core.DTOs.Vice;
using GradeManagementSystem.Core.Entities.Domain;
using GradeManagementSystem.Core.Entities.Enums;
using GradeManagementSystem.Core.Entities.Identity;
using GradeManagementSystem.Core.Interfaces;
using GradeManagementSystem.Repository.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GradeManagementSystem.Services.Services
{
    public class ViceStudentService : IViceStudentService
    {
        private readonly GradeDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<ApplicationRole> _roleManager;

        public ViceStudentService(
            GradeDbContext context,
            UserManager<ApplicationUser> userManager,
            RoleManager<ApplicationRole> roleManager)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        public async Task<List<ViceStudentDto>> GetStudentsAsync(string year, string department, int? classId)
        {
            if (!Enum.TryParse<EducationStage>(year, true, out var stage))
            {
                throw new ArgumentException("Invalid year value. Expected: junior|wheeler|senior.");
            }

            // In the spec Department uses OM/SD. We map them to DepartmentName.
            var departmentName = department.Trim();

            var academicYear = await _context.AcademicYears
                .Where(a => a.IsActive && a.Stage == stage)
                .OrderByDescending(a => a.AcademicYearID)
                .FirstOrDefaultAsync();

            if (academicYear == null)
            {
                return new List<ViceStudentDto>();
            }

            var dept = await _context.Departments
                .FirstOrDefaultAsync(d => d.IsActive && d.DepartmentName == departmentName);

            if (dept == null)
            {
                return new List<ViceStudentDto>();
            }

            var query = _context.Students
                .AsNoTracking()
                .Where(s => s.CurrentAcademicYearID == academicYear.AcademicYearID)
                .Where(s => s.ClassID != null)
                .Where(s => s.UserID.HasValue);

            if (classId.HasValue)
            {
                query = query.Where(s => s.ClassID == classId.Value);
            }

            // Join users for full name, join class for class+department.
            var students = await query
                .Join(_context.Users,
                    s => s.UserID!.Value,
                    u => u.UserId,
                    (s, u) => new { Student = s, User = u })
                .Join(_context.Classes,
                    x => x.Student.ClassID!.Value,
                    c => c.ClassID,
                    (x, c) => new { x.Student, x.User, Class = c })
                .Where(x => x.Class.DepartmentID == dept.DepartmentID && x.Class.IsActive)
                .OrderBy(x => x.Class.ClassID)
                .ThenBy(x => x.Student.StudentID)
                .Select(x => new ViceStudentDto
                {
                    Id = x.Student.StudentID.ToString(),
                    ClassId = x.Class.ClassID,
                    StudentCode = x.Student.NationalID ?? string.Empty,
                    Name = x.User.FullName ?? string.Empty,
                    Department = dept.DepartmentName,
                    ClassName = x.Class.ClassName,
                    Year = year.ToLowerInvariant()
                })
                .ToListAsync();

            return students;
        }

        public async Task<ViceStudentDto?> CreateStudentAsync(ViceCreateStudentRequestDTO request)
        {
            if (request == null)
            {
                return null;
            }

            if (!Enum.TryParse<EducationStage>(request.Year, true, out var stage))
            {
                throw new ArgumentException("Invalid year value. Expected: junior|wheeler|senior.");
            }

            var academicYear = await _context.AcademicYears
                .Where(a => a.IsActive && a.Stage == stage)
                .OrderByDescending(a => a.AcademicYearID)
                .FirstOrDefaultAsync();

            if (academicYear == null)
            {
                return null;
            }

            var role = await _roleManager.Roles.FirstOrDefaultAsync(r => r.RoleName == "Student");
            if (role == null)
            {
                throw new InvalidOperationException("Student role not found.");
            }

            var dept = await _context.Departments.FirstOrDefaultAsync(d => d.IsActive && d.DepartmentName == request.Department);
            if (dept == null)
            {
                return null;
            }

            var cls = await _context.Classes.FirstOrDefaultAsync(c => c.IsActive && c.ClassID == request.ClassId && c.DepartmentID == dept.DepartmentID);
            if (cls == null)
            {
                return null;
            }

            var username = (request.FirstName + "." + request.LastName).Replace(" ", "").ToLowerInvariant() + "-" + new Random().Next(100, 999);

            // Create app user.
            var user = new ApplicationUser
            {
                UserName = username,
                Email = request.Email,
                FirstName = request.FirstName,
                MiddleName = request.MiddleName,
                LastName = request.LastName,
                FullName = $"{request.FirstName} {(string.IsNullOrWhiteSpace(request.MiddleName) ? "" : request.MiddleName + " ")}{request.LastName}",
                PhoneNumber = request.Phone,
                RoleId = role.RoleId,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                EmailConfirmed = true
            };

            var created = await _userManager.CreateAsync(user, "Student@123");
            if (!created.Succeeded)
            {
                throw new InvalidOperationException("Unable to create student user.");
            }

            var student = new Student
            {
                UserID = user.UserId,
                NationalID = request.StudentCode,
                EnrollmentDate = DateTime.UtcNow.Date,
                CurrentAcademicYearID = academicYear.AcademicYearID,
                MajorID = null,
                ClassID = cls.ClassID,
                Status = "Active",
                Gender = Gender.Male
            };

            _context.Students.Add(student);
            await _context.SaveChangesAsync();

            return new ViceStudentDto
            {
                Id = student.StudentID.ToString(),
                ClassId = cls.ClassID,
                StudentCode = student.NationalID ?? string.Empty,
                Name = user.FullName ?? string.Empty,
                Department = dept.DepartmentName,
                ClassName = cls.ClassName,
                Year = request.Year.ToLowerInvariant()
            };
        }

        public async Task<ViceStudentDto?> UpdateStudentAsync(string studentId, ViceCreateStudentRequestDTO request)
        {
            if (string.IsNullOrWhiteSpace(studentId))
            {
                return null;
            }

            if (!int.TryParse(studentId, out var studentPk))
            {
                return null;
            }

            var student = await _context.Students.FirstOrDefaultAsync(s => s.StudentID == studentPk);
            if (student == null || student.UserID == null)
            {
                return null;
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == student.UserID.Value);
            if (user == null)
            {
                return null;
            }

            user.FirstName = request.FirstName;
            user.MiddleName = request.MiddleName;
            user.LastName = request.LastName;
            user.FullName = $"{request.FirstName} {(string.IsNullOrWhiteSpace(request.MiddleName) ? "" : request.MiddleName + " ")}{request.LastName}";
            user.Email = request.Email;
            user.PhoneNumber = request.Phone;

            await _userManager.UpdateAsync(user);

            // Update student code/year/class.
            student.NationalID = request.StudentCode;
            if (Enum.TryParse<EducationStage>(request.Year, true, out var stage))
            {
                var academicYear = await _context.AcademicYears
                    .Where(a => a.IsActive && a.Stage == stage)
                    .OrderByDescending(a => a.AcademicYearID)
                    .FirstOrDefaultAsync();

                if (academicYear != null)
                {
                    student.CurrentAcademicYearID = academicYear.AcademicYearID;
                }
            }

            var dept = await _context.Departments.FirstOrDefaultAsync(d => d.IsActive && d.DepartmentName == request.Department);
            if (dept != null)
            {
                var cls = await _context.Classes.FirstOrDefaultAsync(c =>
                    c.IsActive && c.ClassID == request.ClassId && c.DepartmentID == dept.DepartmentID);
                if (cls != null)
                {
                    student.ClassID = cls.ClassID;
                }
            }

            await _context.SaveChangesAsync();

            return new ViceStudentDto
            {
                Id = student.StudentID.ToString(),
                ClassId = student.ClassID ?? 0,
                StudentCode = student.NationalID ?? string.Empty,
                Name = user.FullName ?? string.Empty,
                Department = request.Department,
                ClassName = (await _context.Classes.FirstOrDefaultAsync(c => c.ClassID == student.ClassID))?.ClassName ?? string.Empty,
                Year = request.Year.ToLowerInvariant()
            };
        }

        public async Task<bool> DeleteStudentAsync(string studentId)
        {
            if (!int.TryParse(studentId, out var pk))
            {
                return false;
            }

            var student = await _context.Students.FirstOrDefaultAsync(s => s.StudentID == pk);
            if (student == null || student.UserID == null)
            {
                return false;
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == student.UserID.Value);

            _context.Students.Remove(student);
            await _context.SaveChangesAsync();

            if (user != null)
            {
                await _userManager.DeleteAsync(user);
            }

            return true;
        }
    }
}

