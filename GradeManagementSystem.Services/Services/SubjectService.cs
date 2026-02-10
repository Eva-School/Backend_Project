using GradeManagementSystem.Core.DTOs.Subject;
using GradeManagementSystem.Core.Entities.Domain;
using GradeManagementSystem.Core.Entities.Enums;
using GradeManagementSystem.Core.Interfaces;
using GradeManagementSystem.Repository.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GradeManagementSystem.Services.Services
{
    public class SubjectService : ISubjectService
    {
        private readonly GradeDbContext _context;

        public SubjectService(GradeDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<SubjectResponseDTO>> GetSubjectsForActiveYearAsync()
        {
            // 1. Get all active academic years (instead of just the first one)
            var activeYears = await _context.AcademicYears
                .Where(y => y.IsActive)
                .ToListAsync();

            if (!activeYears.Any())
            {
                return null;
            }

            var activeYearIds = activeYears.Select(y => y.AcademicYearID).ToList();

            // 2. Get all subjects that belong to ANY of the active academic years
            var subjects = await _context.Subjects
                .Include(s => s.AcademicYear) // Include navigation property to access YearName and Stage
                .Where(s => s.AcademicYearID.HasValue && activeYearIds.Contains(s.AcademicYearID.Value) && s.IsActive)
                .Select(s => new SubjectResponseDTO
                {
                    Id = s.SubjectID,
                    SubjectName = s.SubjectName,
                    YearName = s.AcademicYear.YearName,
                    Stage = s.AcademicYear.Stage.ToString()
                })
                .ToListAsync();

            return subjects;
        }

        public async Task<SubjectResponseDTO> CreateSubjectAsync(CreateSubjectRequestDTO request)
        {
            // 1. Parse the stage from the request
            if (!Enum.TryParse<EducationStage>(request.Stage, true, out var stage))
            {
                return null;
            }

            // 2. Find the SPECIFIC active academic year that matches the requested stage
            var targetedYear = await _context.AcademicYears
                .Where(y => y.IsActive && y.Stage == stage)
                .OrderByDescending(y => y.AcademicYearID)
                .FirstOrDefaultAsync();

            if (targetedYear == null)
            {
                throw new InvalidOperationException($"No active academic year found for the specified stage: {request.Stage}");
            }

            // 3. Create the subject linked to the correct AcademicYearID
            var subject = new Subject
            {
                SubjectName = request.SubjectName.Trim(),
                AcademicYearID = targetedYear.AcademicYearID,
                IsActive = true
            };

            _context.Subjects.Add(subject);
            await _context.SaveChangesAsync();

            // 4. Return the response reflecting the actual year and stage it was linked to
            return new SubjectResponseDTO
            {
                Id = subject.SubjectID,
                SubjectName = subject.SubjectName,
                YearName = targetedYear.YearName,
                Stage = targetedYear.Stage.ToString()
            };
        }
    }
}
