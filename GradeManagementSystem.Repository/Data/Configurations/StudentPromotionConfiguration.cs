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
    public class StudentPromotionConfiguration : IEntityTypeConfiguration<StudentPromotion>
    {
        public void Configure(EntityTypeBuilder<StudentPromotion> builder)
        {
            builder.ToTable("StudentPromotions");

            builder.HasKey(sp => sp.PromotionID);

            builder.Property(sp => sp.RequestDate)
                .IsRequired();

            builder.Property(sp => sp.IsApproved)
                .IsRequired()
                .HasDefaultValue(false);

            builder.HasOne(sp => sp.Student)
                .WithMany(s => s.PromotionsFrom)
                .HasForeignKey(sp => sp.StudentID)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(sp => sp.FromAcademicYear)
                .WithMany(a => a.PromotionsFrom)
                .HasForeignKey(sp => sp.FromAcademicYearID)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(sp => sp.ToAcademicYear)
                .WithMany(a => a.PromotionsTo)
                .HasForeignKey(sp => sp.ToAcademicYearID)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
