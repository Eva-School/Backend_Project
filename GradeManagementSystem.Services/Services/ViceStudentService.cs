using ExcelDataReader;
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
using System.Data;
using System.IO;
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
                        Address = x.Student.Address ?? string.Empty,
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
                        Address = x.Student.Address ?? string.Empty,
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
                    Address = x.Student.Address ?? string.Empty,
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
                Gender = Gender.Male,
                Address = request.Address
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
                Address = student.Address ?? string.Empty,
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
            student.Address = request.Address;
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
                Address = student.Address ?? string.Empty,
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
                Address = student.Address ?? string.Empty,
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

        public async Task<ViceBulkImportStudentsResponseDTO> ImportStudentsFromExcelAsync(Stream stream, string fileName, string defaultYear, string defaultDepartment, string? defaultAcademicYearName = null)
        {
            var response = new ViceBulkImportStudentsResponseDTO();

            if (stream == null || stream.Length == 0)
            {
                response.Errors.Add("File is empty or not provided.");
                return response;
            }

            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

            using var reader = fileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase)
                ? ExcelReaderFactory.CreateCsvReader(stream)
                : ExcelReaderFactory.CreateReader(stream);

            var dataset = reader.AsDataSet();
            if (dataset.Tables.Count == 0)
            {
                response.Errors.Add("No worksheets or data tables found in the file.");
                return response;
            }

            var table = dataset.Tables[0];
            var role = await _roleManager.Roles.FirstOrDefaultAsync(r => r.RoleName == "Student");
            if (role == null)
            {
                response.Errors.Add("Student role not found in the database.");
                return response;
            }

            var departments = await _context.Departments.Where(d => d.IsActive).ToListAsync();
            var academicYears = await _context.AcademicYears.ToListAsync();
            var classes = await _context.Classes.Where(c => c.IsActive).ToListAsync();

            var defaultDeptObj = departments.FirstOrDefault(d => d.DepartmentName.Equals(defaultDepartment.Trim(), StringComparison.OrdinalIgnoreCase));

            string GetVal(DataRow r, int idx)
            {
                if (idx >= 0 && idx < r.ItemArray.Length)
                {
                    return r[idx]?.ToString()?.Trim() ?? string.Empty;
                }
                return string.Empty;
            }

            bool IsYes(string val)
            {
                if (string.IsNullOrWhiteSpace(val)) return false;
                val = val.Trim().ToLowerInvariant();
                return val == "yes" || val == "نعم" || val == "true" || val == "1" || val == "y";
            }

            int rowIndex = 0;
            foreach (DataRow row in table.Rows)
            {
                rowIndex++;

                string col0 = GetVal(row, 0);
                string studentCode = GetVal(row, 1);
                string nationalId = GetVal(row, 2);
                string nameAr = GetVal(row, 3);
                string nameEn = GetVal(row, 4);
                string genderStr = GetVal(row, 5);
                string nationality = GetVal(row, 6);
                string dobStr = GetVal(row, 7);
                string pob = GetVal(row, 8);
                string addressEn = GetVal(row, 9);
                string addressAr = GetVal(row, 10);
                string emailStr = GetVal(row, 11);
                string governorate = GetVal(row, 12);
                string fatherName = GetVal(row, 13);
                string motherName = GetVal(row, 14);
                string relativeName = GetVal(row, 15);
                string fatherPhone = GetVal(row, 16);
                string motherPhone = GetVal(row, 17);
                string studentPhone = GetVal(row, 18);
                string relativePhone = GetVal(row, 19);
                string religion = GetVal(row, 20);
                string fatherProfession = GetVal(row, 21);
                string motherProfession = GetVal(row, 22);
                string healthProblems = GetVal(row, 23);
                string missingDocs = GetVal(row, 24);
                string docsDeliveredStr = GetVal(row, 25);
                string prepGradeStr = GetVal(row, 26);
                string feesPaidStr = GetVal(row, 27);
                string classNameStr = GetVal(row, 28);
                string schoolYearStr = GetVal(row, 29);
                string statusStr = GetVal(row, 30);

                if (string.IsNullOrWhiteSpace(studentCode) && string.IsNullOrWhiteSpace(nationalId) && string.IsNullOrWhiteSpace(nameAr) && string.IsNullOrWhiteSpace(nameEn))
                {
                    continue;
                }

                if (studentCode.Equals("COOD", StringComparison.OrdinalIgnoreCase) || studentCode.Contains("كود") ||
                    nationalId.Equals("ID", StringComparison.OrdinalIgnoreCase) || nationalId.Contains("الرقم") ||
                    nameAr.Contains("اسم الطالب") || nameEn.Contains("Student Name"))
                {
                    continue;
                }

                response.TotalRows++;

                try
                {
                    var code = !string.IsNullOrWhiteSpace(studentCode) ? studentCode : (!string.IsNullOrWhiteSpace(nationalId) ? nationalId : Guid.NewGuid().ToString("N")[..8]);
                    var natId = !string.IsNullOrWhiteSpace(nationalId) ? nationalId : code;

                    var fullName = !string.IsNullOrWhiteSpace(nameAr) ? nameAr : nameEn;
                    if (string.IsNullOrWhiteSpace(fullName))
                    {
                        fullName = $"Student {code}";
                    }

                    var nameParts = fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    var firstName = nameParts.Length > 0 ? nameParts[0] : "Student";
                    var lastName = nameParts.Length > 1 ? nameParts[^1] : code;
                    var middleName = nameParts.Length > 2 ? string.Join(" ", nameParts[1..^1]) : string.Empty;

                    var stage = EducationStage.Junior;
                    if (!string.IsNullOrWhiteSpace(schoolYearStr))
                    {
                        if (schoolYearStr.Contains("الأول") || schoolYearStr.Contains("1") || schoolYearStr.Equals("junior", StringComparison.OrdinalIgnoreCase))
                            stage = EducationStage.Junior;
                        else if (schoolYearStr.Contains("الثاني") || schoolYearStr.Contains("2") || schoolYearStr.Equals("wheeler", StringComparison.OrdinalIgnoreCase))
                            stage = EducationStage.Wheeler;
                        else if (schoolYearStr.Contains("الثالث") || schoolYearStr.Contains("3") || schoolYearStr.Equals("senior", StringComparison.OrdinalIgnoreCase))
                            stage = EducationStage.Senior;
                    }
                    else if (Enum.TryParse<EducationStage>(defaultYear, true, out var parsedDefaultStage))
                    {
                        stage = parsedDefaultStage;
                    }

                    AcademicYear? ay = null;
                    if (!string.IsNullOrWhiteSpace(defaultAcademicYearName))
                    {
                        ay = academicYears.FirstOrDefault(a => a.Stage == stage && a.YearName.Equals(defaultAcademicYearName.Trim(), StringComparison.OrdinalIgnoreCase));
                    }
                    if (ay == null)
                    {
                        ay = academicYears.Where(a => a.Stage == stage && a.IsActive).OrderByDescending(a => a.AcademicYearID).FirstOrDefault()
                             ?? academicYears.Where(a => a.Stage == stage).OrderByDescending(a => a.AcademicYearID).FirstOrDefault();
                    }

                    if (ay == null)
                    {
                        response.FailureCount++;
                        response.Errors.Add($"Row {rowIndex}: Academic year for stage {stage} not found.");
                        continue;
                    }

                    var dept = defaultDeptObj;

                    Class? cls = null;
                    if (!string.IsNullOrWhiteSpace(classNameStr) && dept != null)
                    {
                        cls = classes.FirstOrDefault(c => c.AcademicYearID == ay.AcademicYearID && c.DepartmentID == dept.DepartmentID && c.ClassName.Equals(classNameStr.Trim(), StringComparison.OrdinalIgnoreCase));
                    }

                    if (await _context.Students.AnyAsync(s => s.NationalID == code || s.StudentCode == code))
                    {
                        response.FailureCount++;
                        response.Errors.Add($"Row {rowIndex}: Student code/ID '{code}' already exists in database.");
                        continue;
                    }

                    var email = !string.IsNullOrWhiteSpace(emailStr) ? emailStr.Trim() : $"{code}@school.edu.eg";
                    var normalizedEmail = email.ToUpperInvariant();

                    if (await _context.Users.AnyAsync(u => u.NormalizedEmail == normalizedEmail))
                    {
                        email = $"{code}.{new Random().Next(100, 999)}@school.edu.eg";
                    }

                    var username = $"{firstName}.{lastName}".Replace(" ", "").ToLowerInvariant() + "-" + new Random().Next(100, 999);

                    var gender = Gender.Male;
                    if (!string.IsNullOrWhiteSpace(genderStr))
                    {
                        if (genderStr.Contains("أنثى") || genderStr.Equals("female", StringComparison.OrdinalIgnoreCase) || genderStr.Equals("f", StringComparison.OrdinalIgnoreCase))
                        {
                            gender = Gender.Female;
                        }
                    }

                    DateTime? dob = null;
                    if (DateTime.TryParse(dobStr, out var parsedDob))
                    {
                        dob = parsedDob;
                    }

                    decimal? prepGrade = null;
                    if (decimal.TryParse(prepGradeStr, out var parsedGrade))
                    {
                        prepGrade = parsedGrade;
                    }

                    var user = new ApplicationUser
                    {
                        UserName = username,
                        Email = email,
                        FirstName = firstName,
                        MiddleName = middleName,
                        LastName = lastName,
                        FullName = fullName,
                        PhoneNumber = !string.IsNullOrWhiteSpace(studentPhone) ? studentPhone : fatherPhone,
                        RoleId = role.RoleId,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow,
                        EmailConfirmed = true
                    };

                    var createdUser = await _userManager.CreateAsync(user, "Student@123");
                    if (!createdUser.Succeeded)
                    {
                        response.FailureCount++;
                        var errStr = string.Join("; ", createdUser.Errors.Select(e => e.Description));
                        response.Errors.Add($"Row {rowIndex}: Failed to create user account ({errStr}).");
                        continue;
                    }

                    var student = new Student
                    {
                        UserID = user.UserId,
                        NationalID = natId,
                        StudentCode = code,
                        EnrollmentDate = DateTime.UtcNow.Date,
                        CurrentAcademicYearID = ay.AcademicYearID,
                        DepartmentID = dept?.DepartmentID,
                        ClassID = cls?.ClassID,
                        Status = !string.IsNullOrWhiteSpace(statusStr) ? statusStr : "Active",
                        Gender = gender,
                        Address = !string.IsNullOrWhiteSpace(addressEn) ? addressEn : addressAr,
                        NameArabic = nameAr,
                        NameEnglish = nameEn,
                        Nationality = nationality,
                        DateOfBirth = dob,
                        PlaceOfBirth = pob,
                        AddressArabic = addressAr,
                        Email = email,
                        Governorate = governorate,
                        FatherName = fatherName,
                        FatherPhone = fatherPhone,
                        FatherProfession = fatherProfession,
                        MotherName = motherName,
                        MotherPhone = motherPhone,
                        MotherProfession = motherProfession,
                        RelativeName = relativeName,
                        RelativePhone = relativePhone,
                        Religion = religion,
                        StudentPhone = studentPhone,
                        HealthProblems = healthProblems,
                        MissingDocumentation = missingDocs,
                        DocumentsDelivered = IsYes(docsDeliveredStr),
                        PreparatoryGrade = prepGrade,
                        FeesPaid = IsYes(feesPaidStr)
                    };

                    _context.Students.Add(student);
                    await _context.SaveChangesAsync();

                    response.SuccessCount++;
                    response.ImportedStudents.Add(new ViceStudentDto
                    {
                        Id = student.StudentID.ToString(),
                        ClassId = cls?.ClassID ?? 0,
                        StudentCode = code,
                        Name = fullName,
                        FirstName = firstName,
                        MiddleName = middleName ?? string.Empty,
                        LastName = lastName,
                        Email = email,
                        Phone = studentPhone,
                        Address = student.Address ?? string.Empty,
                        Department = dept?.DepartmentName ?? defaultDepartment,
                        ClassName = cls?.ClassName ?? string.Empty,
                        Year = stage.ToString().ToLowerInvariant(),
                        AcademicYearName = ay.YearName
                    });
                }
                catch (Exception ex)
                {
                    response.FailureCount++;
                    response.Errors.Add($"Row {rowIndex}: Unexpected error ({ex.Message}).");
                }
            }

            return response;
        }
    }
}
