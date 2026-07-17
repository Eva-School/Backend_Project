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

        public async Task<IEnumerable<SubjectResponseDTO>> GetSubjectsForActiveYearAsync(string? yearName = null, string? stage = null)
        {
            // Subjects are a shared catalogue. AcademicYearID records the original
            // setup context only; it does not limit where the subject can be used.
            // The stage remains meaningful, so Junior/Wheeler/Senior keep separate
            // subject lists without duplicating them for every academic year.
            if (!string.IsNullOrWhiteSpace(stage))
            {
                if (!Enum.TryParse<EducationStage>(stage, true, out var parsedStage))
                {
                    throw new ArgumentException("Invalid stage. Expected: junior|wheeler|senior.");
                }
                var stageSubjects = await _context.Subjects
                    .Where(subject => subject.IsActive && subject.AcademicYear != null && subject.AcademicYear.Stage == parsedStage)
                    .Select(subject => new SubjectResponseDTO
                    {
                        Id = subject.SubjectID,
                        SubjectName = subject.SubjectName,
                        YearName = subject.AcademicYear!.YearName,
                        Stage = subject.AcademicYear.Stage.ToString()
                    })
                    .ToListAsync();

                return stageSubjects
                    .GroupBy(subject => subject.SubjectName.Trim(), StringComparer.OrdinalIgnoreCase)
                    .Select(group => group.OrderBy(subject => subject.Id).First())
                    .OrderBy(subject => subject.SubjectName)
                    .ToList();
            }

            var allSubjects = await _context.Subjects
                .Where(subject => subject.IsActive)
                .Select(subject => new SubjectResponseDTO
                {
                    Id = subject.SubjectID,
                    SubjectName = subject.SubjectName,
                    YearName = subject.AcademicYear != null ? subject.AcademicYear.YearName : string.Empty,
                    Stage = subject.AcademicYear != null ? subject.AcademicYear.Stage.ToString() : string.Empty
                })
                .ToListAsync();

            return allSubjects
                .GroupBy(subject => new { subject.Stage, Name = subject.SubjectName.Trim() })
                .Select(group => group.OrderBy(subject => subject.Id).First())
                .OrderBy(subject => subject.Stage)
                .ThenBy(subject => subject.SubjectName)
                .ToList();
        }

        public async Task<SubjectResponseDTO?> CreateSubjectAsync(CreateSubjectRequestDTO request)
        {
            // 1. Parse the stage from the request
            if (!Enum.TryParse<EducationStage>(request.Stage, true, out var stage))
            {
                return null;
            }

            // A new catalogue subject keeps its creation context for its level, but
            // can subsequently be used in every academic year of that level.
            var targetedYear = await _context.AcademicYears
                .Where(y => y.IsActive && y.Stage == stage && y.YearName == request.YearName.Trim())
                .OrderByDescending(y => y.AcademicYearID)
                .FirstOrDefaultAsync();

            if (targetedYear == null)
            {
                throw new InvalidOperationException($"No active academic year named '{request.YearName}' was found for the specified stage: {request.Stage}");
            }

            var existing = await _context.Subjects
                .Include(subject => subject.AcademicYear)
                .FirstOrDefaultAsync(subject => subject.IsActive &&
                    subject.SubjectName == request.SubjectName.Trim() &&
                    subject.AcademicYear != null && subject.AcademicYear.Stage == stage);
            if (existing != null)
            {
                return new SubjectResponseDTO
                {
                    Id = existing.SubjectID,
                    SubjectName = existing.SubjectName,
                    YearName = existing.AcademicYear!.YearName,
                    Stage = existing.AcademicYear.Stage.ToString()
                };
            }

            // Create the shared catalogue subject with its initial setup context.
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
