using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using GradeManagementSystem.Core.DTOs.Auth;
using Xunit;

namespace GradeManagementSystem.Tests
{
    public class AuthValidationTests
    {
        [Fact]
        public void LoginRequest_Validates_Successfully_With_Valid_Email_And_Password()
        {
            var request = new LoginRequest
            {
                Email = "admin@system.com",
                Password = "Admin@123"
            };

            var results = new List<ValidationResult>();
            var isValid = Validator.TryValidateObject(request, new ValidationContext(request), results, true);

            Assert.True(isValid);
            Assert.Empty(results);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null)]
        public void LoginRequest_Fails_When_Email_Is_Missing(string? email)
        {
            var request = new LoginRequest
            {
                Email = email!,
                Password = "Password@123"
            };

            var results = new List<ValidationResult>();
            var isValid = Validator.TryValidateObject(request, new ValidationContext(request), results, true);

            Assert.False(isValid);
            Assert.Contains(results, r => r.MemberNames.Contains(nameof(LoginRequest.Email)));
        }

        [Theory]
        [InlineData("notanemail")]
        [InlineData("missingatsign.com")]
        [InlineData("@nodomain")]
        [InlineData("user@")]
        public void LoginRequest_Fails_When_Email_Format_Is_Invalid(string invalidEmail)
        {
            var request = new LoginRequest
            {
                Email = invalidEmail,
                Password = "Password@123"
            };

            var results = new List<ValidationResult>();
            var isValid = Validator.TryValidateObject(request, new ValidationContext(request), results, true);

            Assert.False(isValid);
            Assert.Contains(results, r => r.MemberNames.Contains(nameof(LoginRequest.Email)));
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null)]
        public void LoginRequest_Fails_When_Password_Is_Missing(string? password)
        {
            var request = new LoginRequest
            {
                Email = "valid@system.com",
                Password = password!
            };

            var results = new List<ValidationResult>();
            var isValid = Validator.TryValidateObject(request, new ValidationContext(request), results, true);

            Assert.False(isValid);
            Assert.Contains(results, r => r.MemberNames.Contains(nameof(LoginRequest.Password)));
        }

        [Fact]
        public void LoginRequest_Fails_When_Email_Exceeds_MaxLength()
        {
            var longEmail = new string('a', 250) + "@domain.com"; // > 256 chars
            var request = new LoginRequest
            {
                Email = longEmail,
                Password = "Password@123"
            };

            var results = new List<ValidationResult>();
            var isValid = Validator.TryValidateObject(request, new ValidationContext(request), results, true);

            Assert.False(isValid);
            Assert.Contains(results, r => r.MemberNames.Contains(nameof(LoginRequest.Email)));
        }
    }
}
