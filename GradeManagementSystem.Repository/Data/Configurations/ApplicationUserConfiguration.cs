using GradeManagementSystem.Core.Entities.Domain;
using GradeManagementSystem.Core.Entities.Identities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GradeManagementSystem.Repository.Data.Configurations
{
    public class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
    {
        public void Configure(EntityTypeBuilder<ApplicationUser> builder)
        {
            builder.ToTable("Users");

            // Primary Key
            builder.HasKey(u => u.UserId);

            builder.Property(u => u.UserId)
                .HasColumnName("UserID");

            builder.Property(u => u.FirstName)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(u => u.MiddleName)
                .HasMaxLength(100);

            builder.Property(u => u.LastName)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(u => u.FullName)
                .IsRequired()
                .HasMaxLength(300);

            builder.Property(u => u.IsActive)
                .IsRequired()
                .HasDefaultValue(true);

            builder.Property(u => u.CreatedAt)
                .IsRequired()
                .HasDefaultValueSql("GETDATE()");

            // Foreign Key to Role
            builder.Property(u => u.RoleId)
                .HasColumnName("RoleID")
                .IsRequired();

            builder.HasOne(u => u.Role)
                .WithMany(r => r.Users)
                .HasForeignKey(u => u.RoleId)
                .OnDelete(DeleteBehavior.Restrict);

            // Navigation to Student (One-to-One)
            builder.HasOne(u => u.Student)
                .WithOne()
                .HasForeignKey<Student>(s => s.UserID)
                .OnDelete(DeleteBehavior.Cascade);

            // Navigation to Teacher (One-to-One)
            builder.HasOne(u => u.Teacher)
                .WithOne()
                .HasForeignKey<Teacher>(t => t.UserID)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
