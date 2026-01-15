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
    public class ApplicationRoleConfiguration : IEntityTypeConfiguration<ApplicationRole>
    {
        public void Configure(EntityTypeBuilder<ApplicationRole> builder)
        {
            builder.ToTable("Roles");

            // Primary Key
            builder.HasKey(r => r.RoleId);

            builder.Property(r => r.RoleId)
                .HasColumnName("RoleID");

            builder.Property(r => r.RoleName)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(r => r.Description)
                .HasColumnType("text");

            // Navigation to Users
            builder.HasMany(r => r.Users)
                .WithOne(u => u.Role)
                .HasForeignKey(u => u.RoleId)
                .OnDelete(DeleteBehavior.Restrict);

            // Seed Default Roles
            //builder.HasData(
            //    new ApplicationRole
            //    {
            //        RoleId = 1,
            //        RoleName = "Admin",
            //        Description = "System Administrator"
            //    },
            //    new ApplicationRole
            //    {
            //        RoleId = 2,
            //        RoleName = "Teacher",
            //        Description = "Teacher Role"
            //    },
            //    new ApplicationRole
            //    {
            //        RoleId = 3,
            //        RoleName = "Student",
            //        Description = "Student Role"
            //    }
            //);
        }
    }
}
