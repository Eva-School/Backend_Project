using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GradeManagementSystem.Core.Entities.Identity
{
    public class ApplicationRole : IdentityRole<int>
    {
        public int RoleId { get; set; }  // PK
        public string RoleName { get; set; }
        public string? Description { get; set; }

        // Navigation
        public ICollection<ApplicationUser> Users { get; set; }
    }
}
