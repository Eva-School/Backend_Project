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
    public class AcademicYearConfiguration : IEntityTypeConfiguration<AcademicYear>
    {
        public void Configure(EntityTypeBuilder<AcademicYear> builder)
        {
            builder.ToTable("AcademicYears");

            builder.HasKey(a => a.AcademicYearID);

            builder.Property(a => a.YearName)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(a => a.OrderNumber)
                .IsRequired();

            builder.Property(a => a.IsActive)
                .IsRequired()
                .HasDefaultValue(true);

            builder.HasIndex(a => a.YearName).IsUnique();
            builder.HasIndex(a => a.OrderNumber).IsUnique();
        }
    }
}
