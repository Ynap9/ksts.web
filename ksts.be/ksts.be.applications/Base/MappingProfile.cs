using AutoMapper;
using ksts.be.application.Auth.Dtos.Role;
using ksts.be.application.Auth.Dtos.User;
using ksts.be.applications.Template.Dtos;
using ksts.be.domain.Auth;
using Microsoft.AspNetCore.Identity;
using TemplateEntity = ksts.be.domain.Template.Template;
using TemplatePositionEntity = ksts.be.domain.Template.TemplatePosition;

namespace ksts.be.applications.Base
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<AppUser, ViewUserDto>();
            CreateMap<AppUser, ViewMeDto>();
            CreateMap<IdentityRole, ViewRoleDto>();
            CreateMap<TemplateEntity, ViewTemplateDto>();
            CreateMap<AddConfigTemplateDto, UpdateConfigTemplateDto>();
            CreateMap<TemplatePositionEntity, TemplatePositionDto>();
        }
    }
}
