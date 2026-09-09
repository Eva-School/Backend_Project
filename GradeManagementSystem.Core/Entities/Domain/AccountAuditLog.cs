using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GradeManagementSystem.Core.Entities.Domain
{
    public class AccountAuditLog
    {
        [Key]
        public int LogId { get; set; }

        [Required]
        [StringLength(100)]
        public string Action { get; set; } = string.Empty;

        public int ActorUserId { get; set; }

        [Required]
        [StringLength(100)]
        public string ActorUsername { get; set; } = string.Empty;

        public int TargetUserId { get; set; }

        [Required]
        [StringLength(100)]
        public string TargetUsername { get; set; } = string.Empty;

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        [Required]
        [StringLength(50)]
        public string Outcome { get; set; } = "Success";

        [StringLength(1000)]
        public string? Details { get; set; }

        [StringLength(100)]
        public string? IpAddress { get; set; }
    }
}
