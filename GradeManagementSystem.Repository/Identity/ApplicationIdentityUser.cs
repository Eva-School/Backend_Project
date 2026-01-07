using GradeManagementSystem.Core.Entities.Identities;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GradeManagementSystem.Repository.Identity
{
    public class ApplicationIdentityUser : IdentityUser<int>
    {
        public int UserId { get; set; }  // FK to ApplicationUser.UserId
        public ApplicationUser ApplicationUser { get; set; }
    }
}
