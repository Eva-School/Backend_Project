using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using GradeManagementSystem.Core.DTOs.Class;
using GradeManagementSystem.Core.DTOs.Student;
using GradeManagementSystem.Core.DTOs.Teacher;
using GradeManagementSystem.Core.Entities.Domain;
using GradeManagementSystem.Core.Entities.Enums;
using GradeManagementSystem.Core.Interfaces.Repositories;
using GradeManagementSystem.Core.Interfaces.Services;
using GradeManagementSystem.Core.Specifications.Includs;
using GradeManagementSystem.Repository.Data;
using GradeManagementSystem.Repository.Repositories;
using Microsoft.EntityFrameworkCore;

namespace GradeManagementSystem.Services.Services
{
    public class TeacherGradeService : ITeacherGradeService
    {
        private readonly IGenericRepository<Grade> gradeRepo;
        private readonly GradeDbContext gradeDbContext;
        private readonly IGenericRepository<Class> repo;
        private readonly IMapper mapper;

        public TeacherGradeService(IGenericRepository<Grade> gradeRepo, GradeDbContext gradeDbContext, IGenericRepository<Class> repo,IMapper mapper)
        {
           
            this.gradeRepo = gradeRepo;
            this.gradeDbContext = gradeDbContext;
            this.repo = repo;
            this.mapper = mapper;
        }

        public async Task<ClassStudentsResponseDto> GetStudentsByClassAsync(int classId)
        {
            var spec = new ClassWithStudentsSpecification(classId);

            var classData = await repo.GetWithIDAsync(spec);

            if (classData == null)
                return null;

            var result = mapper.Map<ClassStudentsResponseDto>(classData);

            foreach (var student in result.Students)
            {
                var term = classData.Students
                    .FirstOrDefault(s => s.StudentID == student.Id)?
                    .SubjectTermResults?.FirstOrDefault();

                var final = classData.Students
                    .FirstOrDefault(s => s.StudentID == student.Id)?
                    .AllResults?.FirstOrDefault();

                int? teacher = (int?)term?.FinalExamScore;
                int? finalGrade = (int?)final?.FinalSubjectScore;

                student.QuarterGrade = (int?)term?.TermTotal;
                student.TeacherGrade = teacher;
                student.FinalGrade = finalGrade;

                student.Status =
                    teacher.HasValue && teacher >= (finalGrade ?? 15)
                    ? "pass"
                    : "fail";
            }

            return result;
        }

        public async Task<object> SubmitGradeAsync(TeacherSubmitGradeDto dto )//, int teacherId)
        {
            if (dto.Score < 0 || dto.Score > 100)
                throw new Exception("Invalid grade value");


            var assignment = await gradeDbContext.TeacherAssignments
             .FirstOrDefaultAsync(x =>
             x.ClassID == dto.ClassID &&
             x.IsActive);

            if (assignment == null)
                throw new Exception("No assignment found for this class");



            var grade = new Grade
            {
                ClassID = dto.ClassID,
                StudentID = dto.StudentID,
                Score = dto.Score

            };
            await gradeRepo.AddAsync(grade);

            return new { ok = true };

        }
    }
}
