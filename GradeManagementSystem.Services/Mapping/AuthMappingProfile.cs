using AutoMapper;
using GradeManagementSystem.Core.DTOs.Auth;
using GradeManagementSystem.Core.DTOs.Teacher;
using GradeManagementSystem.Core.Entities.Domain;
using GradeManagementSystem.Core.Entities.Identity;

namespace GradeManagementSystem.Services.Mapping
{
    public class AuthMappingProfile : Profile
    {
        public AuthMappingProfile()
        {
            CreateMap<ApplicationUser, UserInfoResponse>()
                .ForMember(dest => dest.UserId, opt => opt.MapFrom(src => src.UserId));



            // GET /teacher/profile
            CreateMap<Teacher, TeacherProfileReturnDTO>()
             .ForMember(dest => dest.Name,
                 opt => opt.MapFrom(src => src.User.FullName))

             .ForMember(dest => dest.currentAcademicYear,
                 opt => opt.MapFrom(src =>
                     src.TeacherAssignments
                         .Where(x => x.AcademicYear != null)
                         .Select(x => x.AcademicYear.YearName)
                         .FirstOrDefault()))

             .ForMember(dest => dest.subtitle,
                 opt => opt.MapFrom(src =>
                     src.TeacherAssignments
                         .Where(x => x.Subject != null)
                         .Select(x => x.Subject.SubjectName)
                         .FirstOrDefault()));




            //Get /teacher/Subject
            CreateMap<TeacherAssignment, TeacherSubjectReturnDTO>()
                    .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Subject.SubjectID))
                    .ForMember(dest => dest.SubjectName, opt => opt.MapFrom(src => src.Subject.SubjectName))
                    .ForMember(dest => dest.Title, opt => opt.MapFrom(src => src.AcademicYear.Stage))
                    .ForMember(dest => dest.Year, opt => opt.MapFrom(src => src.AcademicYear.YearName)) 
                    .ForMember(dest => dest.Route, opt => opt.MapFrom(src => "/teacher/classes"));



            //Get /teacher/Classes
            CreateMap<Class, TeacherClassDTO>()
              .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.ClassID))
              .ForMember(dest => dest.ClassName, opt => opt.MapFrom(src => src.ClassName))
              .ForMember(dest => dest.StudentCount, opt => opt.MapFrom(src => src.Capacity));

                    CreateMap<TeacherAssignment, TeacherClassesResponseDTO>()
                        .ForMember(dest => dest.Classes,
                            opt => opt.MapFrom(src =>
                                src.Class == null
                                ? new List<TeacherClassDTO>()
                                : new List<TeacherClassDTO> {
                        new TeacherClassDTO {
                            Id = src.Class.ClassID,
                            ClassName = src.Class.ClassName,
                            StudentCount = src.Class.Capacity ,
                        }
                                }))
                        .ForMember(dest => dest.SubjectName, opt => opt.MapFrom(src => src.Subject.SubjectName))
                        .ForMember(dest => dest.Year, opt => opt.MapFrom(src => src.AcademicYear.YearName));

        }
    }
}
