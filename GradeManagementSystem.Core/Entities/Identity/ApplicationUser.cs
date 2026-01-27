using GradeManagementSystem.Core.Entities.Domain;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GradeManagementSystem.Core.Entities.Identity
{
    public class ApplicationUser : IdentityUser<int>
    {
        public int UserId { get; set; }
        public string FirstName { get; set; }
        public string? MiddleName { get; set; }
        public string LastName { get; set; }
        public string FullName { get; set; }
        public bool IsActive { get; set; }
        public DateTime? LastLoginAt { get; set; }
        public DateTime CreatedAt { get; set; }

        public int RoleId { get; set; }  // FK

        // Navigation Properties
        public ApplicationRole Role { get; set; }
        public Student? Student { get; set; }
        public Teacher? Teacher { get; set; }
    }
}
