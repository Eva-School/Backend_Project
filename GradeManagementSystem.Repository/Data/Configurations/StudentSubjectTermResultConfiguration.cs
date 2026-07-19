using GradeManagementSystem.Core.Entities.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GradeManagementSystem.Repository.Data.Configurations
{
    public class StudentSubjectTermResultConfiguration : IEntityTypeConfiguration<StudentSubjectTermResult>
    {
        public void Configure(EntityTypeBuilder<StudentSubjectTermResult> builder)
        {
            builder.ToTable("StudentSubjectTermResults");

            builder.HasKey(r => r.ResultID);

            builder.Property(r => r.Quarter1Score)
                .HasColumnType("decimal(5,2)");

            builder.Property(r => r.Quarter3Score)
                .HasColumnType("decimal(5,2)");

            builder.Property(r => r.Quarter2Score)
                .HasColumnType("decimal(5,2)");

            builder.Property(r => r.Quarter4Score)
                .HasColumnType("decimal(5,2)");

            builder.Property(r => r.FinalExamScore)
                .HasColumnType("decimal(5,2)");

            builder.Property(r => r.TermTotal)
                .HasColumnType("decimal(5,2)");

            builder.Property(r => r.Status)
                .IsRequired()
                .HasConversion<string>();

            builder.Property(r => r.CreatedAt)
                .IsRequired()
                .HasDefaultValueSql("NOW()");

            builder.HasOne(r => r.Student)
                .WithMany(s => s.SubjectTermResults)
                .HasForeignKey(r => r.StudentID)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(r => r.Subject)
                .WithMany(sub => sub.SubjectTermResults)
                .HasForeignKey(r => r.SubjectID)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(r => r.Term)
                .WithMany(t => t.SubjectTermResults)
                .HasForeignKey(r => r.TermID)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(r => r.AcademicYear)
                .WithMany(a => a.SubjectTermResults)
                .HasForeignKey(r => r.AcademicYearID)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(r => new { r.StudentID, r.SubjectID, r.TermID, r.AcademicYearID }).IsUnique();
        }
    }
}
