namespace GradeManagementSystem.Core.DTOs.Auth
{
    public class UserInfoResponse
    {
        public int UserId { get; set; }
        public string Role { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }
}
