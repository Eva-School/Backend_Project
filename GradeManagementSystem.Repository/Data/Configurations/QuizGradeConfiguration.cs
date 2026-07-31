using GradeManagementSystem.Core.Entities.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GradeManagementSystem.Repository.Data.Configurations
{
    public class QuizGradeConfiguration : IEntityTypeConfiguration<QuizGrade>
    {
        public void Configure(EntityTypeBuilder<QuizGrade> builder)
        {
            builder.ToTable("QuizGrades");

            builder.HasKey(qg => qg.QuizGradeID);

            builder.HasIndex(qg => new { qg.QuizID, qg.StudentID })
                .IsUnique();

            builder.Property(qg => qg.Score)
                .HasColumnType("decimal(18,2)");

            builder.Property(qg => qg.Notes)
                .HasMaxLength(250);

            builder.HasOne(qg => qg.Quiz)
                .WithMany(q => q.QuizGrades)
                .HasForeignKey(qg => qg.QuizID)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(qg => qg.Student)
                .WithMany()
                .HasForeignKey(qg => qg.StudentID)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
