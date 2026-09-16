using AutoMapper;
using kssm.be.applications.Base;
using kssm.be.applications.DongGoi.Package.Dtos;
using kssm.be.applications.DongGoi.Package.Interfaces;
using kssm.be.external.Excel.Interfaces;
using kssm.be.infrastructure.Persistence;
using kssm.be.shared.Constants.DongGoi;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Text;

namespace kssm.be.applications.DongGoi.Package.Implements
{
    public class MetadataValueService : BaseService, IMetadataValueService
    {
        private readonly IHeaderMatchService _headerMatchService;
        private readonly IExcelSheetReader _excelSheetReader;

        public MetadataValueService(
            KssmDbContext kstsDbContext,
            IHttpContextAccessor httpContextAccessor,
            ILogger<MetadataValueService> logger,
            IMapper mapper,
            IHeaderMatchService headerMatchService,
            IExcelSheetReader excelSheetReader
        ) : base(kstsDbContext, logger, httpContextAccessor, mapper)
        {
            _headerMatchService = headerMatchService;
            _excelSheetReader = excelSheetReader;
        }

        public List<string> KiemDong(PackageSchemaDto schema, HeaderMatchReportDto baoCao,
            IReadOnlyDictionary<string, string> dong, PackageType packageType, string? maHoSo)
        {
            var loi = new List<string>();

            foreach (var field in schema.Fields)
            {
                var giaTri = LayGiaTri(baoCao, dong, field.FieldKey);

                var ketQua = string.IsNullOrEmpty(giaTri)
                    ? KiemThieu(schema, baoCao, dong, field)
                    : KiemGiaTri(field, giaTri, packageType, maHoSo);

                if (ketQua != null)
                {
                    loi.Add(ketQua);
                }
            }

            return loi;
        }

        public string TachMa(string giaTri)
        {
            var viTri = giaTri.IndexOf(MetadataValueConstants.DauTachMa);
            return viTri > 0 ? giaTri[..viTri].Trim() : giaTri;
        }

        public string LayGiaTri(HeaderMatchReportDto baoCao, IReadOnlyDictionary<string, string> dong,
            string fieldKey)
        {
            var header = _headerMatchService.ChonHeader(baoCao, fieldKey);

            return header == null
                ? string.Empty
                : _excelSheetReader.LayGiaTri(dong, new[] { header }).Trim();
        }

        public string? KiemThieu(PackageSchemaDto schema, HeaderMatchReportDto baoCao,
            IReadOnlyDictionary<string, string> dong, SchemaFieldDto field)
        {
            if (field.Requirement == FieldRequirement.Required)
            {
                return $"Thiếu \"{field.DisplayName}\"";
            }

            if (field.Requirement != FieldRequirement.Conditional || !DungDieuKien(schema, baoCao, dong, field))
            {
                return null;
            }

            var truongDieuKien = schema.Fields.First(x =>
                string.Equals(x.FieldKey, field.ConditionField, StringComparison.Ordinal));

            return $"Thiếu \"{field.DisplayName}\", bắt buộc khi \"{truongDieuKien.DisplayName}\" "
                + $"là {field.ConditionValues}";
        }

