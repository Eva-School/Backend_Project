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
    public class PreviousSchoolsConfiguration : IEntityTypeConfiguration<PreviousSchools>
    {
        public void Configure(EntityTypeBuilder<PreviousSchools> builder)
        {
            builder.ToTable("PreviousSchools");

            builder.HasKey(p => p.PreviousSchoolID);

            builder.Property(p => p.SchoolName)
                .IsRequired()
                .HasMaxLength(200);

            builder.HasOne(p => p.Student)
                .WithMany(s => s.PreviousSchools)
                .HasForeignKey(p => p.StudentID)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
