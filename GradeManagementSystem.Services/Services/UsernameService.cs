using GradeManagementSystem.Core.Entities.Identity;
using GradeManagementSystem.Core.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace GradeManagementSystem.Services.Services
{
    public class UsernameService : IUsernameService
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public UsernameService(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        public string ExtractBaseUsernameFromEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return "user";
            }

            var trimmedEmail = email.Trim();
            var atIndex = trimmedEmail.IndexOf('@');
            var localPart = atIndex >= 0 ? trimmedEmail.Substring(0, atIndex).Trim() : trimmedEmail;

            // Remove email subaddressing tags (e.g. "userA+alias" -> "userA")
            var plusIndex = localPart.IndexOf('+');
            if (plusIndex >= 0)
            {
                localPart = localPart.Substring(0, plusIndex).Trim();
            }

            // Sanitize: allow letters, digits, dots, underscores, hyphens
            var sb = new StringBuilder(localPart.Length);
            foreach (var c in localPart)
            {
                if (char.IsLetterOrDigit(c) || c == '.' || c == '_' || c == '-')
                {
                    sb.Append(c);
                }
            }

            var sanitized = sb.ToString();

            // Collapse consecutive separators
            sanitized = Regex.Replace(sanitized, @"\.{2,}", ".");
            sanitized = Regex.Replace(sanitized, @"_{2,}", "_");
            sanitized = Regex.Replace(sanitized, @"-{2,}", "-");

            // Trim leading/trailing separators
            sanitized = sanitized.Trim('.', '_', '-');

            if (string.IsNullOrWhiteSpace(sanitized))
            {
                return "user";
            }

            // Truncate to 80 chars if excessively long to leave room for numeric suffixes
            if (sanitized.Length > 80)
            {
                sanitized = sanitized.Substring(0, 80).TrimEnd('.', '_', '-');
            }

            return sanitized;
        }

        public async Task<string> GenerateUniqueUsernameFromEmailAsync(
            string email,
            int? excludeUserId = null,
            ISet<string>? reservedInBatch = null)
        {
            var baseUsername = ExtractBaseUsernameFromEmail(email);
            var normalizedBase = baseUsername.ToUpperInvariant();

            // Single query: load all usernames starting with baseUsername
            var query = _userManager.Users.AsQueryable();
            if (excludeUserId.HasValue)
            {
                query = query.Where(u => u.Id != excludeUserId.Value);
            }

            var existingList = await query
                .Where(u => u.NormalizedUserName != null && u.NormalizedUserName.StartsWith(normalizedBase))
                .Select(u => u.NormalizedUserName!)
                .ToListAsync();

            var taken = new HashSet<string>(existingList, StringComparer.OrdinalIgnoreCase);

            if (reservedInBatch != null)
            {
                foreach (var r in reservedInBatch)
                {
                    if (!string.IsNullOrWhiteSpace(r))
                    {
                        taken.Add(r.Trim());
                    }
                }
            }

            // Candidate 1: exact base username (e.g. "userA")
            if (!taken.Contains(baseUsername))
            {
                reservedInBatch?.Add(baseUsername);
                return baseUsername;
            }

            // Candidate 2+: append numeric suffix (e.g. "userA1", "userA2", "userA3", etc.)
            var counter = 1;
            while (true)
            {
                var candidate = $"{baseUsername}{counter}";
                if (!taken.Contains(candidate))
                {
                    reservedInBatch?.Add(candidate);
                    return candidate;
                }
                counter++;
            }
        }

        public async Task<bool> IsUsernameAvailableAsync(
            string username,
            int? excludeUserId = null,
            ISet<string>? reservedInBatch = null)
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                return false;
            }

            var clean = username.Trim();

            if (reservedInBatch != null && reservedInBatch.Contains(clean))
            {
                return false;
            }

            var user = await _userManager.FindByNameAsync(clean);
            if (user == null)
            {
                return true;
            }

            if (excludeUserId.HasValue && user.Id == excludeUserId.Value)
            {
                return true;
            }

            return false;
        }
    }
}
