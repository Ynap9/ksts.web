using kssm.be.external.Excel.Dtos;
using kssm.be.shared.Constants.DongGoi;

namespace kssm.be.applications.DongGoi.Package.Dtos
{
    public class ExcelDaDocDto
    {
        public string? Loi { get; set; }

        public ExcelSheetDto? SheetHoSo { get; set; }

        public HeaderMatchReportDto? BaoCaoHoSo { get; set; }

        public string? HeaderMaHoSo { get; set; }

        public List<SheetTaiLieuDto> TaiLieu { get; set; } = new();
    }

    public class SheetTaiLieuDto
    {
        public MetadataObjectType ObjectType { get; set; }

        public ExcelSheetDto Sheet { get; set; } = new();

        public HeaderMatchReportDto BaoCao { get; set; } = new();

        public string? HeaderDocId { get; set; }

        public string? HeaderMaHoSo { get; set; }

        public string? HeaderSoThuTu { get; set; }

        public bool CoMaDinhDanh => HeaderDocId != null || (HeaderMaHoSo != null && HeaderSoThuTu != null);
    }
}
