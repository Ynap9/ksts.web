namespace kssm.be.shared.Constants.DongGoi
{
    public static class MetadataValueConstants
    {
        public const char DauTachMa = ':';
        public const char DauPhanCachDieuKien = ',';
        public const char DauNoiMa = '.';

        public static readonly char[] DauPhanCachNhieuGiaTri = { ',', ';' };

        public const string DinhDangNgay = "dd/MM/yyyy";
        public static readonly string[] DinhDangNgayRutGon = { "dd/MM/yyyy", "MM/yyyy", "yyyy" };

        public const string GiaTriDung = "1";
        public const string GiaTriSai = "0";

        public const string FieldKeyMaLuuTru = "docCode";

        public static readonly string[] FieldKeysNhieuGiaTri = { "language" };
        public static readonly string[] FieldKeysNgayRutGon = { "startDate", "endDate" };

        public const int DoDaiSoThuTuTaiLieu = 7;
        public const int DoDaiLyDoToiDa = 2000;
        public const string DauNoiLyDo = "; ";
    }
}
