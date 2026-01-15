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
    public class ResultApprovalConfiguration : IEntityTypeConfiguration<ResultApproval>
    {
        public void Configure(EntityTypeBuilder<ResultApproval> builder)
        {
            builder.ToTable("ResultApprovals");

            builder.HasKey(ra => ra.ApprovalID);

            builder.Property(ra => ra.Decision)
                .IsRequired()
                .HasConversion<string>();

            builder.Property(ra => ra.Notes)
                .HasColumnType("text");

            builder.HasOne(ra => ra.StudentAllResults)
                .WithOne(ar => ar.ResultApproval)
                .HasForeignKey<ResultApproval>(ra => ra.AllResultID)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(ra => ra.AllResultID).IsUnique();
        }
    }
}
