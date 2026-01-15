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
    public class TermConfiguration : IEntityTypeConfiguration<Term>
    {
        public void Configure(EntityTypeBuilder<Term> builder)
        {
            builder.ToTable("Terms");

            builder.HasKey(t => t.TermID);

            builder.Property(t => t.TermName)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(t => t.StartDate)
                .IsRequired();

            builder.Property(t => t.EndDate)
                .IsRequired();

            builder.HasOne(t => t.AcademicYear)
                .WithMany(a => a.Terms)
                .HasForeignKey(t => t.AcademicYearID)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
