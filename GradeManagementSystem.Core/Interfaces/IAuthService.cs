using GradeManagementSystem.Core.DTOs.Auth;
using System.Threading.Tasks;

namespace GradeManagementSystem.Core.Interfaces
{
    public interface IAuthService
    {
        Task<AuthResponse?> LoginAsync(LoginRequest request);
        Task<AuthResponse?> RefreshTokenAsync(string refreshToken);
        Task<bool> LogoutAsync(string refreshToken);
        Task<UserInfoResponse?> GetUserInfoAsync(int userId);
        Task<object> RegisterTeacherAsync(GradeManagementSystem.Core.DTOs.Teacher.TeacherRegisterRequest request);
    }
}
