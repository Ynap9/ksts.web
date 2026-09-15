namespace kssm.be.applications.DongGoi.Package.Interfaces
{
    public interface IDossierLayoutService
    {
        string ChuanHoaDuongDan(string? relativePath, string fileName);

        string LayThuMucGoc(string relativePath);

        string LayThuMuc(string relativePath);

        string TenHienThi(string thuMuc);

        string LayThuMucCha(string thuMuc);

        string? TimExcelGanNhat(string thuMuc, IReadOnlyDictionary<string, string> excelTheoThuMuc);

        string? TimMaHoSoKhopNhat(string docId, IEnumerable<string> danhSachMa);

        bool LaMaCuaTaiLieu(string maHoSo, string docId);
    }
}
