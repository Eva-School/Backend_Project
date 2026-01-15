using GradeManagementSystem.Repository.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GradeManagementSystem.Repository.Data.Configurations
{
    public class ApplicationIdentityRoleConfiguration
     : IEntityTypeConfiguration<ApplicationIdentityRole>
    {
        public void Configure(EntityTypeBuilder<ApplicationIdentityRole> builder)
        {
            builder.ToTable("AspNetRoles");

            // Foreign Key to ApplicationRole
            builder.Property(r => r.RoleId)
                .IsRequired();

            builder.HasOne(r => r.ApplicationRole)
                .WithOne()
                .HasForeignKey<ApplicationIdentityRole>(r => r.RoleId)
                .OnDelete(DeleteBehavior.Restrict);

            // Index for better performance
            builder.HasIndex(r => r.RoleId)
                .IsUnique();

            // Seed Identity Roles (ApplicationRoles)
            //builder.HasData(
            //    new ApplicationIdentityRole
            //    {
            //        Id = 1,
            //        RoleId = 1,
            //        Name = "Admin",
            //        NormalizedName = "ADMIN"
            //    },
            //    new ApplicationIdentityRole
            //    {
            //        Id = 2,
            //        RoleId = 2,
            //        Name = "Teacher",
            //        NormalizedName = "TEACHER"
            //    },
            //    new ApplicationIdentityRole
            //    {
            //        Id = 3,
            //        RoleId = 3,
            //        Name = "Student",
            //        NormalizedName = "STUDENT"
            //    }
            //);
        }
    }
}
