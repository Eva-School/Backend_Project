using GradeManagementSystem.Repository.Data;
using GradeManagementSystem.Core.Entities.Domain;
using GradeManagementSystem.Core.Entities.Enums;
using GradeManagementSystem.Core.Entities.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace GradeManagementSystem.Api.Data
{
    public static class TeacherDashboardSeed
    {
        public static async Task SeedAsync(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<GradeDbContext>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            await context.Database.MigrateAsync();

            var major = await EnsureMajorAsync(context);
            var teacherUser = await EnsureTeacherUserAsync(userManager);
            var teacher = await EnsureTeacherAsync(context, teacherUser.UserId);

            var seniorYear = await context.AcademicYears
                .Where(y => y.IsActive && y.Stage == EducationStage.Senior)
                .OrderByDescending(y => y.AcademicYearID)
                .FirstOrDefaultAsync();

            if (seniorYear == null)
            {
                // Fallback to any active academic year if Senior isn't configured
                seniorYear = await context.AcademicYears
                    .Where(y => y.IsActive)
                    .OrderByDescending(y => y.AcademicYearID)
                    .FirstOrDefaultAsync();
            }

            if (seniorYear == null)
            {
                return;
            }

            var targetClass = await context.Classes
                .Where(c => c.IsActive && c.AcademicYearID == seniorYear.AcademicYearID)
                .OrderBy(c => c.ClassID)
                .FirstOrDefaultAsync();

            if (targetClass == null)
            {
                return;
            }

            var mathSubject = await context.Subjects
                .Where(s => s.IsActive && s.AcademicYearID == seniorYear.AcademicYearID && s.SubjectName == "Mathematics")
                .FirstOrDefaultAsync();

            if (mathSubject == null)
            {
                mathSubject = await context.Subjects
                    .Where(s => s.IsActive && s.AcademicYearID == seniorYear.AcademicYearID)
                    .OrderBy(s => s.SubjectID)
                    .FirstOrDefaultAsync();
            }

            if (mathSubject == null)
            {
                return;
            }

            await EnsureTeacherAssignmentAsync(context, teacher.TeacherID, targetClass.ClassID, mathSubject.SubjectID, seniorYear.AcademicYearID);

            // Ensure we have at least: one passed, one failed student, in the same class.
            var studentPassed = await EnsureStudentAsync(
                context,
                userManager,
                username: "student",
                email: "student@system.com",
                firstName: "Ahmed",
                lastName: "Ali",
                fullName: "Ahmed Ali",
                nationalId: "29901011234567",
                majorId: major.MajorID,
                currentAcademicYearId: seniorYear.AcademicYearID,
                classId: targetClass.ClassID,
                finalExamScore: 85m
            );

            var studentFailed = await EnsureStudentAsync(
                context,
                userManager,
                username: "student_fail",
                email: "student_fail@system.com",
                firstName: "Omar",
                lastName: "Hassan",
                fullName: "Omar Hassan",
                nationalId: "29901011234568",
                majorId: major.MajorID,
                currentAcademicYearId: seniorYear.AcademicYearID,
                classId: targetClass.ClassID,
                finalExamScore: 30m
            );

            // Seed grade rows for students (upsert only if missing).
            await EnsureMathSubjectResultAsync(context, mathSubject.SubjectID, seniorYear.AcademicYearID, studentPassed.StudentID, studentPassed_FinalFinalExamScore: 85m);
            await EnsureMathSubjectResultAsync(context, mathSubject.SubjectID, seniorYear.AcademicYearID, studentFailed.StudentID, studentPassed_FinalFinalExamScore: 30m);
        }

        private static async Task<Major> EnsureMajorAsync(GradeDbContext context)
        {
            var major = await context.Majors.FirstOrDefaultAsync(m => m.MajorName == "General Track");
            if (major != null)
            {
                return major;
            }

            major = new Major
            {
                MajorName = "General Track",
                DepartmentID = 1,
                Description = "General academic track",
                IsActive = true
            };

            context.Majors.Add(major);
            await context.SaveChangesAsync();
            return major;
        }

        private static async Task<ApplicationUser> EnsureTeacherUserAsync(UserManager<ApplicationUser> userManager)
        {
            var existing = await userManager.Users.FirstOrDefaultAsync(u => u.UserName == "teacher");
            if (existing != null)
            {
                return existing;
            }

            var user = new ApplicationUser
            {
                UserName = "teacher",
                Email = "teacher@system.com",
                FirstName = "Ahmed",
                LastName = "Karim",
                FullName = "Ahmed Karim",
                RoleId = 3,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                EmailConfirmed = true
            };

            var result = await userManager.CreateAsync(user, "Teacher@123");
            if (!result.Succeeded)
            {
                throw new InvalidOperationException("Unable to seed teacher user: " + string.Join(", ", result.Errors.Select(e => e.Description)));
            }

            return user;
        }

        private static async Task<Teacher> EnsureTeacherAsync(GradeDbContext context, int teacherUserId)
        {
            // Prefer existing linked teacher row.
            var existing = await context.Teachers.FirstOrDefaultAsync(t => t.UserID == teacherUserId);
            if (existing != null)
            {
                return existing;
            }

            // Seed configuration already adds TeacherID=1 and TeacherID=2. Use TeacherID=1 by default.
            var teacher = await context.Teachers.FirstOrDefaultAsync(t => t.TeacherID == 1) ?? await context.Teachers.FirstOrDefaultAsync();
            if (teacher == null)
            {
                throw new InvalidOperationException("No teacher rows exist in Teachers table.");
            }

            teacher.UserID = teacherUserId;
            await context.SaveChangesAsync();

            return teacher;
        }

        private static async Task EnsureTeacherAssignmentAsync(GradeDbContext context, int teacherId, int classId, int subjectId, int academicYearId)
        {
            var exists = await context.TeacherAssignments.AnyAsync(
                ta => ta.TeacherID == teacherId && ta.ClassID == classId && ta.SubjectID == subjectId && ta.AcademicYearID == academicYearId);

            if (exists)
            {
                return;
            }

            context.TeacherAssignments.Add(new TeacherAssignment
            {
                TeacherID = teacherId,
                ClassID = classId,
                SubjectID = subjectId,
                AcademicYearID = academicYearId,
                AssignedAt = DateTime.UtcNow,
                IsActive = true
            });

            await context.SaveChangesAsync();
        }

        private static async Task<Student> EnsureStudentAsync(
            GradeDbContext context,
            UserManager<ApplicationUser> userManager,
            string username,
            string email,
            string firstName,
            string lastName,
            string fullName,
            string nationalId,
            int majorId,
            int currentAcademicYearId,
            int classId,
            decimal finalExamScore)
        {
            var existingUser = await userManager.Users.FirstOrDefaultAsync(u => u.UserName == username);
            ApplicationUser user;
            if (existingUser != null)
            {
                user = existingUser;
            }
            else
            {
                user = new ApplicationUser
                {
                    UserName = username,
                    Email = email,
                    FirstName = firstName,
                    LastName = lastName,
                    FullName = fullName,
                    RoleId = 4,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    EmailConfirmed = true
                };

                var result = await userManager.CreateAsync(user, "Student@123");
                if (!result.Succeeded)
                {
                    throw new InvalidOperationException("Unable to seed student user: " + string.Join(", ", result.Errors.Select(e => e.Description)));
                }
            }

            var existingStudent = await context.Students.FirstOrDefaultAsync(s => s.UserID == user.UserId);
            if (existingStudent != null)
            {
                // Keep existing rows as-is (teacher POST /grades should update).
                return existingStudent;
            }

            var student = new Student
            {
                UserID = user.UserId,
                NationalID = nationalId,
                EnrollmentDate = DateTime.UtcNow.Date,
                CurrentAcademicYearID = currentAcademicYearId,
                MajorID = majorId,
                ClassID = classId,
                Status = "Active",
                Gender = Gender.Male
            };

            context.Students.Add(student);
            await context.SaveChangesAsync();

            return student;
        }

        private static async Task EnsureMathSubjectResultAsync(
            GradeDbContext context,
            int subjectId,
            int academicYearId,
            int studentId,
            decimal studentPassed_FinalFinalExamScore)
        {
            var term = await context.Terms
                .Where(t => t.AcademicYearID == academicYearId)
                .OrderBy(t => t.TermID)
                .FirstOrDefaultAsync();

            if (term == null)
            {
                term = new Term
                {
                    AcademicYearID = academicYearId,
                    TermName = "Term 1",
                    StartDate = new DateTime(2025, 9, 1),
                    EndDate = new DateTime(2026, 1, 31)
                };

                context.Terms.Add(term);
                await context.SaveChangesAsync();
            }

            var exists = await context.StudentSubjectTermResults.AnyAsync(r =>
                r.StudentID == studentId &&
                r.SubjectID == subjectId &&
                r.TermID == term.TermID &&
                r.AcademicYearID == academicYearId);

            if (exists)
            {
                return;
            }

            // Match the quarter split used in StudentDashboardSeed.
            var quarter1 = 12m;
            var quarter2 = 13m;
            var termTotal = quarter1 + quarter2 + studentPassed_FinalFinalExamScore;

            // Subject max final score is typically 100 -> pass at 50%.
            var maxFinalScore = await context.Subjects
                .Where(s => s.SubjectID == subjectId)
                .Select(s => s.MaxFinalScore)
                .FirstOrDefaultAsync() ?? 100m;

            var status = studentPassed_FinalFinalExamScore >= (maxFinalScore / 2m)
                ? SubjectStatus.Passed
                : SubjectStatus.Failed;

            context.StudentSubjectTermResults.Add(new StudentSubjectTermResult
            {
                StudentID = studentId,
                SubjectID = subjectId,
                TermID = term.TermID,
                AcademicYearID = academicYearId,
                Quarter1Score = quarter1,
                Quarter2Score = quarter2,
                FinalExamScore = studentPassed_FinalFinalExamScore,
                TermTotal = termTotal,
                Status = status,
                CreatedAt = DateTime.UtcNow
            });

            await context.SaveChangesAsync();
        }
    }
}

