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
    public class CompetencyAttemptConfiguration : IEntityTypeConfiguration<CompetencyAttempt>
    {
        public void Configure(EntityTypeBuilder<CompetencyAttempt> builder)
        {
            builder.ToTable("CompetencyAttempts");

            builder.HasKey(ca => ca.AttemptID);

            builder.Property(ca => ca.AttemptNumber)
                .IsRequired();

            builder.Property(ca => ca.Result)
                .HasMaxLength(50);

            builder.HasOne(ca => ca.StudentCompetencyStatus)
                .WithMany(scs => scs.CompetencyAttempts)
                .HasForeignKey(ca => ca.StudentCompetencyStatusID)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(ca => ca.Student)
                .WithMany(s => s.CompetencyAttempts)
                .HasForeignKey(ca => ca.StudentID)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(ca => ca.Evaluator)
                .WithMany(t => t.EvaluatedCompetencies)
                .HasForeignKey(ca => ca.EvaluatedBy)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
