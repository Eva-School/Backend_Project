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
    public class ClassConfiguration : IEntityTypeConfiguration<Class>
    {
        public void Configure(EntityTypeBuilder<Class> builder)
        {
            builder.ToTable("Classes");

            builder.HasKey(c => c.ClassID);

            builder.Property(c => c.ClassName)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(c => c.Capacity)
                .IsRequired();

            builder.Property(c => c.IsActive)
                .IsRequired()
                .HasDefaultValue(true);

            builder.HasOne(c => c.AcademicYear)
                .WithMany(a => a.Classes)
                .HasForeignKey(c => c.AcademicYearID)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(c => c.Department)
                .WithMany(d => d.Classes)
                .HasForeignKey(c => c.DepartmentID)
                .OnDelete(DeleteBehavior.Restrict);

            // A class name may be reused in a later academic year (for example,
            // "Class 1A" in 2024-2025 and 2025-2026). It must only be unique
            // within its academic year and department.
            builder.HasIndex(c => new { c.AcademicYearID, c.DepartmentID, c.ClassName })
                .IsUnique();

            builder.HasData(
                new Class { ClassID = 1, ClassName = "Class 1A", AcademicYearID = 3, Capacity = 30, IsActive = true },
                new Class { ClassID = 2, ClassName = "Class 1B", AcademicYearID = 3, Capacity = 30, IsActive = true },
                new Class { ClassID = 3, ClassName = "Class 2A", AcademicYearID = 3, Capacity = 30, IsActive = true }
            );
        }
    }
}
