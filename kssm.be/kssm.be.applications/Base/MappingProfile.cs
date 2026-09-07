using AutoMapper;
using kssm.be.applications.Template.Dtos;
using TemplateEntity = kssm.be.domain.Template.Template;
using TemplatePositionEntity = kssm.be.domain.Template.TemplatePosition;


namespace kssm.be.applications.Base
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            // Toạ độ đi theo quan hệ mềm nên entity template không mang danh sách Positions - service tự nạp
            // và tự gán, ở đây bỏ qua để AutoMapper không cố dò một thành viên không tồn tại.
            CreateMap<TemplateEntity, ViewTemplateDto>()
                .ForMember(dest => dest.Positions, opt => opt.Ignore());
            CreateMap<TemplatePositionEntity, TemplatePositionDto>();
            CreateMap<AddConfigTemplateDto, UpdateConfigTemplateDto>();
        }
    }
}
