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
    public class ApplicationIdentityUserConfiguration
       : IEntityTypeConfiguration<ApplicationIdentityUser>
    {
        public void Configure(EntityTypeBuilder<ApplicationIdentityUser> builder)
        {
            builder.ToTable("AspNetUsers");

            // Foreign Key to ApplicationUser
            builder.Property(u => u.UserId)
                .IsRequired();

            builder.HasOne(u => u.ApplicationUser)
                .WithOne()
                .HasForeignKey<ApplicationIdentityUser>(u => u.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Index for better performance
            builder.HasIndex(u => u.UserId)
                .IsUnique();
        }
    }
}
