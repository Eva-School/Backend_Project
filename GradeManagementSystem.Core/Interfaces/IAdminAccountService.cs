using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GradeManagementSystem.Core.DTOs.Admin.Account;

namespace GradeManagementSystem.Core.Interfaces
{
    public interface IAdminAccountService
    {
        Task<AccountPagedResultDto<AccountSummaryDto>> GetAccountsAsync(AccountListQueryDto query, CancellationToken cancellationToken = default);
        Task<AccountDetailDto?> GetAccountByIdAsync(int userId, CancellationToken cancellationToken = default);
        Task<CreateAccountResultDto> CreateAccountAsync(CreateAccountDto request, int currentAdminUserId, string? ipAddress = null, CancellationToken cancellationToken = default);
        Task<AccountDetailDto> UpdateAccountAsync(int userId, UpdateAccountProfileDto request, int currentAdminUserId, string? ipAddress = null, CancellationToken cancellationToken = default);
        Task<bool> ChangeRoleAsync(int userId, ChangeUserRoleDto request, int currentAdminUserId, string? ipAddress = null, CancellationToken cancellationToken = default);
        Task<bool> SetStatusAsync(int userId, bool isActive, int currentAdminUserId, string? ipAddress = null, CancellationToken cancellationToken = default);
        Task<bool> ResetPasswordAsync(int userId, ResetPasswordDto request, int currentAdminUserId, string? ipAddress = null, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<RoleOptionDto>> GetRolesAsync(CancellationToken cancellationToken = default);
    }
}
