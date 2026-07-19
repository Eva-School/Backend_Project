using GradeManagementSystem.Core.Entities.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GradeManagementSystem.Repository.Data.Configurations
{
    public class QuarterGradeSubmissionConfiguration : IEntityTypeConfiguration<QuarterGradeSubmission>
    {
        public void Configure(EntityTypeBuilder<QuarterGradeSubmission> builder)
        {
            builder.ToTable("QuarterGradeSubmissions");

            builder.HasKey(x => x.SubmissionID);

            builder.Property(x => x.SubmittedAt)
                .IsRequired()
                .HasDefaultValueSql("NOW()");

            builder.HasIndex(x => new { x.StudentID, x.SubjectID, x.AcademicYearID, x.TermID })
                .IsUnique();
        }
    }
}

