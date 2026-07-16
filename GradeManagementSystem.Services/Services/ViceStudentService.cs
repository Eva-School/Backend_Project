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

        public async Task<List<ViceStudentDto>> GetStudentsAsync(string year, string department, int? classId, bool unassigned = false, string? academicYearName = null)
        {
            if (!Enum.TryParse<EducationStage>(year, true, out var stage))
            {
                throw new ArgumentException("Invalid year value. Expected: junior|wheeler|senior.");
            }

            // In the spec Department uses OM/SD. We map them to DepartmentName.
            var departmentName = department.Trim();

            var academicYears = _context.AcademicYears.Where(a => a.Stage == stage);
            if (!string.IsNullOrWhiteSpace(academicYearName))
            {
                academicYears = academicYears.Where(a => a.YearName == academicYearName.Trim());
            }
            else
            {
                academicYears = academicYears.Where(a => a.IsActive);
            }

            var academicYear = await academicYears
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

            if (unassigned)
            {
                return await _context.Students
                    .AsNoTracking()
                    .Where(s => s.CurrentAcademicYearID == academicYear.AcademicYearID)
                    .Where(s => s.ClassID == null && s.DepartmentID == dept.DepartmentID)
                    .Where(s => s.UserID.HasValue)
                    .Join(_context.Users,
                        s => s.UserID!.Value,
                        u => u.UserId,
                        (s, u) => new { Student = s, User = u })
                    .OrderBy(x => x.Student.StudentID)
                    .Select(x => new ViceStudentDto
                    {
                        Id = x.Student.StudentID.ToString(),
                        ClassId = 0,
                        StudentCode = x.Student.NationalID ?? string.Empty,
                        Name = x.User.FullName ?? string.Empty,
                        FirstName = x.User.FirstName,
                        MiddleName = x.User.MiddleName ?? string.Empty,
                        LastName = x.User.LastName,
                        Email = x.User.Email ?? string.Empty,
                        Phone = x.User.PhoneNumber ?? string.Empty,
                        Department = dept.DepartmentName,
                        ClassName = string.Empty,
                        Year = year.ToLowerInvariant(),
                        AcademicYearName = academicYear.YearName
                    })
                    .ToListAsync();
            }

            if (!classId.HasValue)
            {
                return await _context.Students
                    .AsNoTracking()
                    .Where(s => s.CurrentAcademicYearID == academicYear.AcademicYearID)
                    .Where(s => s.UserID.HasValue)
                    .Join(_context.Users,
                        s => s.UserID!.Value,
                        u => u.UserId,
                        (s, u) => new { Student = s, User = u })
                    .GroupJoin(_context.Classes,
                        x => x.Student.ClassID,
                        c => (int?)c.ClassID,
                        (x, classes) => new { x.Student, x.User, Classes = classes })
                    .SelectMany(
                        x => x.Classes.DefaultIfEmpty(),
                        (x, cls) => new { x.Student, x.User, Class = cls })
                    .Where(x => x.Student.DepartmentID == dept.DepartmentID ||
                                (x.Class != null && x.Class.DepartmentID == dept.DepartmentID && x.Class.IsActive))
                    .OrderBy(x => x.Student.StudentID)
                    .Select(x => new ViceStudentDto
                    {
                        Id = x.Student.StudentID.ToString(),
                        ClassId = x.Student.ClassID ?? 0,
                        StudentCode = x.Student.NationalID ?? string.Empty,
                        Name = x.User.FullName ?? string.Empty,
                        FirstName = x.User.FirstName,
                        MiddleName = x.User.MiddleName ?? string.Empty,
                        LastName = x.User.LastName,
                        Email = x.User.Email ?? string.Empty,
                        Phone = x.User.PhoneNumber ?? string.Empty,
                        Department = dept.DepartmentName,
                        ClassName = x.Class != null ? x.Class.ClassName : string.Empty,
                        Year = year.ToLowerInvariant(),
                        AcademicYearName = academicYear.YearName
                    })
                    .ToListAsync();
            }

            return await _context.Students
                .AsNoTracking()
                .Where(s => s.CurrentAcademicYearID == academicYear.AcademicYearID)
                .Where(s => s.ClassID == classId.Value && s.UserID.HasValue)
                .Join(_context.Users,
                    s => s.UserID!.Value,
                    u => u.UserId,
                    (s, u) => new { Student = s, User = u })
                .Join(_context.Classes,
                    x => x.Student.ClassID!.Value,
                    c => c.ClassID,
                    (x, c) => new { x.Student, x.User, Class = c })
                .Where(x => x.Class.DepartmentID == dept.DepartmentID && x.Class.IsActive)
                .OrderBy(x => x.Student.StudentID)
                .Select(x => new ViceStudentDto
                {
                    Id = x.Student.StudentID.ToString(),
                    ClassId = x.Class.ClassID,
                    StudentCode = x.Student.NationalID ?? string.Empty,
                    Name = x.User.FullName ?? string.Empty,
                    FirstName = x.User.FirstName,
                    MiddleName = x.User.MiddleName ?? string.Empty,
                    LastName = x.User.LastName,
                    Email = x.User.Email ?? string.Empty,
                    Phone = x.User.PhoneNumber ?? string.Empty,
                    Department = dept.DepartmentName,
                    ClassName = x.Class.ClassName,
                    Year = year.ToLowerInvariant(),
                    AcademicYearName = academicYear.YearName
                })
                .ToListAsync();
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

            var studentCode = request.StudentCode.Trim();
            var email = request.Email.Trim();
            var normalizedEmail = email.ToUpperInvariant();

            if (await _context.Students.AnyAsync(s => s.NationalID == studentCode))
            {
                throw new InvalidOperationException($"A student with code '{studentCode}' already exists. Use a different student code.");
            }

            if (await _context.Users.AnyAsync(u => u.NormalizedEmail == normalizedEmail))
            {
                throw new InvalidOperationException("An account with this email address already exists. Use a different email address.");
            }

            var academicYears = _context.AcademicYears.Where(a => a.Stage == stage);
            if (!string.IsNullOrWhiteSpace(request.AcademicYearName))
            {
                academicYears = academicYears.Where(a => a.YearName == request.AcademicYearName.Trim());
            }
            else
            {
                academicYears = academicYears.Where(a => a.IsActive);
            }

            var academicYear = await academicYears
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

            Class? cls = null;
            if (request.ClassId.HasValue && request.ClassId.Value > 0)
            {
                cls = await _context.Classes.FirstOrDefaultAsync(c =>
                    c.IsActive &&
                    c.ClassID == request.ClassId.Value &&
                    c.DepartmentID == dept.DepartmentID &&
                    c.AcademicYearID == academicYear.AcademicYearID);
                if (cls == null)
                {
                    return null;
                }
            }

            var username = (request.FirstName + "." + request.LastName).Replace(" ", "").ToLowerInvariant() + "-" + new Random().Next(100, 999);

            // Create app user.
            var user = new ApplicationUser
            {
                UserName = username,
                Email = email,
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
                var errors = string.Join(" ", created.Errors.Select(error => error.Description));
                throw new InvalidOperationException(string.IsNullOrWhiteSpace(errors)
                    ? "Unable to create the student account."
                    : errors);
            }

            var student = new Student
            {
                UserID = user.UserId,
                NationalID = studentCode,
                EnrollmentDate = DateTime.UtcNow.Date,
                CurrentAcademicYearID = academicYear.AcademicYearID,
                MajorID = null,
                DepartmentID = dept.DepartmentID,
                ClassID = cls?.ClassID,
                Status = "Active",
                Gender = Gender.Male
            };

            _context.Students.Add(student);
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                // UserManager has already persisted the Identity user at this point. Detach
                // the failed student entity before removing that account so a failed student
                // insert never leaves a login that has no matching Student record.
                _context.Entry(student).State = EntityState.Detached;
                await _userManager.DeleteAsync(user);
                throw new InvalidOperationException(
                    "The student could not be saved. No student account was created. Please verify the selected academic year and try again.");
            }

            return new ViceStudentDto
            {
                Id = student.StudentID.ToString(),
                ClassId = cls?.ClassID ?? 0,
                StudentCode = student.NationalID ?? string.Empty,
                Name = user.FullName ?? string.Empty,
                FirstName = user.FirstName,
                MiddleName = user.MiddleName ?? string.Empty,
                LastName = user.LastName,
                Email = user.Email ?? string.Empty,
                Phone = user.PhoneNumber ?? string.Empty,
                Department = dept.DepartmentName,
                ClassName = cls?.ClassName ?? string.Empty,
                Year = request.Year.ToLowerInvariant(),
                AcademicYearName = academicYear.YearName
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

            var studentCode = request.StudentCode.Trim();
            var email = request.Email.Trim();
            var normalizedEmail = email.ToUpperInvariant();

            if (await _context.Students.AnyAsync(s => s.StudentID != studentPk && s.NationalID == studentCode))
            {
                throw new InvalidOperationException($"A student with code '{studentCode}' already exists. Use a different student code.");
            }

            if (await _context.Users.AnyAsync(u => u.UserId != user.UserId && u.NormalizedEmail == normalizedEmail))
            {
                throw new InvalidOperationException("An account with this email address already exists. Use a different email address.");
            }

            user.FirstName = request.FirstName;
            user.MiddleName = request.MiddleName;
            user.LastName = request.LastName;
            user.FullName = $"{request.FirstName} {(string.IsNullOrWhiteSpace(request.MiddleName) ? "" : request.MiddleName + " ")}{request.LastName}";
            user.Email = email;
            user.PhoneNumber = request.Phone;

            var userUpdated = await _userManager.UpdateAsync(user);
            if (!userUpdated.Succeeded)
            {
                var errors = string.Join(" ", userUpdated.Errors.Select(error => error.Description));
                throw new InvalidOperationException(string.IsNullOrWhiteSpace(errors)
                    ? "Unable to update the student account."
                    : errors);
            }

            // Update student code/year/class using the selected academic year when
            // one was supplied by the Student Affairs dashboard.
            student.NationalID = studentCode;
            AcademicYear? selectedAcademicYear = null;
            if (Enum.TryParse<EducationStage>(request.Year, true, out var stage))
            {
                var academicYears = _context.AcademicYears.Where(a => a.Stage == stage);
                if (!string.IsNullOrWhiteSpace(request.AcademicYearName))
                {
                    academicYears = academicYears.Where(a => a.YearName == request.AcademicYearName.Trim());
                }
                else
                {
                    academicYears = academicYears.Where(a => a.IsActive);
                }

                selectedAcademicYear = await academicYears
                    .OrderByDescending(a => a.AcademicYearID)
                    .FirstOrDefaultAsync();

                if (selectedAcademicYear != null)
                {
                    student.CurrentAcademicYearID = selectedAcademicYear.AcademicYearID;
                }
            }

            var dept = await _context.Departments.FirstOrDefaultAsync(d => d.IsActive && d.DepartmentName == request.Department);
            if (dept == null)
            {
                throw new ArgumentException("The selected department was not found.");
            }

            student.DepartmentID = dept.DepartmentID;
            if (request.ClassId.HasValue)
            {
                var cls = await _context.Classes.FirstOrDefaultAsync(c =>
                    c.IsActive && c.ClassID == request.ClassId && c.DepartmentID == dept.DepartmentID &&
                    c.AcademicYearID == student.CurrentAcademicYearID);
                if (cls == null)
                {
                    throw new ArgumentException("The selected class does not belong to the selected academic year and department.");
                }

                student.ClassID = cls.ClassID;
            }
            else
            {
                student.ClassID = null;
            }

            await _context.SaveChangesAsync();

            return new ViceStudentDto
            {
                Id = student.StudentID.ToString(),
                ClassId = student.ClassID ?? 0,
                StudentCode = student.NationalID ?? string.Empty,
                Name = user.FullName ?? string.Empty,
                FirstName = user.FirstName,
                MiddleName = user.MiddleName ?? string.Empty,
                LastName = user.LastName,
                Email = user.Email ?? string.Empty,
                Phone = user.PhoneNumber ?? string.Empty,
                Department = request.Department,
                ClassName = (await _context.Classes.FirstOrDefaultAsync(c => c.ClassID == student.ClassID))?.ClassName ?? string.Empty,
                Year = request.Year.ToLowerInvariant(),
                AcademicYearName = selectedAcademicYear?.YearName ?? string.Empty
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

        public async Task<ViceStudentDto?> AssignStudentToClassAsync(string studentId, int? classId)
        {
            if (!int.TryParse(studentId, out var studentPk))
            {
                return null;
            }

            var student = await _context.Students.FirstOrDefaultAsync(item => item.StudentID == studentPk && item.UserID.HasValue);
            if (student == null)
            {
                return null;
            }

            GradeManagementSystem.Core.Entities.Domain.Class? assignedClass = null;
            if (classId.HasValue)
            {
                assignedClass = await _context.Classes.FirstOrDefaultAsync(item => item.IsActive && item.ClassID == classId.Value);
                if (assignedClass == null || assignedClass.AcademicYearID != student.CurrentAcademicYearID)
                {
                    return null;
                }
                student.ClassID = assignedClass.ClassID;
                student.DepartmentID = assignedClass.DepartmentID;
            }
            else
            {
                student.ClassID = null;
            }

            await _context.SaveChangesAsync();

            var user = await _context.Users.FirstOrDefaultAsync(item => item.UserId == student.UserID!.Value);
            var department = assignedClass?.DepartmentID is int departmentId
                ? await _context.Departments.FirstOrDefaultAsync(item => item.DepartmentID == departmentId)
                : null;
            var academicYear = await _context.AcademicYears.FirstOrDefaultAsync(item => item.AcademicYearID == student.CurrentAcademicYearID);

            return new ViceStudentDto
            {
                Id = student.StudentID.ToString(),
                ClassId = student.ClassID ?? 0,
                StudentCode = student.NationalID ?? string.Empty,
                Name = user?.FullName ?? string.Empty,
                Department = department?.DepartmentName ?? string.Empty,
                ClassName = assignedClass?.ClassName ?? string.Empty,
                Year = academicYear?.Stage.ToString().ToLowerInvariant() ?? string.Empty
            };
        }

        public async Task<int> PromoteStudentsAsync(VicePromoteStudentsRequestDTO request, int? requestedBy)
        {
            if (request == null || request.StudentIds.Count == 0 ||
                !Enum.TryParse<EducationStage>(request.SourceLevel, true, out var sourceStage) ||
                !Enum.TryParse<EducationStage>(request.TargetLevel, true, out var targetStage) ||
                sourceStage == targetStage)
            {
                throw new ArgumentException("A non-empty student list and different valid source and target levels are required.");
            }

            var studentIds = request.StudentIds
                .Select(value => int.TryParse(value, out var id) ? id : 0)
                .Where(id => id > 0)
                .Distinct()
                .ToList();
            if (studentIds.Count != request.StudentIds.Count)
            {
                throw new ArgumentException("Every student ID must be a valid numeric ID.");
            }

            var sourceYear = await _context.AcademicYears
                .Where(year => year.IsActive && year.Stage == sourceStage)
                .OrderByDescending(year => year.AcademicYearID)
                .FirstOrDefaultAsync();
            var targetYear = await _context.AcademicYears
                .Where(year => year.IsActive && year.Stage == targetStage)
                .OrderByDescending(year => year.AcademicYearID)
                .FirstOrDefaultAsync();
            var department = await _context.Departments
                .FirstOrDefaultAsync(item => item.IsActive && item.DepartmentName == request.Department.Trim());
            if (sourceYear == null || targetYear == null || department == null)
            {
                throw new InvalidOperationException("The selected academic years or department could not be found.");
            }

            var students = await _context.Students
                .Include(student => student.Class)
                .Where(student => studentIds.Contains(student.StudentID) &&
                    student.CurrentAcademicYearID == sourceYear.AcademicYearID &&
                    student.ClassID.HasValue && student.Class!.DepartmentID == department.DepartmentID)
                .ToListAsync();
            if (students.Count != studentIds.Count)
            {
                throw new InvalidOperationException("One or more students are not in the selected source level and department.");
            }

            await using var transaction = await _context.Database.BeginTransactionAsync();
            foreach (var student in students)
            {
                student.CurrentAcademicYearID = targetYear.AcademicYearID;
                student.ClassID = null;
                _context.StudentPromotions.Add(new StudentPromotion
                {
                    StudentID = student.StudentID,
                    FromAcademicYearID = sourceYear.AcademicYearID,
                    ToAcademicYearID = targetYear.AcademicYearID,
                    RequestDate = DateTime.UtcNow,
                    IsApproved = true,
                    RequestedBy = requestedBy,
                    ApprovedBy = requestedBy,
                    ApprovalDate = DateTime.UtcNow
                });
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
            return students.Count;
        }
    }
}
