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
    public class CompetencyConfiguration : IEntityTypeConfiguration<Competency>
    {
        public void Configure(EntityTypeBuilder<Competency> builder)
        {
            builder.ToTable("Competencies");

            builder.HasKey(c => c.CompetencyID);

            builder.Property(c => c.CompetencyName)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(c => c.MaxAttempts)
                .IsRequired();

            builder.Property(c => c.IsActive)
                .IsRequired()
                .HasDefaultValue(true);

            builder.Property(c => c.CreatedAt)
                .IsRequired()
                .HasDefaultValueSql("NOW()");

            builder.HasOne(c => c.Major)
                .WithMany(m => m.Competencies)
                .HasForeignKey(c => c.MajorID)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
