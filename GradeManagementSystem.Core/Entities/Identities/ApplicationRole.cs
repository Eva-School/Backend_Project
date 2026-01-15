using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GradeManagementSystem.Core.Entities.Identities
{
    public class ApplicationRole
    {
        public int RoleId { get; set; }  // PK
        public string RoleName { get; set; }
        public string? Description { get; set; }

        // Navigation
        public ICollection<ApplicationUser> Users { get; set; }
    }
}