        public bool DungDieuKien(PackageSchemaDto schema, HeaderMatchReportDto baoCao,
            IReadOnlyDictionary<string, string> dong, SchemaFieldDto field)
        {
            if (string.IsNullOrWhiteSpace(field.ConditionField) || string.IsNullOrWhiteSpace(field.ConditionValues)
                || !schema.Fields.Any(x => string.Equals(x.FieldKey, field.ConditionField, StringComparison.Ordinal)))
            {
                return false;
            }

            var giaTriDieuKien = TachMa(LayGiaTri(baoCao, dong, field.ConditionField));

            return field.ConditionValues
                .Split(MetadataValueConstants.DauPhanCachDieuKien,
                    StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Contains(giaTriDieuKien, StringComparer.Ordinal);
        }

        public string? KiemGiaTri(SchemaFieldDto field, string giaTri, PackageType packageType, string? maHoSo)
        {
            if (field.Codes.Count > 0)
            {
                var loiMa = KiemMa(field, giaTri);
                if (loiMa != null)
                {
                    return loiMa;
                }
            }

            var giaTriGhi = field.Codes.Count > 0 ? TachMa(giaTri) : giaTri;

            var loiKieu = field.DataType switch
            {
                MetadataDataType.Date => KiemNgay(field, giaTriGhi, packageType),
                MetadataDataType.Number => KiemSo(field, giaTriGhi),
                MetadataDataType.Boolean => KiemDungSai(field, giaTriGhi),
                _ => null,
            };

            if (loiKieu != null)
            {
                return loiKieu;
            }

            if (field.FieldLength.HasValue && giaTriGhi.Length > field.FieldLength.Value)
            {
                return $"\"{field.DisplayName}\" dài {giaTriGhi.Length} ký tự, vượt quá {field.FieldLength} ký tự";
            }

            return string.Equals(field.FieldKey, MetadataValueConstants.FieldKeyMaLuuTru, StringComparison.Ordinal)
                ? KiemMaLuuTru(field, giaTriGhi, packageType, maHoSo)
                : null;
        }

        public string? KiemMa(SchemaFieldDto field, string giaTri)
        {
            var nhieuGiaTri = MetadataValueConstants.FieldKeysNhieuGiaTri.Contains(field.FieldKey, StringComparer.Ordinal);

            var phan = nhieuGiaTri
                ? giaTri.Split(MetadataValueConstants.DauPhanCachNhieuGiaTri,
                    StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                : new[] { giaTri };

            if (phan.Length == 0)
            {
                return $"\"{field.DisplayName}\" = \"{giaTri}\" không có mã nào";
            }

            if (phan.Length > 1 && phan.Any(x => x.Contains(MetadataValueConstants.DauTachMa)))
            {
                return $"\"{field.DisplayName}\" chọn nhiều giá trị thì chỉ ghi mã, cách nhau bằng dấu phẩy (ví dụ 01, 02)";
            }

            var saiMa = phan
                .Select(TachMa)
                .Where(ma => !field.Codes.Contains(ma, StringComparer.Ordinal))
                .ToList();

            return saiMa.Count == 0
                ? null
                : $"\"{field.DisplayName}\" có mã {string.Join(", ", saiMa.Select(x => $"\"{x}\""))} "
                    + $"không thuộc danh mục ({string.Join(", ", field.Codes)})";
        }

        public string? KiemNgay(SchemaFieldDto field, string giaTri, PackageType packageType)
        {
            var dinhDang = packageType != PackageType.Sip
                && MetadataValueConstants.FieldKeysNgayRutGon.Contains(field.FieldKey, StringComparer.Ordinal)
                    ? MetadataValueConstants.DinhDangNgayRutGon
                    : new[] { MetadataValueConstants.DinhDangNgay };

            return DateTime.TryParseExact(giaTri, dinhDang, CultureInfo.InvariantCulture, DateTimeStyles.None, out _)
                ? null
                : $"\"{field.DisplayName}\" = \"{giaTri}\" không đúng định dạng "
                    + string.Join(" hoặc ", dinhDang.Select(x => x.ToUpperInvariant()));
        }

        public string? KiemSo(SchemaFieldDto field, string giaTri)
        {
            return giaTri.All(char.IsAsciiDigit)
                ? null
                : $"\"{field.DisplayName}\" = \"{giaTri}\" phải là số nguyên không âm";
        }

        public string? KiemDungSai(SchemaFieldDto field, string giaTri)
        {
            return giaTri is MetadataValueConstants.GiaTriDung or MetadataValueConstants.GiaTriSai
                ? null
                : $"\"{field.DisplayName}\" = \"{giaTri}\" chỉ nhận {MetadataValueConstants.GiaTriDung} "
                    + $"hoặc {MetadataValueConstants.GiaTriSai}";
        }

        public string? KiemMaLuuTru(SchemaFieldDto field, string giaTri, PackageType packageType, string? maHoSo)
        {
            var viTri = giaTri.LastIndexOf(MetadataValueConstants.DauNoiMa);
            var soThuTu = viTri > 0 ? giaTri[(viTri + 1)..] : string.Empty;

            if (soThuTu.Length != MetadataValueConstants.DoDaiSoThuTuTaiLieu || !soThuTu.All(char.IsAsciiDigit))
            {
                return $"\"{field.DisplayName}\" = \"{giaTri}\" phải kết thúc bằng số thứ tự tài liệu "
                    + $"{MetadataValueConstants.DoDaiSoThuTuTaiLieu} ký tự (ví dụ .0000001)";
            }

            if (packageType != PackageType.Sip || string.IsNullOrWhiteSpace(maHoSo))
            {
                return null;
            }

            return string.Equals(giaTri[..viTri].Normalize(NormalizationForm.FormC),
                    maHoSo.Trim().Normalize(NormalizationForm.FormC), StringComparison.OrdinalIgnoreCase)
                ? null
                : $"\"{field.DisplayName}\" = \"{giaTri}\" phải bắt đầu bằng mã hồ sơ \"{maHoSo}\"";
        }
    }
}
