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
    public class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
    {
        public void Configure(EntityTypeBuilder<ApplicationUser> builder)
        {
            builder.ToTable("AspNetUsers");

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

            // Seed Default Users
            var hasher = new Microsoft.AspNetCore.Identity.PasswordHasher<ApplicationUser>();

            builder.HasData(
                new ApplicationUser
                {
                    UserId = 1,
                    Id = 1,
                    UserName = "admin",
                    NormalizedUserName = "ADMIN",
                    Email = "admin@system.com",
                    NormalizedEmail = "ADMIN@SYSTEM.COM",
                    EmailConfirmed = true,
                    PasswordHash = hasher.HashPassword(null, "Admin@123"),
                    FirstName = "System",
                    LastName = "Admin",
                    FullName = "System Admin",
                    IsActive = true,
                    CreatedAt = new DateTime(2026, 1, 1),
                    RoleId = 1,
                    SecurityStamp = Guid.NewGuid().ToString()
                },
                new ApplicationUser
                {
                    UserId = 2,
                    Id = 2,
                    UserName = "staff",
                    NormalizedUserName = "STAFF",
                    Email = "staff@system.com",
                    NormalizedEmail = "STAFF@SYSTEM.COM",
                    EmailConfirmed = true,
                    PasswordHash = hasher.HashPassword(null, "Staff@123"),
                    FirstName = "Student",
                    LastName = "Affairs",
                    FullName = "Student Affairs",
                    IsActive = true,
                    CreatedAt = new DateTime(2026, 1, 1),
                    RoleId = 2,
                    SecurityStamp = Guid.NewGuid().ToString()
                }
            );
        }
    }
}
