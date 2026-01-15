using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GradeManagementSystem.Core.Entities.Domain
{
    public class StudentPromotion
    {
        [Key]
        public int PromotionID { get; set; }

        [ForeignKey("Student")]
        public int? StudentID { get; set; }

        [ForeignKey("FromAcademicYear")]
        public int? FromAcademicYearID { get; set; }

        [ForeignKey("ToAcademicYear")]
        public int? ToAcademicYearID { get; set; }

        public DateTime? RequestDate { get; set; }

        public bool? IsApproved { get; set; }

        public int? ApprovedBy { get; set; }

        public DateTime? ApprovalDate { get; set; }

        public int? RequestedBy { get; set; }

        // Navigation Properties
        public virtual Student Student { get; set; }
        public virtual AcademicYear FromAcademicYear { get; set; }
        public virtual AcademicYear ToAcademicYear { get; set; }
    }
}
