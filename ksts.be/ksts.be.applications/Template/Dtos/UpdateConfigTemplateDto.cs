namespace ksts.be.applications.Template.Dtos
{
    public class UpdateConfigTemplateDto : AddConfigTemplateDto
    {
        public bool XoaAnhDauDo { get; set; }

        public bool XoaAnhChuKyTuoi { get; set; }
    }
}
