using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GradeManagementSystem.Core.Entities.Domain
{
    public class StudentCompetencyStatus
    {
        [Key]
        public int StudentCompetencyStatusID { get; set; }

        [ForeignKey("Student")]
        public int? StudentID { get; set; }

        [ForeignKey("Competency")]
        public int? CompetencyID { get; set; }

        [StringLength(50)]
        public string StatusID { get; set; }

        public int? CurrentAttemptNumber { get; set; }

        public int? MaxAllowedAttempts { get; set; }

        public DateTime? LastEvaluatedAt { get; set; }

        // Navigation Properties
        public virtual Student Student { get; set; }
        public virtual Competency Competency { get; set; }
        public virtual ICollection<CompetencyAttempt> CompetencyAttempts { get; set; } = new List<CompetencyAttempt>();
    }
}
