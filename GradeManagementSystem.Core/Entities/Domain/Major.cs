using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GradeManagementSystem.Core.Entities.Domain
{
    public class Major
    {
        [Key]
        public int MajorID { get; set; }

        [ForeignKey("Department")]
        public int? DepartmentID { get; set; }

        [Required]
        [StringLength(200)]
        public string MajorName { get; set; }

        public string Description { get; set; }

        [Required]
        public bool IsActive { get; set; }

        // Navigation Properties
        public Department? Department { get; set; }
        public ICollection<Student> Students { get; set; } = new List<Student>();
        public ICollection<Competency> Competencies { get; set; } = new List<Competency>();
    }
}
