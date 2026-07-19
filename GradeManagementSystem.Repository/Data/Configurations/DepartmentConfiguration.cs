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
    public class DepartmentConfiguration : IEntityTypeConfiguration<Department>
    {
        public void Configure(EntityTypeBuilder<Department> builder)
        {
            builder.ToTable("Departments");

            builder.HasKey(d => d.DepartmentID);

            builder.Property(d => d.DepartmentName)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(d => d.Description)
                .HasColumnType("text");

            builder.Property(d => d.IsActive)
                .IsRequired()
                .HasDefaultValue(true);

            builder.Property(d => d.CreatedAt)
                .IsRequired()
                .HasDefaultValueSql("NOW()");

            builder.HasIndex(d => d.DepartmentName).IsUnique();

            // Seed Data
            var seedDate = DateTime.SpecifyKind(new DateTime(2024, 1, 1), DateTimeKind.Utc);

            builder.HasData(
                new Department { DepartmentID = 1, DepartmentName = "Mathematics", Description = "Mathematics Department", IsActive = true, CreatedAt = seedDate },
                new Department { DepartmentID = 2, DepartmentName = "Science", Description = "Science Department", IsActive = true, CreatedAt = seedDate },
                new Department { DepartmentID = 3, DepartmentName = "English", Description = "English Language Department", IsActive = true, CreatedAt = seedDate },
                new Department { DepartmentID = 4, DepartmentName = "Social Studies", Description = "Social Studies Department", IsActive = true, CreatedAt = seedDate },
                new Department { DepartmentID = 5, DepartmentName = "Physical Education", Description = "Physical Education Department", IsActive = true, CreatedAt = seedDate }
            );
        }
    }
}
