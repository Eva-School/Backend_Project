using GradeManagementSystem.Core.Entities.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GradeManagementSystem.Repository.Data.Configurations
{
    public class AccountAuditLogConfiguration : IEntityTypeConfiguration<AccountAuditLog>
    {
        public void Configure(EntityTypeBuilder<AccountAuditLog> builder)
        {
            builder.ToTable("AccountAuditLogs");

            builder.HasKey(l => l.LogId);

            builder.Property(l => l.LogId)
                .HasColumnName("LogID");

            builder.Property(l => l.Action)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(l => l.ActorUsername)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(l => l.TargetUsername)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(l => l.Outcome)
                .IsRequired()
                .HasMaxLength(50);

            builder.Property(l => l.Details)
                .HasMaxLength(1000);

            builder.Property(l => l.IpAddress)
                .HasMaxLength(100);

            builder.Property(l => l.Timestamp)
                .IsRequired()
                .HasDefaultValueSql("NOW()");

            builder.HasIndex(l => l.TargetUserId);
            builder.HasIndex(l => l.Timestamp);
        }
    }
}
