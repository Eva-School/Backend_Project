using GradeManagementSystem.Core.Entities.Identities;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GradeManagementSystem.Repository.Identity
{
    public class ApplicationIdentityRole : IdentityRole<int>
    {
        public int RoleId { get; set; }  // FK to ApplicationRole.RoleId
        public ApplicationRole ApplicationRole { get; set; }
    }
}
