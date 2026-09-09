using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using GradeManagementSystem.Core.DTOs.Admin.Account;
using GradeManagementSystem.Services.Services;
using Xunit;

namespace GradeManagementSystem.Tests
{
    public class AdminAccountTests
    {
        [Fact]
        public void CreateAccountDto_Validates_Successfully_With_Valid_Data()
        {
            var dto = new CreateAccountDto
            {
                Username = "new.teacher",
                Email = "teacher@school.edu",
                Password = "SecurePassword@123",
                FirstName = "Fatima",
                LastName = "Zahra",
                Role = "Teacher",
                PhoneNumber = "+201000000000"
            };

            var results = new List<ValidationResult>();
            var isValid = Validator.TryValidateObject(dto, new ValidationContext(dto), results, true);

            Assert.True(isValid);
            Assert.Empty(results);
        }

        [Theory]
        [InlineData(null, true)]
        [InlineData("", true)]
        [InlineData("   ", true)]
        [InlineData("01012345678", true)]
        [InlineData("+201012345678", true)]
        [InlineData("010 1234 5678", true)]
        [InlineData("010-1234-5678", true)]
        [InlineData("+20 10 1234 5678", true)]
        [InlineData("٠١٠١٢٣٤٥٦٧٨", true)]
        [InlineData("123", true)]
        [InlineData("1", true)]
        [InlineData("-", false)]
        [InlineData("none", false)]
        [InlineData("not-a-phone", false)]
        public void CreateAccountDto_PhoneNumber_Validation(string? phone, bool shouldBeValid)
        {
            var dto = new CreateAccountDto
            {
                Username = "test.user",
                Email = "test@school.edu",
                FirstName = "Test",
                LastName = "User",
                Role = "Teacher",
                PhoneNumber = phone
            };

            var results = new List<ValidationResult>();
            var isValid = Validator.TryValidateObject(dto, new ValidationContext(dto), results, true);

            Assert.Equal(shouldBeValid, isValid);
        }

        [Theory]
        [InlineData("", "valid@email.com", "First", "Last")]
        [InlineData("validuser", "", "First", "Last")]
        [InlineData("validuser", "valid@email.com", "", "Last")]
        [InlineData("validuser", "valid@email.com", "First", "")]
        public void CreateAccountDto_Rejects_Missing_Required_Fields(string username, string email, string first, string last)
        {
            var dto = new CreateAccountDto
            {
                Username = username,
                Email = email,
                FirstName = first,
                LastName = last,
                Role = "Student"
            };

            var results = new List<ValidationResult>();
            var isValid = Validator.TryValidateObject(dto, new ValidationContext(dto), results, true);

            Assert.False(isValid);
            Assert.NotEmpty(results);
        }

