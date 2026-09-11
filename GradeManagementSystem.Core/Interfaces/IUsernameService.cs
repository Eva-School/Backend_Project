using System.Collections.Generic;
using System.Threading.Tasks;

namespace GradeManagementSystem.Core.Interfaces
{
    public interface IUsernameService
    {
        /// <summary>
        /// Extracts and sanitizes the base username from an email address's local part.
        /// E.g., "userA@example.com" -> "userA", "john.doe+alias@domain.com" -> "john.doe"
        /// </summary>
        string ExtractBaseUsernameFromEmail(string email);

        /// <summary>
        /// Generates a unique, conflict-free username derived from the email address.
        /// If "userA" is available, returns "userA".
        /// If "userA" is already taken, deterministically tries "userA1", "userA2", etc.
        /// </summary>
        Task<string> GenerateUniqueUsernameFromEmailAsync(
            string email,
            int? excludeUserId = null,
            ISet<string>? reservedInBatch = null);

        /// <summary>
        /// Checks whether the candidate username is currently available.
        /// </summary>
        Task<bool> IsUsernameAvailableAsync(
            string username,
            int? excludeUserId = null,
            ISet<string>? reservedInBatch = null);
    }
}
