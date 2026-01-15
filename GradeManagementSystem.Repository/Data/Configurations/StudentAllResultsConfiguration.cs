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
    public class StudentAllResultsConfiguration : IEntityTypeConfiguration<StudentAllResults>
    {
        public void Configure(EntityTypeBuilder<StudentAllResults> builder)
        {
            builder.ToTable("StudentAllResults");

            builder.HasKey(ar => ar.AllResultID);

            builder.Property(ar => ar.FinalSubjectScore)
                .HasColumnType("decimal(8,2)");

            builder.Property(ar => ar.TotalTermScore)
                .HasColumnType("decimal(8,2)");

            builder.Property(ar => ar.SubjectStatus)
                .IsRequired()
                .HasConversion<string>();

            builder.Property(ar => ar.OverallTermStatus)
                .IsRequired()
                .HasConversion<string>();

            builder.Property(ar => ar.GeneratedAt)
                .IsRequired()
                .HasDefaultValueSql("GETDATE()");

            builder.HasOne(ar => ar.Student)
                .WithMany(s => s.AllResults)
                .HasForeignKey(ar => ar.StudentID)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(ar => ar.Subject)
                .WithMany(sub => sub.AllResults)
                .HasForeignKey(ar => ar.SubjectID)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(ar => ar.Term)
                .WithMany(t => t.AllResults)
                .HasForeignKey(ar => ar.TermID)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(ar => ar.AcademicYear)
                .WithMany(a => a.AllResults)
                .HasForeignKey(ar => ar.AcademicYearID)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(ar => new { ar.StudentID, ar.SubjectID, ar.TermID, ar.AcademicYearID }).IsUnique();
        }
    }
}
