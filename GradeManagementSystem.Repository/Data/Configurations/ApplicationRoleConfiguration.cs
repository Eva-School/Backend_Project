using GradeManagementSystem.Core.Entities.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GradeManagementSystem.Repository.Data.Configurations
{
    public class ApplicationRoleConfiguration : IEntityTypeConfiguration<ApplicationRole>
    {
        public void Configure(EntityTypeBuilder<ApplicationRole> builder)
        {
            builder.ToTable("AspNetRoles");

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
            builder.HasData(
                new ApplicationRole
                {
                    RoleId = 1,
                    Id = 1,
                    Name = "Admin",
                    NormalizedName = "ADMIN",
                    RoleName = "Admin",
                    Description = "System Administrator",
                    ConcurrencyStamp = "39081a45-da45-4b25-9aca-787bbaf07b22"
                },
                new ApplicationRole
                {
                    RoleId = 2,
                    Id = 2,
                    Name = "StudentAffairs",
                    NormalizedName = "STUDENTAFFAIRS",
                    RoleName = "Student Affairs",
                    Description = "Student Affairs Officer",
                    ConcurrencyStamp = "6fea2cca-8207-43aa-90dc-006a3755a606"
                },
                new ApplicationRole
                {
                    RoleId = 3,
                    Id = 3,
                    Name = "Teacher",
                    NormalizedName = "TEACHER",
                    RoleName = "Teacher",
                    Description = "Teacher Role",
                    ConcurrencyStamp = "7172e6ec-4b75-4da6-8a74-5317a3b4924f"
                },
                new ApplicationRole
                {
                    RoleId = 4,
                    Id = 4,
                    Name = "Student",
                    NormalizedName = "STUDENT",
                    RoleName = "Student",
                    Description = "Student Role",
                    ConcurrencyStamp = "0adc8fbb-91cb-4dd7-a957-59453987a14f"
                }
            );
        }
    }
}
