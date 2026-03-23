using GradeManagementSystem.Core.DTOs.TeacherAssignment;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GradeManagementSystem.Core.Interfaces
{
    public interface ITeacherAssignmentService
    {
        Task<(bool success, string message)> AssignTeacherToClassesAsync(TeacherAssignmentRequestDTO request);
    }
}
