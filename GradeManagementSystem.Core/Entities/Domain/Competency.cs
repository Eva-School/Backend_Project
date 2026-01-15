using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GradeManagementSystem.Core.Entities.Domain
{
    public class Competency
    {
        [Key]
        public int CompetencyID { get; set; }

        [ForeignKey("Major")]
        public int? MajorID { get; set; }

        [Required]
        [StringLength(200)]
        public string CompetencyName { get; set; }

        public int? MaxAttempts { get; set; }

        [Required]
        public bool IsActive { get; set; }

        public DateTime CreatedAt { get; set; }

        // Navigation Properties
        public virtual Major? Major { get; set; }
        public virtual ICollection<StudentCompetencyStatus> StudentCompetencyStatuses { get; set; } = new List<StudentCompetencyStatus>();
    }
}
