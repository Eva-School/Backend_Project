using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GradeManagementSystem.Core.Entities.Domain
{
    public class Department
    {
        [Key]
        public int DepartmentID { get; set; }

        [Required]
        [StringLength(100)]
        public string DepartmentName { get; set; }

        public string Description { get; set; }

        [Required]
        public bool IsActive { get; set; }

        public DateTime CreatedAt { get; set; }

        // Navigation Properties
        public virtual ICollection<Major> Majors { get; set; } = new List<Major>();
        public virtual ICollection<Class> Classes { get; set; } = new List<Class>();
        public virtual ICollection<Teacher> Teachers { get; set; } = new List<Teacher>();
    }
}
