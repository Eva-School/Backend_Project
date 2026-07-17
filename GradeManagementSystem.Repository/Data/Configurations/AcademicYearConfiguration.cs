using GradeManagementSystem.Core.Entities.Domain;
using GradeManagementSystem.Core.Entities.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GradeManagementSystem.Repository.Data.Configurations
{
    public class AcademicYearConfiguration : IEntityTypeConfiguration<AcademicYear>
    {
        public void Configure(EntityTypeBuilder<AcademicYear> builder)
        {
            builder.ToTable("AcademicYears");

            builder.HasKey(a => a.AcademicYearID);

            builder.Property(a => a.YearName)
                   .IsRequired()
                   .HasMaxLength(100);

            builder.Property(a => a.Stage)
                   .HasConversion<string>()
                   .HasMaxLength(20)
                   .IsRequired();

            builder.Property(a => a.IsActive)
                   .IsRequired();

            builder.HasIndex(a => new { a.YearName, a.Stage })
                   .IsUnique();

            builder.HasData(
                new AcademicYear
                {
                    AcademicYearID = 1,
                    YearName = "2022-2023",
                    Stage = EducationStage.Junior,
                    IsActive = false
                },
                new AcademicYear
                {
                    AcademicYearID = 2,
                    YearName = "2023-2024",
                    Stage = EducationStage.Wheeler,
                    IsActive = false
                },
                new AcademicYear
                {
                    AcademicYearID = 3,
                    YearName = "2024-2025",
                    Stage = EducationStage.Senior,
                    IsActive = true
                },
                new AcademicYear
                {
                    AcademicYearID = 4,
                    YearName = "2024-2025",
                    Stage = EducationStage.Junior,
                    IsActive = true
                }
                ,
                new AcademicYear
                {
                    AcademicYearID = 5,
                    YearName = "2024-2025",
                    Stage = EducationStage.Wheeler,
                    IsActive = true
                }
            );
        }
    }
}
