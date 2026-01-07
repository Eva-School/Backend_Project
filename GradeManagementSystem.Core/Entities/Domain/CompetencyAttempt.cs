using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GradeManagementSystem.Core.Entities.Domain
{
    public class CompetencyAttempt
    {
        [Key]
        public int AttemptID { get; set; }

        [ForeignKey("StudentCompetencyStatus")]
        public int? StudentCompetencyStatusID { get; set; }

        [ForeignKey("Student")]
        public int? StudentID { get; set; }

        public int? AttemptNumber { get; set; }

        [StringLength(50)]
        public string Result { get; set; }

        [ForeignKey("Evaluator")]
        public int? EvaluatedBy { get; set; }

        public DateTime? EvaluatedAt { get; set; }

        // Navigation Properties
        public virtual StudentCompetencyStatus StudentCompetencyStatus { get; set; }
        public virtual Student Student { get; set; }
        public virtual Teacher? Evaluator { get; set; }
    }
}
