using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GradeManagementSystem.Core.DTOs.Class;
using GradeManagementSystem.Core.DTOs.Teacher;

namespace GradeManagementSystem.Core.Interfaces.Services
{
    public interface ITeacherGradeService
    {
        Task<object> SubmitGradeAsync(TeacherSubmitGradeDto dto);//, int teacherId);
        Task<ClassStudentsResponseDto> GetStudentsByClassAsync(int classId);
    }
}
