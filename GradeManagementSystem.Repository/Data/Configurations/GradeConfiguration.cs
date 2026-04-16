using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GradeManagementSystem.Core.Entities.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GradeManagementSystem.Repository.Data.Configurations
{
    public class GradeConfiguration : IEntityTypeConfiguration<Grade>
    {
        public void Configure(EntityTypeBuilder<Grade> builder)
        {

            builder.HasKey(g => g.GradeID);

            builder.Property(g => g.Score)
                   .IsRequired()
                   .HasColumnType("decimal(8,2)");

            builder.Property(g => g.GradeType)
                   .IsRequired();

         
            builder.HasOne(g => g.Student)
                   .WithMany()
                   .HasForeignKey(g => g.StudentID)
                   .OnDelete(DeleteBehavior.Restrict);

           
            builder.HasOne(g => g.Class)
                   .WithMany()
                   .HasForeignKey(g => g.ClassID)
                   .OnDelete(DeleteBehavior.Restrict);

       
            builder.HasOne(g => g.Subject)
                   .WithMany()
                   .HasForeignKey(g => g.SubjectID)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(g => g.AcademicYear)
                   .WithMany()
                   .HasForeignKey(g => g.AcademicYearID)
                   .OnDelete(DeleteBehavior.Restrict);

            
            builder.HasOne(g => g.Term)
                   .WithMany()
                   .HasForeignKey(g => g.TermID)
                   .OnDelete(DeleteBehavior.Restrict)
                   .IsRequired(false);
        }
    }
}
