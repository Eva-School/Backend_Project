using GradeManagementSystem.Core.DTOs.Class;
using GradeManagementSystem.Core.Entities.Domain;
using GradeManagementSystem.Core.Entities.Enums;
using GradeManagementSystem.Core.Entities.Identity;
using GradeManagementSystem.Repository.Data;
using GradeManagementSystem.Services.Services;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace GradeManagementSystem.Tests
{
    public class ClassManagementTests
    {
        private GradeDbContext CreateInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<GradeDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            return new GradeDbContext(options);
        }

        [Fact]
        public async Task GetCohortsSummaryAsync_ReturnsThreeCohortsWithAccurateStats()
        {
            await using var context = CreateInMemoryDbContext();

            // Seed Academic Years for the 3 cohorts
            var juniorYear = new AcademicYear { AcademicYearID = 1, YearName = "2025-2026", Stage = EducationStage.Junior, IsActive = true };
            var wheelerYear = new AcademicYear { AcademicYearID = 2, YearName = "2025-2026", Stage = EducationStage.Wheeler, IsActive = true };
            var seniorYear = new AcademicYear { AcademicYearID = 3, YearName = "2025-2026", Stage = EducationStage.Senior, IsActive = true };
            context.AcademicYears.AddRange(juniorYear, wheelerYear, seniorYear);

            var dept = new Department { DepartmentID = 1, DepartmentName = "OM", Description = "Operations", IsActive = true };
            context.Departments.Add(dept);

            // Add classes
            var classJunior1 = new Class { ClassID = 10, ClassName = "J-1", AcademicYearID = 1, DepartmentID = 1, Capacity = 30, IsActive = true };
            var classJunior2 = new Class { ClassID = 11, ClassName = "J-2", AcademicYearID = 1, DepartmentID = 1, Capacity = 35, IsActive = true };
            var classSenior1 = new Class { ClassID = 12, ClassName = "S-1", AcademicYearID = 3, DepartmentID = 1, Capacity = 25, IsActive = true };
            context.Classes.AddRange(classJunior1, classJunior2, classSenior1);

            // Add students
            var user1 = new ApplicationUser { UserId = 1, UserName = "stu1", FirstName = "Student", LastName = "One", FullName = "Student One" };
            var user2 = new ApplicationUser { UserId = 2, UserName = "stu2", FirstName = "Student", LastName = "Two", FullName = "Student Two" };
            context.Users.AddRange(user1, user2);

            var student1 = new Student { StudentID = 100, UserID = 1, ClassID = 10, CurrentAcademicYearID = 1, Gender = Gender.Male, NationalID = "1111", Status = "Active", EnrollmentDate = DateTime.UtcNow };
            var student2 = new Student { StudentID = 101, UserID = 2, ClassID = 10, CurrentAcademicYearID = 1, Gender = Gender.Female, NationalID = "2222", Status = "Active", EnrollmentDate = DateTime.UtcNow };
            context.Students.AddRange(student1, student2);

            await context.SaveChangesAsync();

            var service = new ClassService(context);
            var summaries = (await service.GetCohortsSummaryAsync()).ToList();

            Assert.Equal(3, summaries.Count);

            var junior = summaries.Single(s => s.Stage == "junior");
            Assert.Equal("Junior", junior.StageName);
            Assert.Equal(2, junior.ClassCount);
            Assert.Equal(65, junior.TotalCapacity);
            Assert.Equal(2, junior.TotalStudents);

            var wheeler = summaries.Single(s => s.Stage == "wheeler");
            Assert.Equal(0, wheeler.ClassCount);
            Assert.Equal(0, wheeler.TotalStudents);

            var senior = summaries.Single(s => s.Stage == "senior");
            Assert.Equal(1, senior.ClassCount);
            Assert.Equal(25, senior.TotalCapacity);
        }

        [Fact]
        public async Task GetClassesByYearIdAsync_WithStage_ReturnsClassesWithCapacityAndStudentCount()
        {
            await using var context = CreateInMemoryDbContext();

            var year = new AcademicYear { AcademicYearID = 1, YearName = "2025-2026", Stage = EducationStage.Junior, IsActive = true };
            var dept = new Department { DepartmentID = 1, DepartmentName = "OM", Description = "Operations", IsActive = true };
            context.AcademicYears.Add(year);
            context.Departments.Add(dept);

            var cls = new Class { ClassID = 1, ClassName = "Class A", AcademicYearID = 1, DepartmentID = 1, Capacity = 28, IsActive = true };
            context.Classes.Add(cls);

            var user = new ApplicationUser { UserId = 1, UserName = "stu1", FirstName = "Student", LastName = "One", FullName = "Student One" };
            context.Users.Add(user);
            var student = new Student { StudentID = 1, UserID = 1, ClassID = 1, CurrentAcademicYearID = 1, Gender = Gender.Male, NationalID = "123", Status = "Active", EnrollmentDate = DateTime.UtcNow };
            context.Students.Add(student);

            await context.SaveChangesAsync();

            var service = new ClassService(context);
            var classes = (await service.GetClassesByYearIdAsync(null, "junior")).ToList();

            Assert.Single(classes);
            var first = classes[0];
            Assert.Equal("Class A", first.ClassName);
            Assert.Equal("OM", first.DepartmentName);
            Assert.Equal(28, first.Capacity);
            Assert.Equal(1, first.StudentCount);
            Assert.True(first.IsActive);
        }

        [Fact]
        public async Task GetClassDetailsAsync_ReturnsClassDetailsWithEnrolledStudentsAndTeachers()
        {
            await using var context = CreateInMemoryDbContext();

            var year = new AcademicYear { AcademicYearID = 1, YearName = "2025-2026", Stage = EducationStage.Senior, IsActive = true };
            var dept = new Department { DepartmentID = 2, DepartmentName = "SD", Description = "Software", IsActive = true };
            var subject = new Subject { SubjectID = 10, SubjectName = "Programming", AcademicYearID = 1, IsActive = true };
            context.AcademicYears.Add(year);
            context.Departments.Add(dept);
            context.Subjects.Add(subject);

            var cls = new Class { ClassID = 5, ClassName = "Senior-1", AcademicYearID = 1, DepartmentID = 2, Capacity = 30, IsActive = true };
            context.Classes.Add(cls);

            var teacherUser = new ApplicationUser { UserId = 50, UserName = "prof.ahmed", FirstName = "Ahmed", LastName = "Teacher", FullName = "Ahmed Teacher" };
            context.Users.Add(teacherUser);
            var teacher = new Teacher { TeacherID = 20, UserID = 50, EmployeeCode = "EMP-9999", Qualifications = "IT", IsActive = true };
            context.Teachers.Add(teacher);

            var assignment = new TeacherAssignment { TeacherAssignmentID = 1, TeacherID = 20, ClassID = 5, SubjectID = 10, AcademicYearID = 1, IsActive = true, AssignedAt = DateTime.UtcNow };
            context.TeacherAssignments.Add(assignment);

            var studentUser = new ApplicationUser { UserId = 60, UserName = "stu.mona", FirstName = "Mona", LastName = "Ali", FullName = "Mona Ali", Email = "mona@school.com", PhoneNumber = "0100000000" };
            context.Users.Add(studentUser);
            var student = new Student { StudentID = 70, UserID = 60, ClassID = 5, CurrentAcademicYearID = 1, NationalID = "30001011234567", StudentCode = "STU-0070", Gender = Gender.Female, Status = "Active", EnrollmentDate = DateTime.UtcNow };
            context.Students.Add(student);

            await context.SaveChangesAsync();

            var service = new ClassService(context);
            var details = await service.GetClassDetailsAsync(5);

            Assert.NotNull(details);
            Assert.Equal("Senior-1", details.ClassName);
            Assert.Equal("SD", details.DepartmentName);
            Assert.Equal(30, details.Capacity);
            Assert.Equal(1, details.StudentCount);
            Assert.Single(details.Teachers);
            Assert.Equal("Ahmed Teacher", details.Teachers[0].TeacherName);
            Assert.Equal("Programming", details.Teachers[0].SubjectName);
            Assert.Single(details.Students);
            Assert.Equal("Mona Ali", details.Students[0].FullName);
            Assert.Equal("STU-0070", details.Students[0].StudentCode);
            Assert.Equal("Female", details.Students[0].Gender);
        }

        [Fact]
        public async Task CreateClassAsync_And_UpdateClassAsync_PerformAccurately()
        {
            await using var context = CreateInMemoryDbContext();

            var year = new AcademicYear { AcademicYearID = 1, YearName = "2025-2026", Stage = EducationStage.Junior, IsActive = true };
            var deptOM = new Department { DepartmentID = 1, DepartmentName = "OM", Description = "Operations", IsActive = true };
            var deptSD = new Department { DepartmentID = 2, DepartmentName = "SD", Description = "Software", IsActive = true };
            context.AcademicYears.Add(year);
            context.Departments.AddRange(deptOM, deptSD);
            await context.SaveChangesAsync();

            var service = new ClassService(context);

            // Create Class
            var createRequest = new CreateClassRequestDTO
            {
                YearId = "2025-2026",
                Stage = "junior",
                Department = "OM",
                ClassName = "Cohort1-A",
                Capacity = 25
            };
            var created = await service.CreateClassAsync(createRequest);

            Assert.NotNull(created);
            Assert.Equal("Cohort1-A", created.ClassName);
            Assert.Equal("OM", created.DepartmentName);
            Assert.Equal(25, created.Capacity);

            // Update Class
            var updateRequest = new UpdateClassRequestDTO
            {
                ClassName = "Cohort1-A-Renamed",
                Department = "SD",
                Capacity = 32,
                IsActive = true
            };
            var updated = await service.UpdateClassAsync(created.ClassId, updateRequest);

            Assert.NotNull(updated);
            Assert.Equal("Cohort1-A-Renamed", updated.ClassName);
            Assert.Equal("SD", updated.DepartmentName);
            Assert.Equal(32, updated.Capacity);
        }

        [Fact]
        public async Task DeleteClassAsync_SafelyUnassignsStudentsAndDeletesRecord()
        {
            await using var context = CreateInMemoryDbContext();

            var year = new AcademicYear { AcademicYearID = 1, YearName = "2025-2026", Stage = EducationStage.Wheeler, IsActive = true };
            var dept = new Department { DepartmentID = 1, DepartmentName = "OM", Description = "Operations", IsActive = true };
            var cls = new Class { ClassID = 9, ClassName = "Class To Delete", AcademicYearID = 1, DepartmentID = 1, Capacity = 20, IsActive = true };
            context.AcademicYears.Add(year);
            context.Departments.Add(dept);
            context.Classes.Add(cls);

            var student = new Student { StudentID = 10, ClassID = 9, CurrentAcademicYearID = 1, Gender = Gender.Male, NationalID = "4444", Status = "Active", EnrollmentDate = DateTime.UtcNow };
            context.Students.Add(student);
            await context.SaveChangesAsync();

            var service = new ClassService(context);
            var (success, message) = await service.DeleteClassAsync(9);

            Assert.True(success);
            Assert.Equal("Class deleted successfully.", message);

            // Verify class removed
            var classInDb = await context.Classes.FirstOrDefaultAsync(c => c.ClassID == 9);
            Assert.Null(classInDb);

            // Verify student was NOT deleted, but unassigned
            var studentInDb = await context.Students.FirstOrDefaultAsync(s => s.StudentID == 10);
            Assert.NotNull(studentInDb);
            Assert.Null(studentInDb.ClassID);
        }
    }
}
