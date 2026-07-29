using GradeManagementSystem.Core.Entities.Domain;
using GradeManagementSystem.Core.Entities.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GradeManagementSystem.Repository.Data.Configurations
{
    public class StudentConfiguration : IEntityTypeConfiguration<Student>
    {
        public void Configure(EntityTypeBuilder<Student> builder)
        {
            builder.ToTable("Students");

            builder.HasKey(s => s.StudentID);

            builder.Property(s => s.NationalID)
                .HasMaxLength(50);

            builder.Property(s => s.EnrollmentDate)
                .IsRequired();

            builder.Property(s => s.Status)
                .HasMaxLength(50);

            builder.Property(s => s.Gender)
                .IsRequired()
                .HasConversion<string>();

            builder.Property(s => s.Address)
                .HasMaxLength(250);

            builder.HasOne<ApplicationUser>()
             .WithOne(u => u.Student)
             .HasForeignKey<Student>(s => s.UserID)
             .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(s => s.CurrentAcademicYear)
                .WithMany(a => a.Students)
                .HasForeignKey(s => s.CurrentAcademicYearID)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(s => s.Major)
                .WithMany(m => m.Students)
                .HasForeignKey(s => s.MajorID)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(s => s.Department)
                .WithMany(d => d.Students)
                .HasForeignKey(s => s.DepartmentID)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(s => s.Class)
                .WithMany(c => c.Students)
                .HasForeignKey(s => s.ClassID)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(s => s.NationalID).IsUnique();
            builder.HasIndex(s => s.UserID).IsUnique();
        }
    }
}
