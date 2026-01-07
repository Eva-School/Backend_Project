using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GradeManagementSystem.Core.Entities.Domain
{
    public class Guardian
    {
        [Key]
        public int GuardianID { get; set; }

        [ForeignKey("Student")]
        public int? StudentID { get; set; }

        [StringLength(200)]
        public string GuardianName { get; set; }

        [StringLength(50)]
        public string GuardianRelation { get; set; }

        [StringLength(20)]
        public string GuardianPhone { get; set; }

        // Navigation Properties
        public virtual Student Student { get; set; }
    }
}
