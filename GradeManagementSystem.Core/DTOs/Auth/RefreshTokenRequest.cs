using System.ComponentModel.DataAnnotations;

namespace GradeManagementSystem.Core.DTOs.Auth
{
    public class RefreshTokenRequest
    {
        [Required(ErrorMessage = "Refresh token is required")]
        public string RefreshToken { get; set; }
    }
}
