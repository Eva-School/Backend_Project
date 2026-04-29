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
    public class SubjectConfiguration : IEntityTypeConfiguration<Subject>
    {
        public void Configure(EntityTypeBuilder<Subject> builder)
        {
            builder.ToTable("Subjects");

            builder.HasKey(s => s.SubjectID);

            builder.Property(s => s.SubjectName)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(s => s.MaxFinalScore);

            builder.Property(s => s.MaxQuarterScore);

            builder.Property(s => s.IsActive)
                .IsRequired()
                .HasDefaultValue(true);

            builder.HasOne(s => s.AcademicYear)
                .WithMany(a => a.Subjects)
                .HasForeignKey(s => s.AcademicYearID)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasData(
                new Subject
                {
                    SubjectID = 1,
                    SubjectName = "Mathematics",
                    AcademicYearID = 3,
                    MaxFinalScore = 100,
                    MaxQuarterScore = 25,
                    MaxQuarterQ1Score = 12,
                    MaxQuarterQ2Score = 13,
                    MaxQuarterQ3Score = 12,
                    MaxQuarterQ4Score = 13,
                    IsActive = true
                },
                new Subject
                {
                    SubjectID = 2,
                    SubjectName = "English",
                    AcademicYearID = 3,
                    MaxFinalScore = 100,
                    MaxQuarterScore = 25,
                    MaxQuarterQ1Score = 12,
                    MaxQuarterQ2Score = 13,
                    MaxQuarterQ3Score = 12,
                    MaxQuarterQ4Score = 13,
                    IsActive = true
                }
            );
        }
    }
}
