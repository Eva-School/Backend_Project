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
    public class ClassConfiguration : IEntityTypeConfiguration<Class>
    {
        public void Configure(EntityTypeBuilder<Class> builder)
        {
            builder.ToTable("Classes");

            builder.HasKey(c => c.ClassID);

            builder.Property(c => c.ClassName)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(c => c.Capacity)
                .IsRequired();

            builder.Property(c => c.IsActive)
                .IsRequired()
                .HasDefaultValue(true);

            builder.HasOne(c => c.AcademicYear)
                .WithMany(a => a.Classes)
                .HasForeignKey(c => c.AcademicYearID)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(c => c.Department)
                .WithMany(d => d.Classes)
                .HasForeignKey(c => c.DepartmentID)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(c => c.ClassName).IsUnique();
        }
    }
}
