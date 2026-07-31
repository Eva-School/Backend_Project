using GradeManagementSystem.Core.Entities.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GradeManagementSystem.Repository.Data.Configurations
{
    public class QuizConfiguration : IEntityTypeConfiguration<Quiz>
    {
        public void Configure(EntityTypeBuilder<Quiz> builder)
        {
            builder.ToTable("Quizzes");

            builder.HasKey(q => q.QuizID);

            builder.Property(q => q.Title)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(q => q.MaxScore)
                .IsRequired()
                .HasColumnType("decimal(18,2)");

            builder.Property(q => q.Description)
                .HasMaxLength(500);

            builder.HasOne(q => q.Class)
                .WithMany()
                .HasForeignKey(q => q.ClassID)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(q => q.Subject)
                .WithMany()
                .HasForeignKey(q => q.SubjectID)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(q => q.AcademicYear)
                .WithMany()
                .HasForeignKey(q => q.AcademicYearID)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(q => q.Teacher)
                .WithMany()
                .HasForeignKey(q => q.CreatedByTeacherID)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
