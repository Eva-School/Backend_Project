using GradeManagementSystem.Core.DTOs.Student;
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
    public class StudentDashboardTests
    {
        private GradeDbContext CreateInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<GradeDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            return new GradeDbContext(options);
        }

        [Fact]
        public async Task GetProfileAsync_ReturnsCompleteProfileWithMetrics()
        {
            await using var context = CreateInMemoryDbContext();

            var year = new AcademicYear
            {
                AcademicYearID = 1,
                YearName = "2025-2026",
                Stage = EducationStage.Junior,
                IsActive = true
            };
            var dept = new Department { DepartmentID = 1, DepartmentName = "Computer Science", Description = "CS Dept", IsActive = true };
            var major = new Major { MajorID = 1, MajorName = "Software Engineering", DepartmentID = 1, Description = "SE Major" };
            var cls = new Class { ClassID = 1, ClassName = "CS-101", Capacity = 30, AcademicYearID = 1 };

            var user = new ApplicationUser
            {
                UserId = 10,
                UserName = "student1@system.com",
                FirstName = "Omar",
                LastName = "Khaled",
                FullName = "Omar Khaled",
                Email = "student1@system.com",
                PhoneNumber = "01000000001"
            };

            var student = new Student
            {
                StudentID = 1,
                UserID = 10,
                NationalID = "30101010000000",
                StudentCode = "STD-00001",
                NameEnglish = "Omar Khaled",
                NameArabic = "عمر خالد",
                Gender = Gender.Male,
                CurrentAcademicYearID = 1,
                DepartmentID = 1,
                MajorID = 1,
                ClassID = 1,
                Status = "Active",
                Address = "Cairo, Egypt",
                AddressArabic = "القاهرة، مصر",
                EnrollmentDate = new DateTime(2025, 9, 1)
            };

            var comp = new Competency { CompetencyID = 1, CompetencyName = "Python Basics", MaxAttempts = 3, IsActive = true };
            var compStatus = new StudentCompetencyStatus
            {
                StudentCompetencyStatusID = 1,
                StudentID = 1,
                CompetencyID = 1,
                StatusID = "Passed",
                CurrentAttemptNumber = 1,
                MaxAllowedAttempts = 3
            };

            context.AcademicYears.Add(year);
            context.Departments.Add(dept);
            context.Majors.Add(major);
            context.Classes.Add(cls);
            context.Users.Add(user);
            context.Students.Add(student);
            context.Competencies.Add(comp);
            context.StudentCompetencyStatuses.Add(compStatus);
            await context.SaveChangesAsync();

            var service = new StudentDashboardService(context);
            var profile = await service.GetProfileAsync(10);

            Assert.NotNull(profile);
            Assert.Equal("STD-00001", profile.StudentCode);
            Assert.Equal("CS-101", profile.ClassName);
            Assert.Equal("Software Engineering", profile.MajorName);
            Assert.Equal(1, profile.CompletedCompetencies);
            Assert.Equal(1, profile.TotalCompetencies);
            Assert.Equal("Year 1", profile.Year);
        }

        [Fact]
        public async Task UpdateProfileAsync_UpdatesContactInformation()
        {
            await using var context = CreateInMemoryDbContext();

            var user = new ApplicationUser
            {
                UserId = 20,
                UserName = "student2@system.com",
                FirstName = "Fatima",
                LastName = "Ali",
                FullName = "Fatima Ali"
            };

            var student = new Student
            {
                StudentID = 2,
                UserID = 20,
                NationalID = "30202020000000",
                Gender = Gender.Female,
                Address = "Old Address",
                StudentPhone = "01111111111",
                Status = "Active",
                EnrollmentDate = new DateTime(2025, 9, 1)
            };

            context.Users.Add(user);
            context.Students.Add(student);
            await context.SaveChangesAsync();

            var service = new StudentDashboardService(context);
            var request = new UpdateStudentContactDto
            {
                Phone = "01222222222",
                Address = "New Alexandria Street",
                AddressArabic = "شارع الاسكندرية الجديد",
                RelativeName = "Ali (Father)",
                RelativePhone = "01099999999"
            };

            var result = await service.UpdateProfileAsync(20, request);

            Assert.True(result);
            var updatedStudent = await context.Students.FirstAsync(s => s.StudentID == 2);
            Assert.Equal("01222222222", updatedStudent.StudentPhone);
            Assert.Equal("New Alexandria Street", updatedStudent.Address);
            Assert.Equal("شارع الاسكندرية الجديد", updatedStudent.AddressArabic);
            Assert.Equal("Ali (Father)", updatedStudent.RelativeName);
        }

        [Fact]
        public async Task GetQuarterGradesAsync_ComputesCourseworkAndReturnsQuizzes()
        {
            await using var context = CreateInMemoryDbContext();

            var year = new AcademicYear { AcademicYearID = 1, YearName = "2025-2026", Stage = EducationStage.Junior, IsActive = true };
            var student = new Student
            {
                StudentID = 3,
                UserID = 30,
                NationalID = "30303030000000",
                Gender = Gender.Male,
                Status = "Active",
                EnrollmentDate = new DateTime(2025, 9, 1)
            };
            var subject = new Subject
            {
                SubjectID = 1,
                SubjectName = "Mathematics",
                AcademicYearID = 1,
                MaxQuarterQ1Score = 25,
                MaxQuarterQ2Score = 25,
                MaxQuarterQ3Score = 25,
                MaxQuarterQ4Score = 25,
                MaxQuarterScore = 100,
                IsActive = true
            };

            var result = new StudentSubjectTermResult
            {
                ResultID = 1,
                StudentID = 3,
                SubjectID = 1,
                AcademicYearID = 1,
                TermID = 1,
                Quarter1Score = 22,
                Quarter2Score = 24,
                Quarter3Score = 20,
                Quarter4Score = 23,
                Status = SubjectStatus.Passed,
                CreatedAt = DateTime.UtcNow
            };

            var teacher = new Teacher { TeacherID = 1, EmployeeCode = "T-001", Qualifications = "B.Sc. Math", IsActive = true };
            var cls = new Class { ClassID = 1, ClassName = "Class 1", Capacity = 30 };
            var quiz = new Quiz
            {
                QuizID = 1,
                Title = "Quiz 1: Algebra",
                MaxScore = 10,
                SubjectID = 1,
                AcademicYearID = 1,
                ClassID = 1,
                CreatedByTeacherID = 1
            };
            var quizGrade = new QuizGrade
            {
                QuizGradeID = 1,
                QuizID = 1,
                StudentID = 3,
                Score = 9.5m,
                Notes = "Great performance"
            };

            context.AcademicYears.Add(year);
            context.Students.Add(student);
            context.Subjects.Add(subject);
            context.Classes.Add(cls);
            context.Teachers.Add(teacher);
            context.StudentSubjectTermResults.Add(result);
            context.Quizzes.Add(quiz);
            context.QuizGrades.Add(quizGrade);
            await context.SaveChangesAsync();

            var service = new StudentDashboardService(context);
            var response = await service.GetQuarterGradesAsync(30, "junior", 1);

            Assert.NotNull(response);
            Assert.Single(response.Grades);
            var item = response.Grades[0];
            Assert.Equal("Mathematics", item.Subject);
            Assert.Equal(89m, item.CourseworkTotal); // 22 + 24 + 20 + 23
            Assert.Equal(89m, item.Percentage);
            Assert.Single(item.Quizzes);
            Assert.Equal("Quiz 1: Algebra", item.Quizzes[0].Title);
            Assert.Equal(9.5m, item.Quizzes[0].Score);
        }

        [Fact]
        public async Task GetFinalGradesAsync_ComputesPercentageAndStanding()
        {
            await using var context = CreateInMemoryDbContext();

            var year = new AcademicYear { AcademicYearID = 1, YearName = "2025-2026", Stage = EducationStage.Junior, IsActive = true };
            var student = new Student
            {
                StudentID = 4,
                UserID = 40,
                NationalID = "30404040000000",
                Gender = Gender.Female,
                Status = "Active",
                EnrollmentDate = new DateTime(2025, 9, 1)
            };
            var subject1 = new Subject
            {
                SubjectID = 1,
                SubjectName = "Physics",
                AcademicYearID = 1,
                MaxQuarterScore = 50,
                MaxFinalScore = 50,
                IsActive = true
            };
            var termResult = new StudentSubjectTermResult
            {
                ResultID = 1,
                StudentID = 4,
                SubjectID = 1,
                AcademicYearID = 1,
                Quarter1Score = 20,
                Quarter2Score = 22,
                FinalExamScore = 45,
                TermTotal = 87,
                Status = SubjectStatus.Passed,
                CreatedAt = DateTime.UtcNow
            };

            context.AcademicYears.Add(year);
            context.Students.Add(student);
            context.Subjects.Add(subject1);
            context.StudentSubjectTermResults.Add(termResult);
            await context.SaveChangesAsync();

            var service = new StudentDashboardService(context);
            var response = await service.GetFinalGradesAsync(40, "junior");

            Assert.NotNull(response);
            Assert.Single(response.Grades);
            var grade = response.Grades[0];
            Assert.Equal("Physics", grade.Subject);
            Assert.Equal(87m, grade.TotalScore);
            Assert.Equal("A", grade.LetterGrade);
            Assert.NotNull(response.CumulativeAverage);
            Assert.True(response.CumulativeAverage > 80m);
            Assert.Equal(87m, response.TotalEarnedScore);
            Assert.Equal(100m, response.TotalMaxScore);
            Assert.Equal("Excellent (ممتاز)", response.Standing);
        }

        [Fact]
        public async Task GetFinalGradesAsync_UnreleasedGrades_ReturnsNullsAndNotReleased()
        {
            await using var context = CreateInMemoryDbContext();

            var year = new AcademicYear { AcademicYearID = 1, YearName = "2025-2026", Stage = EducationStage.Junior, IsActive = true };
            var term1 = new Term { TermID = 1, TermName = "Term 1", AcademicYearID = 1, StartDate = DateTime.UtcNow, EndDate = DateTime.UtcNow.AddMonths(4) };
            var term2 = new Term { TermID = 2, TermName = "Term 2", AcademicYearID = 1, StartDate = DateTime.UtcNow.AddMonths(4), EndDate = DateTime.UtcNow.AddMonths(8) };
            var student = new Student
            {
                StudentID = 9,
                UserID = 90,
                NationalID = "30909090000000",
                Gender = Gender.Male,
                Status = "Active",
                EnrollmentDate = new DateTime(2025, 9, 1)
            };
            var math = new Subject
            {
                SubjectID = 1,
                SubjectName = "Math",
                AcademicYearID = 1,
                MaxQuarterScore = 50,
                MaxFinalScore = 75,
                IsActive = true
            };
            var arabic = new Subject
            {
                SubjectID = 2,
                SubjectName = "Arabic",
                AcademicYearID = 1,
                MaxQuarterScore = 50,
                MaxFinalScore = 75,
                IsActive = true
            };
            // Arabic has coursework only in Term 1 (In Progress)
            var arabicT1Result = new StudentSubjectTermResult
            {
                ResultID = 1,
                StudentID = 9,
                SubjectID = 2,
                TermID = 1,
                AcademicYearID = 1,
                Quarter1Score = 5,
                TermTotal = 5,
                Status = SubjectStatus.InProgress,
                CreatedAt = DateTime.UtcNow
            };

            context.AcademicYears.Add(year);
            context.Terms.AddRange(term1, term2);
            context.Students.Add(student);
            context.Subjects.AddRange(math, arabic);
            context.StudentSubjectTermResults.Add(arabicT1Result);
            await context.SaveChangesAsync();

            var service = new StudentDashboardService(context);
            var response = await service.GetFinalGradesAsync(90, "junior");

            Assert.NotNull(response);
            Assert.Equal(4, response.Grades.Count); // Math T1, Math T2, Arabic T1, Arabic T2

            // Math T1 has no grades entered -> Not Released
            var mathT1 = response.Grades.First(g => g.Subject == "Math" && g.TermName == "Term 1");
            Assert.Null(mathT1.CourseworkScore);
            Assert.Null(mathT1.FinalExamScore);
            Assert.Null(mathT1.TotalScore);
            Assert.Null(mathT1.Percentage);
            Assert.Equal("Not Released", mathT1.Status);

            // Arabic T1 has coursework entered (5) -> In Progress
            var arabicT1 = response.Grades.First(g => g.Subject == "Arabic" && g.TermName == "Term 1");
            Assert.Equal(5m, arabicT1.CourseworkScore);
            Assert.Null(arabicT1.FinalExamScore);
            Assert.Equal(5m, arabicT1.TotalScore);
            Assert.Equal("In Progress", arabicT1.Status);

            // Overall metrics should not treat unreleased as 0
            Assert.Null(response.CumulativeAverage);
            Assert.Equal("Not Released", response.Standing);
        }

        [Fact]
        public async Task GetQuarterGradesAsync_UnreleasedGrades_ReturnsNulls()
        {
            await using var context = CreateInMemoryDbContext();

            var year = new AcademicYear { AcademicYearID = 1, YearName = "2025-2026", Stage = EducationStage.Junior, IsActive = true };
            var term1 = new Term { TermID = 1, TermName = "Term 1", AcademicYearID = 1, StartDate = DateTime.UtcNow, EndDate = DateTime.UtcNow.AddMonths(4) };
            var student = new Student
            {
                StudentID = 10,
                UserID = 100,
                NationalID = "31001000000000",
                Gender = Gender.Male,
                Status = "Active",
                EnrollmentDate = new DateTime(2025, 9, 1)
            };
            var math = new Subject
            {
                SubjectID = 1,
                SubjectName = "Math",
                AcademicYearID = 1,
                MaxQuarterScore = 50,
                IsActive = true
            };

            context.AcademicYears.Add(year);
            context.Terms.Add(term1);
            context.Students.Add(student);
            context.Subjects.Add(math);
            await context.SaveChangesAsync();

            var service = new StudentDashboardService(context);
            var response = await service.GetQuarterGradesAsync(100, "junior", 1);

            Assert.NotNull(response);
            Assert.Single(response.Grades);
            var grade = response.Grades[0];
            Assert.Null(grade.Quarter1);
            Assert.Null(grade.CourseworkTotal);
            Assert.Null(grade.Percentage);
        }

        [Fact]
        public async Task GetJadaratGradesAsync_IncludesAttemptHistoryAndStatus()
        {
            await using var context = CreateInMemoryDbContext();

            var year = new AcademicYear { AcademicYearID = 1, YearName = "2025-2026", Stage = EducationStage.Junior, IsActive = true };
            var student = new Student
            {
                StudentID = 5,
                UserID = 50,
                NationalID = "30505050000000",
                Gender = Gender.Male,
                Status = "Active",
                EnrollmentDate = new DateTime(2025, 9, 1)
            };
            var competency = new Competency
            {
                CompetencyID = 1,
                CompetencyName = "Network Cabling",
                MaxAttempts = 3,
                IsActive = true
            };

            var status = new StudentCompetencyStatus
            {
                StudentCompetencyStatusID = 1,
                StudentID = 5,
                CompetencyID = 1,
                StatusID = "Passed",
                CurrentAttemptNumber = 2,
                MaxAllowedAttempts = 3,
                LastEvaluatedAt = DateTime.UtcNow
            };

            var attempt1 = new CompetencyAttempt
            {
                AttemptID = 1,
                StudentCompetencyStatusID = 1,
                StudentID = 5,
                AttemptNumber = 1,
                Result = "Needs Improvement",
                EvaluatedAt = DateTime.UtcNow.AddDays(-7)
            };

            var attempt2 = new CompetencyAttempt
            {
                AttemptID = 2,
                StudentCompetencyStatusID = 1,
                StudentID = 5,
                AttemptNumber = 2,
                Result = "Pass",
                EvaluatedAt = DateTime.UtcNow
            };

            context.AcademicYears.Add(year);
            context.Students.Add(student);
            context.Competencies.Add(competency);
            context.StudentCompetencyStatuses.Add(status);
            context.CompetencyAttempts.AddRange(attempt1, attempt2);
            await context.SaveChangesAsync();

            var service = new StudentDashboardService(context);
            var response = await service.GetJadaratGradesAsync(50, "junior");

            Assert.NotNull(response);
            Assert.Equal(1, response.TotalCompetencies);
            Assert.Equal(1, response.PassedCompetencies);
            Assert.Equal(0, response.PendingCompetencies);
            Assert.Single(response.Grades);
            var grade = response.Grades[0];
            Assert.Equal("Network Cabling", grade.Jadarat);
            Assert.Equal("Passed", grade.CurrentStatus);
            Assert.Equal(2, grade.AttemptHistory.Count);
            Assert.Equal("Needs Improvement", grade.AttemptHistory[0].Result);
            Assert.Equal("Pass", grade.AttemptHistory[1].Result);
        }

        [Fact]
        public async Task GetEnrollmentDetailsAsync_ReturnsEnrolledClassAndTeachers()
        {
            await using var context = CreateInMemoryDbContext();

            var year = new AcademicYear { AcademicYearID = 1, YearName = "2025-2026", Stage = EducationStage.Junior, IsActive = true };
            var cls = new Class { ClassID = 1, ClassName = "Class 1A", Capacity = 30, AcademicYearID = 1 };
            var student = new Student
            {
                StudentID = 6,
                UserID = 60,
                NationalID = "30606060000000",
                Gender = Gender.Male,
                CurrentAcademicYearID = 1,
                ClassID = 1,
                Status = "Active",
                EnrollmentDate = new DateTime(2025, 9, 1)
            };

            var subject = new Subject { SubjectID = 1, SubjectName = "Algorithms", IsActive = true };
            var teacherUser = new ApplicationUser
            {
                UserId = 61,
                FirstName = "Hassan",
                LastName = "Ibrahim",
                FullName = "Hassan Ibrahim",
                Email = "hassan@system.com"
            };
            var teacher = new Teacher { TeacherID = 1, UserID = 61, EmployeeCode = "T-061", Qualifications = "Ph.D.", IsActive = true };
            var assignment = new TeacherAssignment
            {
                TeacherAssignmentID = 1,
                TeacherID = 1,
                ClassID = 1,
                SubjectID = 1,
                AcademicYearID = 1,
                IsActive = true,
                AssignedAt = DateTime.UtcNow
            };

            context.AcademicYears.Add(year);
            context.Classes.Add(cls);
            context.Students.Add(student);
            context.Subjects.Add(subject);
            context.Users.Add(teacherUser);
            context.Teachers.Add(teacher);
            context.TeacherAssignments.Add(assignment);
            await context.SaveChangesAsync();

            var service = new StudentDashboardService(context);
            var enrollment = await service.GetEnrollmentDetailsAsync(60);

            Assert.NotNull(enrollment);
            Assert.Equal("Class 1A", enrollment.ClassName);
            Assert.Equal(string.Empty, enrollment.Section);
            Assert.Single(enrollment.Teachers);
            Assert.Equal("Algorithms", enrollment.Teachers[0].SubjectName);
            Assert.Equal("Hassan Ibrahim", enrollment.Teachers[0].TeacherName);
            Assert.Equal("hassan@system.com", enrollment.Teachers[0].TeacherEmail);
        }
    }
}
