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
    public class TeacherAssignmentConfiguration : IEntityTypeConfiguration<TeacherAssignment>
    {
        public void Configure(EntityTypeBuilder<TeacherAssignment> builder)
        {
            builder.ToTable("TeacherAssignments");

            builder.HasKey(ta => new { ta.TeacherID, ta.ClassID, ta.SubjectID, ta.AcademicYearID });

            builder.Property(ta => ta.AssignedAt)
                .IsRequired()
                .HasDefaultValueSql("GETDATE()");

            builder.Property(ta => ta.IsActive)
                .IsRequired()
                .HasDefaultValue(true);

            builder.HasOne(ta => ta.Teacher)
                .WithMany(t => t.TeacherAssignments)
                .HasForeignKey(ta => ta.TeacherID)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(ta => ta.Class)
                .WithMany(c => c.TeacherAssignments)
                .HasForeignKey(ta => ta.ClassID)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(ta => ta.Subject)
                .WithMany(s => s.TeacherAssignments)
                .HasForeignKey(ta => ta.SubjectID)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(ta => ta.AcademicYear)
                .WithMany(a => a.TeacherAssignments)
                .HasForeignKey(ta => ta.AcademicYearID)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