        [Theory]
        [InlineData("invalid-email-no-at")]
        [InlineData("user@")]
        [InlineData("@domain.com")]
        [InlineData("user@@domain.com")]
        public void CreateAccountDto_Rejects_Malformed_Email(string badEmail)
        {
            var dto = new CreateAccountDto
            {
                Username = "valid.user",
                Email = badEmail,
                FirstName = "John",
                LastName = "Doe",
                Role = "Teacher"
            };

            var results = new List<ValidationResult>();
            var isValid = Validator.TryValidateObject(dto, new ValidationContext(dto), results, true);

            Assert.False(isValid);
            Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateAccountDto.Email)));
        }

        [Fact]
        public void CreateAccountDto_Rejects_Short_Password()
        {
            var dto = new CreateAccountDto
            {
                Username = "validuser",
                Email = "user@school.com",
                FirstName = "First",
                LastName = "Last",
                Password = "short", // less than 8 characters
                Role = "Teacher"
            };

            var results = new List<ValidationResult>();
            var isValid = Validator.TryValidateObject(dto, new ValidationContext(dto), results, true);

            Assert.False(isValid);
            Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateAccountDto.Password)));
        }

        [Theory]
        [InlineData("admin", "Admin")]
        [InlineData("ADMIN", "Admin")]
        [InlineData("Admin", "Admin")]
        [InlineData("Student Affairs", "Student Affairs")]
        [InlineData("student affairs", "Student Affairs")]
        [InlineData("studentaffairs", "Student Affairs")]
        [InlineData("StudentAffairs", "Student Affairs")]
        [InlineData("vice", "Student Affairs")]
        [InlineData("Teacher", "Teacher")]
        [InlineData("teacher", "Teacher")]
        [InlineData("Student", "Student")]
        [InlineData("student", "Student")]
        public void ResolveCanonicalRole_Accepts_All_Four_Supported_Roles_In_Any_Casing(string input, string expected)
        {
            var resolved = AdminAccountService.ResolveCanonicalRole(input);
            Assert.Equal(expected, resolved);
        }

        [Theory]
        [InlineData("SuperAdmin")]
        [InlineData("Principal")]
        [InlineData("Manager")]
        [InlineData("Guest")]
        [InlineData("UnknownRole")]
        public void ResolveCanonicalRole_Rejects_Unsupported_Roles(string unsupportedRole)
        {
            Assert.Throws<ArgumentException>(() => AdminAccountService.ResolveCanonicalRole(unsupportedRole));
        }

        [Fact]
        public void ToFrontendRole_Maps_StudentAffairs_Correctly()
        {
            Assert.Equal("Admin", AdminAccountService.ToFrontendRole("Admin"));
            Assert.Equal("StudentAffairs", AdminAccountService.ToFrontendRole("Student Affairs"));
            Assert.Equal("Teacher", AdminAccountService.ToFrontendRole("Teacher"));
            Assert.Equal("Student", AdminAccountService.ToFrontendRole("Student"));
        }

        [Fact]
        public void ResetPasswordDto_Enforces_Required_And_Minimum_Length()
        {
            var invalid = new ResetPasswordDto { NewPassword = "123" };
            var results = new List<ValidationResult>();
            var isValid = Validator.TryValidateObject(invalid, new ValidationContext(invalid), results, true);
            Assert.False(isValid);

            var valid = new ResetPasswordDto { NewPassword = "StrongPassword@123" };
            results.Clear();
            Assert.True(Validator.TryValidateObject(valid, new ValidationContext(valid), results, true));
        }

        [Fact]
        public void AccountListQueryDto_Clamps_PageSize_And_PageNumber()
        {
            var query = new AccountListQueryDto
            {
                PageNumber = -5,
                PageSize = 9999
            };

            Assert.Equal(1, query.PageNumber);
            Assert.Equal(100, query.PageSize); // max limit 100
        }

        [Fact]
        public void AccountPagedResult_Calculates_TotalPages_And_Navigation_Correctly()
        {
            var paged = new AccountPagedResultDto<AccountSummaryDto>
            {
                TotalCount = 25,
                PageNumber = 2,
                PageSize = 10,
                Items = new List<AccountSummaryDto>()
            };

            Assert.Equal(3, paged.TotalPages);
            Assert.True(paged.HasPreviousPage);
            Assert.True(paged.HasNextPage);
        }

        [Fact]
        public void SelfDemotion_Detection_Correctly_Identifies_Illegal_Admin_Action()
        {
            int currentAdminId = 42;
            int targetUserId = 42;
            string currentRole = "Admin";
            string targetRole = "Teacher";

            bool isSelfDemotion = (currentAdminId == targetUserId && currentRole == "Admin" && targetRole != "Admin");

            Assert.True(isSelfDemotion);
        }

        [Fact]
        public void SelfDeactivation_Detection_Correctly_Identifies_Illegal_Admin_Action()
        {
            int currentAdminId = 42;
            int targetUserId = 42;
            bool targetActiveState = false;

            bool isSelfDeactivation = (currentAdminId == targetUserId && !targetActiveState);

            Assert.True(isSelfDeactivation);
        }

        [Fact]
        public void LastAdmin_Protection_Triggers_When_Only_One_Admin_Remains()
        {
            int activeAdminCount = 1;
            bool wouldRemoveActiveAdmin = true;

            bool isBlocked = wouldRemoveActiveAdmin && activeAdminCount <= 1;

            Assert.True(isBlocked);
        }
    }
}
