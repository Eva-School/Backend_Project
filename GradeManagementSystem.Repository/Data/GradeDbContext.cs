using GradeManagementSystem.Core.Entities.Domain;
using GradeManagementSystem.Core.Entities.Identities;
using GradeManagementSystem.Repository.Data.Configurations;
using GradeManagementSystem.Repository.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GradeManagementSystem.Repository.Data
{
    public class GradeDbContext : IdentityDbContext<ApplicationIdentityUser, ApplicationIdentityRole, int>
    {
        public GradeDbContext(DbContextOptions<GradeDbContext> options) : base(options)
        {
        }

        // DbSets
        public DbSet<Student> Students { get; set; }
        public DbSet<Guardian> Guardians { get; set; }
        public DbSet<PreviousSchools> PreviousSchools { get; set; }
        public DbSet<Teacher> Teachers { get; set; }
        public DbSet<AcademicYear> AcademicYears { get; set; }
        public DbSet<Term> Terms { get; set; }
        public DbSet<Department> Departments { get; set; }
        public DbSet<Major> Majors { get; set; }
        public DbSet<Subject> Subjects { get; set; }
        public DbSet<Class> Classes { get; set; }
        public DbSet<TeacherAssignment> TeacherAssignments { get; set; }
        public DbSet<Competency> Competencies { get; set; }
        public DbSet<StudentCompetencyStatus> StudentCompetencyStatuses { get; set; }
        public DbSet<CompetencyAttempt> CompetencyAttempts { get; set; }
        public DbSet<StudentSubjectTermResult> StudentSubjectTermResults { get; set; }
        public DbSet<StudentAllResults> StudentAllResults { get; set; }
        public DbSet<ResultApproval> ResultApprovals { get; set; }
        public DbSet<StudentPromotion> StudentPromotions { get; set; }

        // Domain entities
        public DbSet<ApplicationUser> Users { get; set; }
        public DbSet<ApplicationRole> Roles { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Apply all configurations
            modelBuilder.ApplyConfiguration(new ApplicationUserConfiguration());
            modelBuilder.ApplyConfiguration(new ApplicationRoleConfiguration());
            modelBuilder.ApplyConfiguration(new ApplicationIdentityUserConfiguration());
            modelBuilder.ApplyConfiguration(new ApplicationIdentityRoleConfiguration());

            modelBuilder.ApplyConfiguration(new StudentConfiguration());
            modelBuilder.ApplyConfiguration(new GuardianConfiguration());
            modelBuilder.ApplyConfiguration(new PreviousSchoolsConfiguration());
            modelBuilder.ApplyConfiguration(new TeacherConfiguration());
            modelBuilder.ApplyConfiguration(new AcademicYearConfiguration());
            modelBuilder.ApplyConfiguration(new TermConfiguration());
            modelBuilder.ApplyConfiguration(new DepartmentConfiguration());
            modelBuilder.ApplyConfiguration(new MajorConfiguration());
            modelBuilder.ApplyConfiguration(new SubjectConfiguration());
            modelBuilder.ApplyConfiguration(new ClassConfiguration());
            modelBuilder.ApplyConfiguration(new TeacherAssignmentConfiguration());
            modelBuilder.ApplyConfiguration(new CompetencyConfiguration());
            modelBuilder.ApplyConfiguration(new StudentCompetencyStatusConfiguration());
            modelBuilder.ApplyConfiguration(new CompetencyAttemptConfiguration());
            modelBuilder.ApplyConfiguration(new StudentSubjectTermResultConfiguration());
            modelBuilder.ApplyConfiguration(new StudentAllResultsConfiguration());
            modelBuilder.ApplyConfiguration(new ResultApprovalConfiguration());
            modelBuilder.ApplyConfiguration(new StudentPromotionConfiguration());

            // Configure Identity table names
            modelBuilder.Entity<Microsoft.AspNetCore.Identity.IdentityUserRole<int>>()
                .ToTable("UserRoles");

            modelBuilder.Entity<Microsoft.AspNetCore.Identity.IdentityUserClaim<int>>()
                .ToTable("UserClaims");

            modelBuilder.Entity<Microsoft.AspNetCore.Identity.IdentityUserLogin<int>>()
                .ToTable("UserLogins");

            modelBuilder.Entity<Microsoft.AspNetCore.Identity.IdentityUserToken<int>>()
                .ToTable("UserTokens");

            modelBuilder.Entity<Microsoft.AspNetCore.Identity.IdentityRoleClaim<int>>()
                .ToTable("RoleClaims");
        }
    }
}
