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
    public class MajorConfiguration : IEntityTypeConfiguration<Major>
    {
        public void Configure(EntityTypeBuilder<Major> builder)
        {
            builder.ToTable("Majors");

            builder.HasKey(m => m.MajorID);

            builder.Property(m => m.MajorName)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(m => m.Description)
                .HasColumnType("text");

            builder.Property(m => m.IsActive)
                .IsRequired()
                .HasDefaultValue(true);

            builder.HasOne(m => m.Department)
                .WithMany(d => d.Majors)
                .HasForeignKey(m => m.DepartmentID)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(m => m.MajorName).IsUnique();
        }
    }
}
