using GradeManagementSystem.Core.DTOs.TeacherAssignment;
using GradeManagementSystem.Core.DTOs.Class;
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
        Task<(bool success, string message)> ReplaceTeacherAssignmentClassesAsync(TeacherAssignmentRequestDTO request);
        Task<List<TeacherAssignmentListItemDto>> GetAssignmentsAsync(string? yearName, string? stage);
        Task<(bool success, string message)> SetAssignmentStatusAsync(TeacherAssignmentStatusRequestDto request);
        Task<(bool success, string message)> DeleteAssignmentAsync(TeacherAssignmentStatusRequestDto request);
        Task<List<TeacherAssignmentDashboardYearDto>> GetMyDashboardAsync(int teacherUserId);
        Task<List<ClassResponseDTO>> GetMyClassesAsync(int teacherUserId, string yearId);
    }
}
