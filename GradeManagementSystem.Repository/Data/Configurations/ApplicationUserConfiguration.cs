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
                .HasDefaultValueSql("NOW()");

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
                    PasswordHash = "AQAAAAIAAYagAAAAENYA8Zrd5LoGYV68oOm9/E59pSiucbfv+8+e4I5zx9voIAI5REKOkJ2yoA4NxCUPYg==",
                    FirstName = "System",
                    LastName = "Admin",
                    FullName = "System Admin",
                    IsActive = true,
                    CreatedAt = new DateTime(2026, 1, 1),
                    RoleId = 1,
                    SecurityStamp = "bc6b0bd0-2e6d-4631-9f37-5cc9540f40d1",
                    ConcurrencyStamp = "be729c7f-d1ce-4543-8f75-9a453025a340"
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
                    PasswordHash = "AQAAAAIAAYagAAAAEA/hkmvGbeFcTcU81jZWyVAOO+YixBNd9Y/pubiQWCx4FGy9SWa60X1F/fPBQycaEQ==",
                    FirstName = "Student",
                    LastName = "Affairs",
                    FullName = "Student Affairs",
                    IsActive = true,
                    CreatedAt = new DateTime(2026, 1, 1),
                    RoleId = 2,
                    SecurityStamp = "f9d7a40b-130f-432a-8015-3e6381c6f961",
                    ConcurrencyStamp = "e4d6f077-c63e-4dab-8f76-c8beed69f411"
                }
            );
        }
    }
}
