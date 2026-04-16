using AutoMapper;
using GradeManagementSystem.Core.DTOs.Auth;
using GradeManagementSystem.Core.DTOs.Class;
using GradeManagementSystem.Core.DTOs.Student;
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

            CreateMap<Student, StudentDto>()
                      .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.StudentID))
                      .ForMember(dest => dest.Name, opt => opt.MapFrom(src => $"Student {src.StudentID}"));

            CreateMap<Class, ClassStudentsResponseDto>()
                .ForMember(dest => dest.ClassId, opt => opt.MapFrom(src => src.ClassID))
                .ForMember(dest => dest.ClassName, opt => opt.MapFrom(src => src.ClassName))
                .ForMember(dest => dest.Students, opt => opt.MapFrom(src => src.Students));

        }
    }
}
