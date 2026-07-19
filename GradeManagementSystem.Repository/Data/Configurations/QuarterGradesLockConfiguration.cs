using GradeManagementSystem.Core.Entities.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GradeManagementSystem.Repository.Data.Configurations
{
    public class QuarterGradesLockConfiguration : IEntityTypeConfiguration<QuarterGradesLock>
    {
        public void Configure(EntityTypeBuilder<QuarterGradesLock> builder)
        {
            builder.ToTable("QuarterGradesLocks");

            builder.HasKey(x => new { x.AcademicYearID, x.SubjectID, x.DepartmentID, x.ClassID });

            builder.Property(x => x.LockedAt)
                .IsRequired()
                .HasDefaultValueSql("NOW()");

            builder.Property(x => x.LockedBy);
        }
    }
}

