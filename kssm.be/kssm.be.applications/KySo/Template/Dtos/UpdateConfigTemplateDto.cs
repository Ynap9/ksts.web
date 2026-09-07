namespace kssm.be.applications.KySo.Template.Dtos
{
    public class UpdateConfigTemplateDto : AddConfigTemplateDto
    {
        public bool XoaAnhDauDo { get; set; }

        public bool XoaAnhChuKyTuoi { get; set; }
    }
}
