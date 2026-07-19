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
    public class TeacherConfiguration : IEntityTypeConfiguration<Teacher>
    {
        public void Configure(EntityTypeBuilder<Teacher> builder)
        {
            builder.ToTable("Teachers");

            builder.HasKey(t => t.TeacherID);

            builder.Property(t => t.EmployeeCode)
                .HasMaxLength(50);

            builder.Property(t => t.Qualifications)
                .HasColumnType("text");

            builder.Property(t => t.IsActive)
                .IsRequired()
                .HasDefaultValue(true);

            builder.HasOne<ApplicationUser>()
                .WithOne(u => u.Teacher)
                .HasForeignKey<Teacher>(t => t.UserID)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(t => t.Department)
                .WithMany(d => d.Teachers)
                .HasForeignKey(t => t.DepartmentID)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(t => t.EmployeeCode).IsUnique();
            builder.HasIndex(t => t.UserID).IsUnique();

            builder.HasData(
                new Teacher { TeacherID = 1, EmployeeCode = "TCH001", Qualifications = "B.Sc. Mathematics", IsActive = true, HireDate = DateTime.SpecifyKind(new DateTime(2020, 1, 1), DateTimeKind.Utc) },
                new Teacher { TeacherID = 2, EmployeeCode = "TCH002", Qualifications = "B.A. English", IsActive = true, HireDate = DateTime.SpecifyKind(new DateTime(2021, 5, 15), DateTimeKind.Utc) }
            );
        }
    }
}
