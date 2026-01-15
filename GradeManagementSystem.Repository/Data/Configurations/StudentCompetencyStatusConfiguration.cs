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
    public class StudentCompetencyStatusConfiguration : IEntityTypeConfiguration<StudentCompetencyStatus>
    {
        public void Configure(EntityTypeBuilder<StudentCompetencyStatus> builder)
        {
            builder.ToTable("StudentCompetencyStatuses");

            builder.HasKey(scs => scs.StudentCompetencyStatusID);

            builder.Property(scs => scs.StatusID)
                .HasMaxLength(50);

            builder.Property(scs => scs.CurrentAttemptNumber)
                .IsRequired();

            builder.Property(scs => scs.MaxAllowedAttempts)
                .IsRequired();

            builder.HasOne(scs => scs.Student)
                .WithMany(s => s.StudentCompetencyStatuses)
                .HasForeignKey(scs => scs.StudentID)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(scs => scs.Competency)
                .WithMany(c => c.StudentCompetencyStatuses)
                .HasForeignKey(scs => scs.CompetencyID)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(scs => new { scs.StudentID, scs.CompetencyID }).IsUnique();
        }
    }
}
