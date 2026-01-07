using GradeManagementSystem.Core.Entities.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GradeManagementSystem.Core.Entities.Domain
{
    public class ResultApproval
    {
        [Key]
        public int ApprovalID { get; set; }

        [ForeignKey("StudentAllResult")]
        public int? AllResultID { get; set; }

        [Required]
        public Decision Decision { get; set; }

        public string Notes { get; set; }

        public int? ApprovedBy { get; set; }

        public DateTime? ApprovalDate { get; set; }

        // Navigation Properties
        public virtual StudentAllResults StudentAllResults { get; set; }
    }
}
