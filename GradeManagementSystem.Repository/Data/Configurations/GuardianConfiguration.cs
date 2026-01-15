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
    public class GuardianConfiguration : IEntityTypeConfiguration<Guardian>
    {
        public void Configure(EntityTypeBuilder<Guardian> builder)
        {
            builder.ToTable("Guardians");

            builder.HasKey(g => g.GuardianID);

            builder.Property(g => g.GuardianName)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(g => g.GuardianRelation)
                .HasMaxLength(50);

            builder.Property(g => g.GuardianPhone)
                .HasMaxLength(20);

            builder.HasOne(g => g.Student)
                .WithMany(s => s.Guardians)
                .HasForeignKey(g => g.StudentID)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
