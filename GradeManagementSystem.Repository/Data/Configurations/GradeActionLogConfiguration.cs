using GradeManagementSystem.Core.Entities.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GradeManagementSystem.Repository.Data.Configurations
{
    public class GradeActionLogConfiguration : IEntityTypeConfiguration<GradeActionLog>
    {
        public void Configure(EntityTypeBuilder<GradeActionLog> builder)
        {
            builder.ToTable("GradeActionLogs");

            builder.HasKey(x => x.ActionLogID);

            builder.Property(x => x.Action)
                .IsRequired()
                .HasColumnType("text");

            builder.Property(x => x.Timestamp)
                .IsRequired()
                .HasDefaultValueSql("GETDATE()");
        }
    }
}

